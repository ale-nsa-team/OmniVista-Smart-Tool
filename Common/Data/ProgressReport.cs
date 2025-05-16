using Common.Enums;

namespace Common.Data
{
    public class ProgressReport
    {
        public ReportType Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public ProgressReport(string message)
        {
            Type = ReportType.Status;
            Title = string.Empty;
            Message = message;
        }

        public ProgressReport(ReportType type, string title, string message)
        {
            Type = type;
            Title = title;
            Message = message;
        }
    }
}