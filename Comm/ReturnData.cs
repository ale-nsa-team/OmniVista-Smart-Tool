namespace PoEWizard.Comm
{
    public class ReturnData
    {
        public string Result { get; set; }
        public string Error { get; set; }

        public ReturnData(string result, string error)
        {
            Result = result;
            Error = error;
        }
    }
}
