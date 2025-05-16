using System.Threading.Tasks;

namespace Common.Services.Implementations
{
    public interface ISshService
    {
        Task<bool> Connect();

        void Disconnect();

        bool IsConnected { get; }
    }
}