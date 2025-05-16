using System.Threading.Tasks;

namespace Common.Services.Implementations
{
    public interface IRestService
    {
        Task<bool> Connect();

        void Disconnect();

        bool IsConnected { get; }
    }
}