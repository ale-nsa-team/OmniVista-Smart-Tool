using System.Text;

namespace PoEWizard.Data
{
    public class ProgressReportResult
    {
        public bool Proceed { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public ProgressReportResult() {  }
    }
}
