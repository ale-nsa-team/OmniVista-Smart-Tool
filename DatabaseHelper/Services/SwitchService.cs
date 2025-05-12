using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using DatabaseHelper.Interfaces;
using DatabaseHelper.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseHelper.Services
{
    public class SwitchService : ISwitchService, IDisposable
    {
        private readonly IUnitOfWork _unitOfWork;
        private bool _disposed;

        public SwitchService(string dataPath)
        {
            DbContext.Initialize(dataPath);
            var dbContext = DbContext.Instance;
            _unitOfWork = new UnitOfWork(dbContext._connection);
        }

        /// <summary>
        /// Constructor for testing purposes that accepts a unit of work instance.
        /// </summary>
        /// <param name="unitOfWork">The unit of work to use</param>
        public SwitchService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        #region Switch CRUD with Complete Hierarchy

        public async Task<SwitchEntity> CreateSwitchWithHierarchyAsync(SwitchEntity switchEntity)
        {
            if (switchEntity == null)
                throw new ArgumentNullException(nameof(switchEntity));

            // Validate name
            if (string.IsNullOrWhiteSpace(switchEntity.Name))
                throw new ValidationException("Switch name cannot be empty");

            // Validate IP address format
            if (!IsValidIpAddress(switchEntity.IpAddress))
                throw new ValidationException("Invalid IP address format");

            // Check for duplicate IP addresses
            var existingSwitches = await _unitOfWork.Switches.GetAllAsync();
            if (existingSwitches.Any(s => s.IpAddress == switchEntity.IpAddress))
                throw new DuplicateEntityException($"A switch with IP address {switchEntity.IpAddress} already exists");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Add Switch
                var createdSwitch = await _unitOfWork.Switches.AddAsync(switchEntity);

                if (switchEntity.ChassisList != null)
                {
                    foreach (var chassis in switchEntity.ChassisList)
                    {
                        chassis.SwitchId = createdSwitch.Id;
                        var createdChassis = await _unitOfWork.Chassis.AddAsync(chassis);

                        // Add Slots
                        if (chassis.Slots != null)
                        {
                            foreach (var slot in chassis.Slots)
                            {
                                slot.ChassisId = createdChassis.Id;
                                var createdSlot = await _unitOfWork.Slots.AddAsync(slot);

                                // Add Ports
                                if (slot.Ports != null)
                                {
                                    foreach (var port in slot.Ports)
                                    {
                                        port.SlotId = createdSlot.Id;
                                        var createdPort = await _unitOfWork.Ports.AddAsync(port);

                                        // Add EndPointDevices
                                        if (port.EndPointDevices != null)
                                        {
                                            foreach (var device in port.EndPointDevices)
                                            {
                                                device.PortId = createdPort.Id;
                                                var createdDevice = await _unitOfWork.EndPointDevices.AddAsync(device);

                                                // Add Capabilities
                                                if (device.Capabilities != null)
                                                {
                                                    foreach (var capability in device.Capabilities)
                                                    {
                                                        capability.EndPointDeviceId = createdDevice.Id;
                                                        await _unitOfWork.Capabilities.AddAsync(capability);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Add PowerSupplies
                        if (chassis.PowerSupplies != null)
                        {
                            foreach (var powerSupply in chassis.PowerSupplies)
                            {
                                powerSupply.ChassisId = createdChassis.Id;
                                await _unitOfWork.PowerSupplies.AddAsync(powerSupply);
                            }
                        }

                        // Add Temperatures
                        if (chassis.Temperature != null)
                        {
                            TemperatureEntity temperature = chassis.Temperature;
                            temperature.ChassisId = createdChassis.Id;
                            await _unitOfWork.Temperatures.AddAsync(temperature);
                        }
                    }
                }

                // Add Debug Apps
                if (switchEntity.DebugApp != null)
                {
                    foreach (var debugApp in switchEntity.DebugApp.Values)
                    {
                        debugApp.SwitchId = createdSwitch.Id;
                        await _unitOfWork.SwitchDebugApps.AddAsync(debugApp);
                    }
                }

                await _unitOfWork.CommitAsync();
                return createdSwitch;
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<SwitchEntity> GetSwitchWithHierarchyAsync(Guid switchId)
        {
            if (switchId == Guid.Empty)
                throw new ArgumentNullException(nameof(switchId));

            // Check if switch exists
            bool exists = await _unitOfWork.Switches.ExistsAsync(switchId);
            if (!exists)
                return null;

            var switchEntity = await _unitOfWork.Switches.GetByIdAsync(switchId);
            if (switchEntity != null)
            {
                switchEntity.ChassisList = new List<ChassisEntity>();
                var chassisList = await _unitOfWork.Chassis.GetBySwitchIdAsync(switchId);

                foreach (var chassis in chassisList)
                {
                    // Load Slots and their hierarchy
                    chassis.Slots = new List<SlotEntity>();
                    var slots = await _unitOfWork.Slots.GetByChassisIdAsync(chassis.Id);

                    foreach (var slot in slots)
                    {
                        // Load Ports and their hierarchy
                        slot.Ports = new List<PortEntity>();
                        var ports = await _unitOfWork.Ports.GetBySlotIdAsync(slot.Id);

                        foreach (var port in ports)
                        {
                            // Load EndPointDevices and their capabilities
                            port.EndPointDevices = new List<EndpointDeviceEntity>();
                            var devices = await _unitOfWork.EndPointDevices.GetByPortIdAsync(port.Id);

                            foreach (var device in devices)
                            {
                                device.Capabilities = (await _unitOfWork.Capabilities.GetByDeviceIdAsync(device.Id)).ToList();
                                port.EndPointDevices.Add(device);
                            }
                            slot.Ports.Add(port);
                        }
                        chassis.Slots.Add(slot);
                    }

                    // Load PowerSupplies
                    chassis.PowerSupplies = (await _unitOfWork.PowerSupplies.GetByChassisIdAsync(chassis.Id)).ToList();

                    // Load Temperatures
                    chassis.Temperature = await _unitOfWork.Temperatures.GetByChassisIdAsync(chassis.Id);

                    switchEntity.ChassisList.Add(chassis);
                }

                // Load Debug Apps
                var debugApps = await _unitOfWork.SwitchDebugApps.GetBySwitchIdAsync(switchId);
                switchEntity.DebugApp = debugApps.ToDictionary(d => d.AppId, d => d);
            }
            return switchEntity;
        }

        public async Task UpdateSwitchWithHierarchyAsync(SwitchEntity switchEntity)
        {
            if (switchEntity == null)
                throw new ArgumentNullException(nameof(switchEntity));

            // Check if switch exists
            bool exists = await _unitOfWork.Switches.ExistsAsync(switchEntity.Id);
            if (!exists)
                throw new SwitchNotFoundException($"Switch with ID {switchEntity.Id} not found");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Update Switch
                await _unitOfWork.Switches.UpdateAsync(switchEntity);

                if (switchEntity.ChassisList != null)
                {
                    var existingChassis = await _unitOfWork.Chassis.GetBySwitchIdAsync(switchEntity.Id);
                    var existingChassisIds = existingChassis.Select(c => c.Id).ToList();
                    var newChassisIds = switchEntity.ChassisList.Select(c => c.Id).ToList();

                    // Handle Chassis deletions
                    foreach (var chassisToDelete in existingChassis.Where(c => !newChassisIds.Contains(c.Id)))
                    {
                        await DeleteChassisWithHierarchyAsync(chassisToDelete.Id);
                    }

                    // Handle Chassis updates and additions
                    foreach (var chassis in switchEntity.ChassisList)
                    {
                        if (existingChassisIds.Contains(chassis.Id))
                        {
                            await UpdateChassisWithHierarchyAsync(chassis);
                        }
                        else
                        {
                            chassis.SwitchId = switchEntity.Id;
                            await CreateChassisWithHierarchyAsync(chassis);
                        }
                    }
                }

                // Update Debug Apps
                if (switchEntity.DebugApp != null)
                {
                    var existingDebugApps = await _unitOfWork.SwitchDebugApps.GetBySwitchIdAsync(switchEntity.Id);
                    var existingDebugAppIds = existingDebugApps.Select(d => d.Id).ToList();
                    var newDebugAppIds = switchEntity.DebugApp.Values.Select(d => d.Id).ToList();

                    foreach (var debugAppToDelete in existingDebugApps.Where(d => !newDebugAppIds.Contains(d.Id)))
                    {
                        await _unitOfWork.SwitchDebugApps.DeleteAsync(debugAppToDelete.Id);
                    }

                    foreach (var debugApp in switchEntity.DebugApp.Values)
                    {
                        if (existingDebugAppIds.Contains(debugApp.Id))
                        {
                            await _unitOfWork.SwitchDebugApps.UpdateAsync(debugApp);
                        }
                        else
                        {
                            debugApp.SwitchId = switchEntity.Id;
                            await _unitOfWork.SwitchDebugApps.AddAsync(debugApp);
                        }
                    }
                }

                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteSwitchWithHierarchyAsync(Guid switchId)
        {
            if (switchId == Guid.Empty)
                throw new ArgumentNullException(nameof(switchId));

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Check if the switch exists before attempting to delete it
                var switchExists = await _unitOfWork.Switches.ExistsAsync(switchId);
                if (!switchExists)
                {
                    throw new SwitchNotFoundException($"Switch with ID {switchId} not found");
                }

                var chassisList = await _unitOfWork.Chassis.GetBySwitchIdAsync(switchId);
                foreach (var chassis in chassisList)
                {
                    await DeleteChassisWithHierarchyAsync(chassis.Id);
                }

                var debugApps = await _unitOfWork.SwitchDebugApps.GetBySwitchIdAsync(switchId);
                foreach (var debugApp in debugApps)
                {
                    await _unitOfWork.SwitchDebugApps.DeleteAsync(debugApp.Id);
                }

                await _unitOfWork.Switches.DeleteAsync(switchId);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        #endregion Switch CRUD with Complete Hierarchy

        #region Helper Methods

        private async Task DeleteChassisWithHierarchyAsync(Guid chassisId)
        {
            var slots = await _unitOfWork.Slots.GetByChassisIdAsync(chassisId);
            foreach (var slot in slots)
            {
                var ports = await _unitOfWork.Ports.GetBySlotIdAsync(slot.Id);
                foreach (var port in ports)
                {
                    var devices = await _unitOfWork.EndPointDevices.GetByPortIdAsync(port.Id);
                    foreach (var device in devices)
                    {
                        var capabilities = await _unitOfWork.Capabilities.GetByDeviceIdAsync(device.Id);
                        foreach (var capability in capabilities)
                        {
                            await _unitOfWork.Capabilities.DeleteAsync(capability.Id);
                        }
                        await _unitOfWork.EndPointDevices.DeleteAsync(device.Id);
                    }
                    await _unitOfWork.Ports.DeleteAsync(port.Id);
                }
                await _unitOfWork.Slots.DeleteAsync(slot.Id);
            }

            var powerSupplies = await _unitOfWork.PowerSupplies.GetByChassisIdAsync(chassisId);
            foreach (var powerSupply in powerSupplies)
            {
                await _unitOfWork.PowerSupplies.DeleteAsync(powerSupply.Id);
            }

            var temperature = await _unitOfWork.Temperatures.GetByChassisIdAsync(chassisId);
            if (temperature != null)
            {
                await _unitOfWork.Temperatures.DeleteAsync(temperature.Id);
            }

            await _unitOfWork.Chassis.DeleteAsync(chassisId);
        }

        private async Task CreateChassisWithHierarchyAsync(ChassisEntity chassis)
        {
            var createdChassis = await _unitOfWork.Chassis.AddAsync(chassis);

            if (chassis.Slots != null)
            {
                foreach (var slot in chassis.Slots)
                {
                    slot.ChassisId = createdChassis.Id;
                    var createdSlot = await _unitOfWork.Slots.AddAsync(slot);

                    if (slot.Ports != null)
                    {
                        foreach (var port in slot.Ports)
                        {
                            port.SlotId = createdSlot.Id;
                            var createdPort = await _unitOfWork.Ports.AddAsync(port);

                            if (port.EndPointDevices != null)
                            {
                                foreach (var device in port.EndPointDevices)
                                {
                                    device.PortId = createdPort.Id;
                                    var createdDevice = await _unitOfWork.EndPointDevices.AddAsync(device);

                                    if (device.Capabilities != null)
                                    {
                                        foreach (var capability in device.Capabilities)
                                        {
                                            capability.EndPointDeviceId = createdDevice.Id;
                                            await _unitOfWork.Capabilities.AddAsync(capability);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (chassis.PowerSupplies != null)
            {
                foreach (var powerSupply in chassis.PowerSupplies)
                {
                    powerSupply.ChassisId = createdChassis.Id;
                    await _unitOfWork.PowerSupplies.AddAsync(powerSupply);
                }
            }

            if (chassis.Temperature != null)
            {
                TemperatureEntity temperature = chassis.Temperature;
                temperature.ChassisId = createdChassis.Id;
                await _unitOfWork.Temperatures.AddAsync(temperature);
            }
        }

        private async Task UpdateChassisWithHierarchyAsync(ChassisEntity chassis)
        {
            try
            {
                // Update Chassis base properties
                await _unitOfWork.Chassis.UpdateAsync(chassis);

                // Update Slots hierarchy
                if (chassis.Slots != null)
                {
                    var existingSlots = await _unitOfWork.Slots.GetByChassisIdAsync(chassis.Id);
                    var existingSlotIds = existingSlots.Select(s => s.Id).ToList();
                    var newSlotIds = chassis.Slots.Select(s => s.Id).ToList();

                    // Delete slots that are no longer present
                    foreach (var slotToDelete in existingSlots.Where(s => !newSlotIds.Contains(s.Id)))
                    {
                        await DeleteSlotWithHierarchyAsync(slotToDelete.Id);
                    }

                    // Update or create slots
                    foreach (var slot in chassis.Slots)
                    {
                        if (existingSlotIds.Contains(slot.Id))
                        {
                            await UpdateSlotWithHierarchyAsync(slot);
                        }
                        else
                        {
                            slot.ChassisId = chassis.Id;
                            await CreateSlotWithHierarchyAsync(slot);
                        }
                    }
                }
                else
                {
                    // If slots collection is null, delete all existing slots
                    var existingSlots = await _unitOfWork.Slots.GetByChassisIdAsync(chassis.Id);
                    foreach (var slot in existingSlots)
                    {
                        await DeleteSlotWithHierarchyAsync(slot.Id);
                    }
                }

                // Update PowerSupplies
                if (chassis.PowerSupplies != null)
                {
                    var existingPowerSupplies = await _unitOfWork.PowerSupplies.GetByChassisIdAsync(chassis.Id);
                    var existingPowerSupplyIds = existingPowerSupplies.Select(p => p.Id).ToList();
                    var newPowerSupplyIds = chassis.PowerSupplies.Select(p => p.Id).ToList();

                    // Delete power supplies that are no longer present
                    foreach (var powerSupplyToDelete in existingPowerSupplies.Where(p => !newPowerSupplyIds.Contains(p.Id)))
                    {
                        await _unitOfWork.PowerSupplies.DeleteAsync(powerSupplyToDelete.Id);
                    }

                    // Update or create power supplies
                    foreach (var powerSupply in chassis.PowerSupplies)
                    {
                        if (existingPowerSupplyIds.Contains(powerSupply.Id))
                        {
                            powerSupply.ChassisId = chassis.Id;
                            await _unitOfWork.PowerSupplies.UpdateAsync(powerSupply);
                        }
                        else
                        {
                            powerSupply.ChassisId = chassis.Id;
                            await _unitOfWork.PowerSupplies.AddAsync(powerSupply);
                        }
                    }
                }
                else
                {
                    // If power supplies collection is null, delete all existing power supplies
                    var existingPowerSupplies = await _unitOfWork.PowerSupplies.GetByChassisIdAsync(chassis.Id);
                    foreach (var powerSupply in existingPowerSupplies)
                    {
                        await _unitOfWork.PowerSupplies.DeleteAsync(powerSupply.Id);
                    }
                }

                // Update Temperature
                var existingTemperature = await _unitOfWork.Temperatures.GetByChassisIdAsync(chassis.Id);
                if (chassis.Temperature != null)
                {
                    chassis.Temperature.ChassisId = chassis.Id;
                    if (existingTemperature != null)
                    {
                        // Update existing temperature
                        chassis.Temperature.Id = existingTemperature.Id;
                        await _unitOfWork.Temperatures.UpdateAsync(chassis.Temperature);
                    }
                    else
                    {
                        // Create new temperature
                        if (chassis.Temperature.Id == Guid.Empty)
                        {
                            chassis.Temperature.Id = Guid.NewGuid();
                        }
                        await _unitOfWork.Temperatures.AddAsync(chassis.Temperature);
                    }
                }
                else if (existingTemperature != null)
                {
                    // Delete existing temperature if new temperature is null
                    await _unitOfWork.Temperatures.DeleteAsync(existingTemperature.Id);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task DeleteSlotWithHierarchyAsync(Guid slotId)
        {
            var ports = await _unitOfWork.Ports.GetBySlotIdAsync(slotId);
            foreach (var port in ports)
            {
                var devices = await _unitOfWork.EndPointDevices.GetByPortIdAsync(port.Id);
                foreach (var device in devices)
                {
                    var capabilities = await _unitOfWork.Capabilities.GetByDeviceIdAsync(device.Id);
                    foreach (var capability in capabilities)
                    {
                        await _unitOfWork.Capabilities.DeleteAsync(capability.Id);
                    }
                    await _unitOfWork.EndPointDevices.DeleteAsync(device.Id);
                }
                await _unitOfWork.Ports.DeleteAsync(port.Id);
            }
            await _unitOfWork.Slots.DeleteAsync(slotId);
        }

        private async Task CreateSlotWithHierarchyAsync(SlotEntity slot)
        {
            var createdSlot = await _unitOfWork.Slots.AddAsync(slot);

            if (slot.Ports != null)
            {
                foreach (var port in slot.Ports)
                {
                    port.SlotId = createdSlot.Id;
                    var createdPort = await _unitOfWork.Ports.AddAsync(port);

                    if (port.EndPointDevices != null)
                    {
                        foreach (var device in port.EndPointDevices)
                        {
                            device.PortId = createdPort.Id;
                            var createdDevice = await _unitOfWork.EndPointDevices.AddAsync(device);

                            if (device.Capabilities != null)
                            {
                                foreach (var capability in device.Capabilities)
                                {
                                    capability.EndPointDeviceId = createdDevice.Id;
                                    await _unitOfWork.Capabilities.AddAsync(capability);
                                }
                            }
                        }
                    }
                }
            }
        }

        private async Task UpdateSlotWithHierarchyAsync(SlotEntity slot)
        {
            await _unitOfWork.Slots.UpdateAsync(slot);

            if (slot.Ports != null)
            {
                var existingPorts = await _unitOfWork.Ports.GetBySlotIdAsync(slot.Id);
                var existingPortIds = existingPorts.Select(p => p.Id).ToList();
                var newPortIds = slot.Ports.Select(p => p.Id).ToList();

                foreach (var portToDelete in existingPorts.Where(p => !newPortIds.Contains(p.Id)))
                {
                    await DeletePortWithHierarchyAsync(portToDelete.Id);
                }

                foreach (var port in slot.Ports)
                {
                    if (existingPortIds.Contains(port.Id))
                    {
                        await UpdatePortWithHierarchyAsync(port);
                    }
                    else
                    {
                        port.SlotId = slot.Id;
                        await CreatePortWithHierarchyAsync(port);
                    }
                }
            }
        }

        private async Task DeletePortWithHierarchyAsync(Guid portId)
        {
            var devices = await _unitOfWork.EndPointDevices.GetByPortIdAsync(portId);
            foreach (var device in devices)
            {
                var capabilities = await _unitOfWork.Capabilities.GetByDeviceIdAsync(device.Id);
                foreach (var capability in capabilities)
                {
                    await _unitOfWork.Capabilities.DeleteAsync(capability.Id);
                }
                await _unitOfWork.EndPointDevices.DeleteAsync(device.Id);
            }
            await _unitOfWork.Ports.DeleteAsync(portId);
        }

        private async Task CreatePortWithHierarchyAsync(PortEntity port)
        {
            var createdPort = await _unitOfWork.Ports.AddAsync(port);

            if (port.EndPointDevices != null)
            {
                foreach (var device in port.EndPointDevices)
                {
                    device.PortId = createdPort.Id;
                    var createdDevice = await _unitOfWork.EndPointDevices.AddAsync(device);

                    if (device.Capabilities != null)
                    {
                        foreach (var capability in device.Capabilities)
                        {
                            capability.EndPointDeviceId = createdDevice.Id;
                            await _unitOfWork.Capabilities.AddAsync(capability);
                        }
                    }
                }
            }
        }

        private async Task UpdatePortWithHierarchyAsync(PortEntity port)
        {
            await _unitOfWork.Ports.UpdateAsync(port);

            if (port.EndPointDevices != null)
            {
                var existingDevices = await _unitOfWork.EndPointDevices.GetByPortIdAsync(port.Id);
                var existingDeviceIds = existingDevices.Select(d => d.Id).ToList();
                var newDeviceIds = port.EndPointDevices.Select(d => d.Id).ToList();

                foreach (var deviceToDelete in existingDevices.Where(d => !newDeviceIds.Contains(d.Id)))
                {
                    await DeleteEndPointDeviceWithHierarchyAsync(deviceToDelete.Id);
                }

                foreach (var device in port.EndPointDevices)
                {
                    if (existingDeviceIds.Contains(device.Id))
                    {
                        await UpdateEndPointDeviceWithHierarchyAsync(device);
                    }
                    else
                    {
                        device.PortId = port.Id;
                        await CreateEndPointDeviceWithHierarchyAsync(device);
                    }
                }
            }
        }

        private async Task DeleteEndPointDeviceWithHierarchyAsync(Guid deviceId)
        {
            var capabilities = await _unitOfWork.Capabilities.GetByDeviceIdAsync(deviceId);
            foreach (var capability in capabilities)
            {
                await _unitOfWork.Capabilities.DeleteAsync(capability.Id);
            }
            await _unitOfWork.EndPointDevices.DeleteAsync(deviceId);
        }

        private async Task CreateEndPointDeviceWithHierarchyAsync(EndpointDeviceEntity device)
        {
            var createdDevice = await _unitOfWork.EndPointDevices.AddAsync(device);

            if (device.Capabilities != null)
            {
                foreach (var capability in device.Capabilities)
                {
                    capability.EndPointDeviceId = createdDevice.Id;
                    await _unitOfWork.Capabilities.AddAsync(capability);
                }
            }
        }

        private async Task UpdateEndPointDeviceWithHierarchyAsync(EndpointDeviceEntity device)
        {
            await _unitOfWork.EndPointDevices.UpdateAsync(device);

            if (device.Capabilities != null)
            {
                var existingCapabilities = await _unitOfWork.Capabilities.GetByDeviceIdAsync(device.Id);
                var existingCapabilityIds = existingCapabilities.Select(c => c.Id).ToList();
                var newCapabilityIds = device.Capabilities.Select(c => c.Id).ToList();

                foreach (var capabilityToDelete in existingCapabilities.Where(c => !newCapabilityIds.Contains(c.Id)))
                {
                    await _unitOfWork.Capabilities.DeleteAsync(capabilityToDelete.Id);
                }

                foreach (var capability in device.Capabilities)
                {
                    if (existingCapabilityIds.Contains(capability.Id))
                    {
                        await _unitOfWork.Capabilities.UpdateAsync(capability);
                    }
                    else
                    {
                        capability.EndPointDeviceId = device.Id;
                        await _unitOfWork.Capabilities.AddAsync(capability);
                    }
                }
            }
        }

        /// <summary>
        /// Validates whether the given string is a valid IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address string to validate</param>
        /// <returns>True if the IP address is valid, otherwise false</returns>
        private bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            // Simple regex pattern for IPv4 address
            var ipPattern = new System.Text.RegularExpressions.Regex(
                @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");

            return ipPattern.IsMatch(ipAddress);
        }

        #endregion Helper Methods

        public void Dispose()
        {
            if (!_disposed)
            {
                _unitOfWork.Dispose();
                _disposed = true;
            }
        }
    }
}