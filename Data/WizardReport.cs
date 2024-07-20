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
        public string Description { get; set; } = string.Empty;
        public string PortStatus { get; set; } = string.Empty;
        public string Error {  get; set; } = string.Empty;
        public string Duration {  get; set; } = string.Empty;

        public ReportResult(string port, string wizardAction)
        {
            Port = port;
            WizardAction = wizardAction;
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("\n - ").Append(WizardAction);
            if (!string.IsNullOrEmpty(Description)) txt.Append(" ").Append(Description);
            if (!string.IsNullOrEmpty(Error)) txt.Append("\n    ").Append(Error);
            return txt.ToString();
        }
    }
    public class WizardReport
    {

        private readonly object _lock_report_result = new object();

        public WizardResult Result => this.GetReportResult();
        public ConcurrentDictionary<string, List<ReportResult>> ReportResult { get; set; } = new ConcurrentDictionary<string, List<ReportResult>>();
        public string Message => this.ToString();
        public string Error => this.GetErrorMessage();

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

        public void UpdateResult(string port, WizardResult result, string description = null)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(port);
                ReportResult report = GetLastReport(reportList);
                if (report == null) return;
                report.Result = result;
                if (!string.IsNullOrEmpty(description)) report.Description = description;
                reportList[reportList.Count - 1] = report;
                this.ReportResult[port] = reportList;
            }
        }

        public void UpdatePortStatus(string port, string status)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(port);
                ReportResult report = GetLastReport(reportList);
                if (report == null) return;
                report.PortStatus = status;
                reportList[reportList.Count - 1] = report;
                this.ReportResult[port] = reportList;
            }
        }

        public void UpdateError(string port, string error)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(port);
                ReportResult report = GetLastReport(reportList);
                if (report == null) return;
                report.Error = error;
                reportList[reportList.Count - 1] = report;
                this.ReportResult[port] = reportList;
            }
        }

        public void UpdateDuration(string port, string duration)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(port);
                ReportResult report = GetLastReport(reportList);
                if (report == null) return;
                report.Duration = duration;
                reportList[reportList.Count - 1] = report;
                this.ReportResult[port] = reportList;
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

        private string GetErrorMessage()
        {
            lock (_lock_report_result)
            {
                StringBuilder txt = new StringBuilder();
                foreach (var reports in this.ReportResult)
                {
                    List<ReportResult> reportsList = reports.Value as List<ReportResult>;
                    foreach (var report in reportsList)
                    {
                        if (string.IsNullOrEmpty(report.Error)) continue;
                        if (txt.Length > 0) txt.Append("\n");
                        txt.Append(report.Error);
                    }
                }
                return txt.ToString();
            }
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
