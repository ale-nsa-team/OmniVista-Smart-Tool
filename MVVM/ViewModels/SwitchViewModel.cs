using PoEWizard.Device;
using System;
using System.ComponentModel;

namespace MVVM.ViewModels
{
    public class SwitchViewModel : ViewModelBase, IDisposable
    {
        private bool _isDisposed;

        public SwitchViewModel(SwitchModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Model.PropertyChanged += OnModelPropertyChanged;
        }

        public SwitchModel Model { get; }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Model.PropertyChanged -= OnModelPropertyChanged;
                }

                _isDisposed = true;
            }
        }

        ~SwitchViewModel()
        {
            Dispose(false);
        }
    }
}