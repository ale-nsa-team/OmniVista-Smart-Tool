namespace PoEWizard.Comm
{
    public interface IComService
    {
        bool Connected { get; }
        ResultCallback Callback { get; set; }
        void Connect();
        void Write(string text);
        void Write(byte[] bytes);
        void Close();
        void ClearBuffer();
    }
}
