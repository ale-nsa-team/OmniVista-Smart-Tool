using Common.Services.Implementations;
using MVVM.Models;
using System;
using System.Threading.Tasks;

namespace Common.Services
{
    public class SftpService : ISftpService
    {
        private readonly SwitchModel _switchModel;

        public SftpService(SwitchModel switchModel)
        {
            _switchModel = switchModel ?? throw new ArgumentNullException();
        }

        public bool IsConnected => throw new NotImplementedException();

        public Task<bool> Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }
    }
}