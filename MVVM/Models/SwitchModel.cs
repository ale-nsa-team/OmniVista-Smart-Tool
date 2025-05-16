using System.ComponentModel;

namespace MVVM.Models
{
    public class SwitchModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string IpAddress { get; set; }
        public string Name { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public int Timeout { get; set; }

        public SwitchModel()
        { }
    }
}