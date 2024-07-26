using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{

    public class ReportResult
    {
        public WizardResult Result { get; set; }
        public string ID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActionResult { get; set; } = string.Empty;
        public string PortStatus { get; set; } = string.Empty;
        public string AlertDescription {  get; set; } = string.Empty;
        public string Duration {  get; set; } = string.Empty;
        public object Parameter { get; set; } = null;

        public ReportResult(string id, string action) : this(id, WizardResult.Starting, action) { }
        public ReportResult(string id, WizardResult result, string action)
        {
            ID = id;
            Action = action;
            Result = result;
            if (result == WizardResult.Warning || result == WizardResult.Fail) AlertDescription = action;
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("\n - ").Append(Action);
            if (!string.IsNullOrEmpty(ActionResult)) txt.Append(" ").Append(ActionResult);
            if (!string.IsNullOrEmpty(AlertDescription)) txt.Append("\n    ").Append(AlertDescription);
            return txt.ToString();
        }
    }

    public class WizardReport
    {

        private readonly object _lock_report_result = new object();

        public ConcurrentDictionary<string, List<ReportResult>> Result { get; set; } = new ConcurrentDictionary<string, List<ReportResult>>();
        public string Message => this.ToString();

        public WizardReport() {  }

        public void CreateReportResult(string id, WizardResult result, string action)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(id);
                reportList.Add(new ReportResult(id, result, action));
                this.Result[id] = reportList;
            }
        }

        public WizardResult GetReportResult(string id)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report == null) return WizardResult.Proceed;
                return report.Result;
            }
        }

        public string GetAlertDescription(string id)
        {
            lock (_lock_report_result)
            {
                return GetCurrentReport(id)?.AlertDescription;
            }
        }

        public object GetReturnParameter(string id)
        {
            lock (_lock_report_result)
            {
                return GetCurrentReport(id)?.Parameter;
            }
        }

        public void UpdateResult(string id, WizardResult result, string actionResult = null)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report != null)
                {
                    report.Result = result;
                    if (!string.IsNullOrEmpty(actionResult)) report.ActionResult = actionResult;
                }
            }
        }

        public void RemoveLastWizardReport(string id)
        {
            lock (_lock_report_result)
            {
                List<ReportResult> reportList = GetReportList(id);
                if (reportList?.Count > 0)
                {
                    reportList.RemoveAt(reportList.Count - 1);
                    this.Result[id] = reportList;
                }
            }
        }

        public void UpdateWizardReport(string id, WizardResult result, string action)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report != null)
                {
                    report.Result = result;
                    report.Action = action;
                    report.ActionResult = string.Empty;
                }
            }
        }

        public void UpdatePortStatus(string id, string status)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report != null) report.PortStatus = status;
            }
        }

        public void UpdateAlert(string id, WizardResult alert, string alertDescription)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report != null)
                {
                    report.Result = alert;
                    report.AlertDescription = alertDescription;
                }
            }
        }

        public void UpdateDuration(string id, string duration)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report != null) report.Duration = duration;
            }
        }

        public void SetReturnParameter(string id, object parameter)
        {
            lock (_lock_report_result)
            {
                ReportResult report = GetCurrentReport(id);
                if (report != null) report.Parameter = parameter;
            }
        }

        private ReportResult GetCurrentReport(string id)
        {
            List<ReportResult> reportList = GetReportList(id);
            return GetLastReport(reportList);
        }

        private ReportResult GetLastReport(List<ReportResult> reportList)
        {
            if (reportList?.Count > 0) return reportList[reportList.Count - 1]; else return null;
        }

        private List<ReportResult> GetReportList(string id)
        {
            if (this.Result.ContainsKey(id))
            {
                if (this.Result.TryGetValue(id, out List<ReportResult> reportList)) return reportList;
            }
            return new List<ReportResult>();
        }

        public override string ToString()
        {
            lock (_lock_report_result)
            {
                StringBuilder txt = new StringBuilder();
                foreach (var reports in this.Result)
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
