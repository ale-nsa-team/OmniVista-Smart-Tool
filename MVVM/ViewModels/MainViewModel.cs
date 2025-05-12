using MVVM.Commands;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace MVVM.ViewModels
{
    public class MainViewModel : ViewModelBase, IDisposable, INotifyPropertyChanged
    {
        private SwitchViewModel _currentSwitch;
        private bool _isBusy;
        private bool _disposed;

        public MainViewModel()
        {
            ConnectCommand = new RelayCommand<object>(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);
        }

        public SwitchViewModel CurrentSwitch
        {
            get => _currentSwitch;
            set
            {
                if (_currentSwitch != value)
                {
                    _currentSwitch = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ConnectCommand { get; }

        public ICommand DisconnectCommand { get; }

        private void Connect(object owner)
        {
            throw new NotImplementedException();
        }

        private void Disconnect()
        {
            throw new NotImplementedException();
        }

        private bool CanConnect(object owner)
        {
            return !IsBusy && owner != null;
        }

        private bool CanDisconnect()
        {
            return !IsBusy && CurrentSwitch != null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                CurrentSwitch = null;
            }

            _disposed = true;
        }

        ~MainViewModel()
        {
            Dispose(false);
        }
    }
}