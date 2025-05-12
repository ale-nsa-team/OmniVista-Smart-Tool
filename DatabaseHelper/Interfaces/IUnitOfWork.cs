using DatabaseHelper.Entities;
using System;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<SwitchEntity> Switches { get; }
        IChassisRepository Chassis { get; }
        ISlotRepository Slots { get; }
        IPortRepository Ports { get; }
        IEndpointDeviceRepository EndPointDevices { get; }
        IPowerSupplyRepository PowerSupplies { get; }
        ITemperatureRepository Temperatures { get; }
        ICapabilityRepository Capabilities { get; }
        ISwitchDebugAppRepository SwitchDebugApps { get; }
        IDatabaseVersionRepository DatabaseVersions { get; }

        Task BeginTransactionAsync();

        Task CommitAsync();

        Task RollbackAsync();
    }
}