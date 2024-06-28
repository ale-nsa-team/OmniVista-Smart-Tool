using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace PoEWizard.Comm
{
    public class RestApiClient
    {
        #region Internal Constants

        private const int WAIT_SWITCH_CONNECT_DELAY_MS = 100;

        #endregion Internal Constants

        #region Internal Variables

        private readonly HttpClient _httpClient;
        private readonly string _ip_address;
        private readonly string _login;
        private readonly string _password;
        private readonly double _cnx_timeout;
        private bool _connected = false;

        #endregion Internal Variables

        #region Properties

        #endregion Properties

        #region Constructors

        public RestApiClient(SwitchModel switchInfo)
        {
            this._ip_address = switchInfo.IpAddr;
            this._login = switchInfo.Login;
            this._password = switchInfo.Password;
            this._cnx_timeout = switchInfo.CnxTimeout;
            this._httpClient = new HttpClient();
            if (!string.IsNullOrEmpty(switchInfo.IpAddr))
            {
                this._httpClient.BaseAddress = new Uri($"https://{this._ip_address}");
                this._httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.alcatellucentaos+xml");
                this._httpClient.Timeout = TimeSpan.FromSeconds(this._cnx_timeout);
            }

            System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }
        #endregion Constructors

        #region Open Rest Api Client Method

        public void Login()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                string domain = "authv2";
                string url = $"{this._httpClient.BaseAddress}?domain={domain}&username={this._login}&password={this._password}";
                var response = this._httpClient.GetAsync(url);
                while (!response.IsCompleted)
                {
                    if (Utils.IsTimeExpired(startTime, this._cnx_timeout))
                    {
                        throw new SwitchConnectionFailure($"{PrintUnreachableError(startTime)}\nTook too long to respond (>{Utils.CalcStringDuration(startTime)})");
                    }
                    Thread.Sleep(WAIT_SWITCH_CONNECT_DELAY_MS);
                };
                if (response.IsCanceled || response.Result == null || response.Result.Content == null)
                {
                    throw new SwitchConnectionFailure(PrintUnreachableError(startTime));
                }
                XmlDocument xmlDoc = GetRestApiResponse(response.Result.Content.ReadAsStringAsync().Result);
                XmlNode tokenNode;
                if (response.Result.StatusCode == HttpStatusCode.OK)
                {
                    tokenNode = xmlDoc.SelectSingleNode("/nodes/result/data/token");
                    if (tokenNode != null && !string.IsNullOrEmpty(tokenNode.InnerText))
                    {
                        RemoveToken();
                        var authenticationHeaderValue = new AuthenticationHeaderValue("Bearer", tokenNode.InnerText);
                        this._httpClient.DefaultRequestHeaders.Authorization = authenticationHeaderValue;
                        _connected = true;
                    }
                    else
                    {
                        _connected = false;
                        throw new SwitchAuthenticationFailure("Invalid response body - token not found!");
                    }
                }
                else
                {
                    _connected = false;
                    tokenNode = xmlDoc.SelectSingleNode("/nodes/result/error");
                    if (tokenNode != null) throw new SwitchAuthenticationFailure(tokenNode.InnerText);
                }
            }
            catch (HttpRequestException ex)
            {
                string error = PrintUnreachableError(startTime);
                if (ex.InnerException != null)
                {
                    error = ex.InnerException.Message;
                }
                throw new SwitchConnectionFailure(error);
            }
            catch (SwitchConnectionFailure ex)
            {
                throw ex;
            }
            catch (SwitchAuthenticationFailure ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                string error = PrintUnreachableError(startTime);
                if (ex.InnerException != null)
                {
                    error = ex.InnerException.Message;
                    if (ex.InnerException.InnerException != null)
                    {
                        error = ex.InnerException.InnerException.Message;
                        if (error.ToLower().Contains("unable to connect"))
                        {
                            error = $"Failed to establish a connection to {this._ip_address}!";
                            throw new SwitchConnectionFailure(error);
                        }
                    }
                }
                throw new Exception(error);
            }
        }

        private XmlDocument GetRestApiResponse(string xml)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                return xmlDoc;
            }
            catch
            {
                throw new SwitchConnectionFailure(this._ip_address + " doesn't support Rest Api!");
            }
        }

        private void RemoveToken()
        {
            if (this._httpClient.DefaultRequestHeaders.Authorization != null)
            {
                this._httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
        }

        #endregion Open Rest Api Client Method

        #region Send Api Resquest

        public Dictionary<string, string> SendRequest(RestUrlEntry entry)
        {
            string url = RestUrl.ParseUrl(entry);
            if (string.IsNullOrEmpty(url)) throw new SwitchCommandError("Command line is missing!");
            url = $"{this._httpClient.BaseAddress}{url}";
            entry.StartTime = DateTime.Now;
            Dictionary<string, string> response;
            response = SendRestApiRequest(entry, url);
            response[RestUrl.DURATION] = Utils.CalcStringDuration(entry.StartTime);
            entry.Duration = response[RestUrl.DURATION];
            entry.Response = response;
            return response;
        }

        private Dictionary<string, string> SendRestApiRequest(RestUrlEntry entry, string url)
        {
            Dictionary<string, string> response = new Dictionary<string, string>
            {
                [RestUrl.REST_URL] = url,
                [RestUrl.RESULT] = "",
                [Constants.ERROR] = "",
                [RestUrl.DURATION] = ""
            };
            try
            {
                HttpRequestMessage request = GetHttpRequest(entry, url);
                var http_response = this._httpClient.SendAsync(request);
                while (!http_response.IsCompleted) { };
                if (http_response.IsCanceled || http_response.Result == null) throw new SwitchConnectionFailure("Switch connection failure!");
                if (http_response != null & http_response.Result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    RemoveToken();
                    Login();
                    request = GetHttpRequest(entry, url);
                    http_response = this._httpClient.SendAsync(request);
                }
                if (http_response.Result.StatusCode == HttpStatusCode.OK)
                {
                    response[RestUrl.RESULT] = http_response.Result.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    string errorDescr = ParseError(http_response, url);
                    if (errorDescr != null)
                    {
                        response[Constants.ERROR] = errorDescr;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException is HttpRequestException) throw new SwitchConnectionFailure("Switch connection failure!");
                else throw ex;
            }
            return response;
        }

        private string ParseError(Task<HttpResponseMessage> http_response, string url)
        {
            try
            {
                string xmlError = http_response.Result.Content.ReadAsStringAsync().Result;
                var errorList = CliParseUtils.ParseXmlToDictionary(xmlError, "//nodes//result//*");
                if (errorList.ContainsKey(RestUrl.API_ERROR) && !string.IsNullOrEmpty(errorList[RestUrl.API_ERROR]))
                {
                    string error = errorList[RestUrl.API_ERROR].Trim();
                    if (errorList.ContainsKey(RestUrl.HTTP_RESPONSE) && !string.IsNullOrEmpty(errorList[RestUrl.HTTP_RESPONSE]))
                    {
                        HttpStatusCode code = Utils.ConvertToHttpStatusCode(errorList);
                        error = $"{code} ({errorList[RestUrl.HTTP_RESPONSE]})\r\n{error}";
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        error = $"Requested URL: {url}\r\n{error}";
                        return error;
                    }
                }
            }
            catch { }
            return null;
        }

        private HttpRequestMessage GetHttpRequest(RestUrlEntry entry, string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(entry.Method, url);
            if (entry.Method == HttpMethod.Post && entry.Content != null)
            {
                request.Content = new FormUrlEncodedContent(entry.Content);
            }
            return request;
        }

        private string PrintUnreachableError(DateTime startTime)
        {
            return $"Switch is unreachable (>{Utils.CalcStringDuration(startTime)})";
        }

        #endregion Send Api Resquest

        #region Other Public Methods

        public void Abort()
        {
            this._httpClient.Dispose();
        }

        public bool IsConnected()
        {
            if (!_connected) Login();
            return _connected;
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("RestApiClient for Switch IP Address: ");
            txt.Append(this._ip_address).Append(", BaseUrl: ").Append(this._httpClient.BaseAddress);
            return txt.ToString();
        }

        #endregion Other Public Methods
    }

}
