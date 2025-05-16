using System.Threading.Tasks;

namespace Common.Services.Implementations
{
    public interface ISftpService
    {
        Task<bool> Connect();

        void Disconnect();

        bool IsConnected { get; }
    }
}