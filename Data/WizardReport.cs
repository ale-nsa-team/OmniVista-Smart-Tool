using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{

    public class ReportResult
    {
        public WizardResult Result { get; set; } = WizardResult.Starting;
        public string Port { get; set; }
        public string WizardAction { get; set; } = string.Empty;
        public string ActionResult { get; set; } = string.Empty;
        public string PortStatus { get; set; } = string.Empty;
        public string AlertDescription {  get; set; } = string.Empty;
        public string Duration {  get; set; } = string.Empty;
        public object Parameter { get; set; } = null;

        public ReportResult(string port, string wizardAction)
        {
            Port = port;
            WizardAction = wizardAction;
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("\n - ").Append(WizardAction);
            if (!string.IsNullOrEmpty(ActionResult)) txt.Append(" ").Append(ActionResult);
            if (!string.IsNullOrEmpty(AlertDescription)) txt.Append("\n    ").Append(AlertDescription);
            return txt.ToString();
        }
    }
    public class WizardReport
    {

        private readonly object _lock_report_result = new object();

        public WizardResult Result => this.GetReportResult();
        public ConcurrentDictionary<string, List<ReportResult>> ReportResult { get; set; } = new ConcurrentDictionary<string, List<ReportResult>>();
        public string Message => this.ToString();

        public WizardReport() {  }

        public void CreateReportResult(string port, string wizardAction)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(port);
                reportList.Add(new ReportResult(port, wizardAction));
                this.ReportResult[port] = reportList;
            }
        }
        public string GetAlertDescription(string port)
        {
            lock (_lock_report_result)
            {
                return GetCurrentReport(port)?.AlertDescription;
            }
        }


        public object GetReturnParameter(string port)
        {
            lock (_lock_report_result)
            {
                return GetCurrentReport(port)?.Parameter;
            }
        }

        public void UpdateResult(string port, WizardResult result, string actionResult = null)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(port);
                if (report != null)
                {
                    report.Result = result;
                    if (!string.IsNullOrEmpty(actionResult)) report.ActionResult = actionResult;
                }
            }
        }

        public void RemoveLastWizardReport(string port)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(port);
                if (reportList?.Count > 0)
                {
                    reportList.RemoveAt(reportList.Count - 1);
                    this.ReportResult[port] = reportList;
                }
            }
        }

        public void UpdateWizardReport(string port, WizardResult result, string wizardAction)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(port);
                if (report != null)
                {
                    report.Result = result;
                    report.WizardAction = wizardAction;
                    report.ActionResult = string.Empty;
                }
            }
        }

        public void UpdatePortStatus(string port, string status)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(port);
                if (report != null) report.PortStatus = status;
            }
        }

        public void UpdateAlert(string port, WizardResult alert, string alertDescription)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(port);
                if (report != null)
                {
                    report.Result = alert;
                    report.AlertDescription = alertDescription;
                }
            }
        }

        public void UpdateDuration(string port, string duration)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(port);
                if (report != null) report.Duration = duration;
            }
        }

        public void SetReturnParameter(string port, object parameter)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(port);
                if (report != null) report.Parameter = parameter;
            }
        }

        private WizardResult GetReportResult()
        {
            lock (_lock_report_result)
            {
                if (this.ReportResult.Count < 0) return WizardResult.Proceed;
                foreach (var result in ReportResult)
                {
                    List<ReportResult> reportList = result.Value as List<ReportResult>;
                    ReportResult report = GetLastReport(reportList);
                    if (report == null) continue;
                    return report.Result;
                }
                return WizardResult.Proceed;
            }
        }

        private ReportResult GetCurrentReport(string port)
        {
            List<ReportResult> reportList = GetReportList(port);
            return GetLastReport(reportList);
        }

        private ReportResult GetLastReport(List<ReportResult> reportList)
        {
            if (reportList?.Count > 0) return reportList[reportList.Count - 1]; else return null;
        }

        private List<ReportResult> GetReportList(string port)
        {
            if (this.ReportResult.ContainsKey(port))
            {
                if (this.ReportResult.TryGetValue(port, out List<ReportResult> reportList)) return reportList;
            }
            return new List<ReportResult>();
        }

        public override string ToString()
        {
            lock (_lock_report_result)
            {
                StringBuilder txt = new StringBuilder();
                foreach (var reports in this.ReportResult)
                {
                    List<ReportResult> reportsList = reports.Value as List<ReportResult>;
                    foreach (var report in reportsList)
                    {
                        txt.Append(report.ToString());
                    }
                }
                return txt.ToString();
            }
        }

    }
}
