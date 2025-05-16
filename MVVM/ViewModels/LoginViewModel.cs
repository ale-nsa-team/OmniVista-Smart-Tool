using Common;
using MVVM.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace MVVM.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _ipAddress;
        private string _username;
        private string _password;
        private bool _isPasswordVisible;

        public static List<string> IpAddressPool = new List<string>();

        public LoginViewModel(string user)
        {
            // Initialize commands
            OkCommand = new RelayCommand(OnOk, CanOk);
            CancelCommand = new RelayCommand(OnCancel);
            ClearListCommand = new RelayCommand(ClearIpList);
            DeleteSelectedCommand = new RelayCommand(DeleteSelectedIp);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibility);

            // Initialize data
            IpList = new List<string>();

            User = user;

            // Set initial IP address
            if (IpList.Count > 0)
            {
                IpAddress = IpList[0];
            }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public string User
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsPwdVisible
        {
            get => _isPasswordVisible;
            set => SetProperty(ref _isPasswordVisible, value);
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearListCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; }

        public bool HasValidationErrors()
        {
            bool ipErr = string.IsNullOrEmpty(IpAddress);
            bool nameErr = string.IsNullOrEmpty(User?.Trim());
            bool pwdErr = string.IsNullOrEmpty(Password?.Trim());

            return ipErr || nameErr || pwdErr;
        }

        private bool CanOk()
        {
            return !HasValidationErrors();
        }

        private void OnOk()
        {
            if (HasValidationErrors()) return;

            if (!IpAddressPool.Contains(IpAddress))
            {
                if (IpAddressPool.Count >= Constants.MAX_SW_LIST_SIZE)
                {
                    IpList.RemoveAt(IpList.Count - 1);
                }
                IpList.Add(IpAddress);
                IpList.Sort();
                owner.Config.Set("switches", string.Join(",", IpList));
            }

            // Will be handled by view to close the window
            DialogResult = true;
        }

        private void OnCancel()
        {
            DialogResult = false;
        }

        private void ClearIpList()
        {
            IpList.Clear();
            MainWindow.Config.Set("switches", "");
            OnPropertyChanged(nameof(IpList));
        }

        private void DeleteSelectedIp()
        {
            IpList.Remove(IpAddress);
            MainWindow.Config.Set("switches", string.Join(",", IpList));
            OnPropertyChanged(nameof(IpList));

            if (IpList.Count > 0)
            {
                IpAddress = IpList[0];
            }
            else
            {
                IpAddress = string.Empty;
            }
        }

        private void TogglePasswordVisibility()
        {
            IsPwdVisible = !IsPwdVisible;
        }

        // Property to communicate dialog result back to the view
        public bool? DialogResult { get; private set; }
    }
}