﻿using Device.Net.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Device.Net
{
    public class DeviceManager : IDeviceManager
    {
        //TODO: Put logging in here

        #region Fields
        private readonly ILoggerFactory _loggerFactory;
        #endregion

        #region Public Properties
        public const string ObsoleteMessage = "This method will soon be removed. Create an instance of DeviceManager and register factories there";
        public List<IDeviceFactory> DeviceFactories { get; } = new List<IDeviceFactory>();
        public bool IsInitialized => DeviceFactories.Count > 0;
        #endregion

        #region Constructor
        public DeviceManager(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        #endregion

        #region Public Methods
        public async Task<IEnumerable<ConnectedDeviceDefinition>> GetConnectedDeviceDefinitionsAsync(FilterDeviceDefinition deviceDefinition)
        {
            if (DeviceFactories.Count == 0) throw new DeviceFactoriesNotRegisteredException();

            var retVal = new List<ConnectedDeviceDefinition>();
            foreach (var deviceFactory in DeviceFactories)
            {
                var connectedDeviceDefinitions = await deviceFactory.GetConnectedDeviceDefinitionsAsync(deviceDefinition);

                foreach (var connectedDeviceDefinition in connectedDeviceDefinitions)
                {
                    //Don't add the same device twice
                    //Note: this probably won't cause issues where there is no DeviceId, but funny behaviour is probably going on when there isn't anyway...
                    if (retVal.Select(d => d.DeviceId).Contains(connectedDeviceDefinition.DeviceId)) continue;

                    retVal.Add(connectedDeviceDefinition);
                }
            }

            return retVal;
        }

        //TODO: Duplicate code here...
        public IDevice GetDevice(ConnectedDeviceDefinition connectedDeviceDefinition)
        {
            if (connectedDeviceDefinition == null) throw new ArgumentNullException(nameof(connectedDeviceDefinition));

            foreach (var deviceFactory in DeviceFactories.Where(deviceFactory => !connectedDeviceDefinition.DeviceType.HasValue || (deviceFactory.DeviceType == connectedDeviceDefinition.DeviceType)))
            {
                return deviceFactory.GetDevice(connectedDeviceDefinition);
            }

            throw new DeviceException(Messages.ErrorMessageCouldntGetDevice);
        }

        public async Task<List<IDevice>> GetDevicesAsync(IList<FilterDeviceDefinition> deviceDefinitions)
        {
            if (deviceDefinitions == null) throw new ArgumentNullException(nameof(deviceDefinitions), $"{nameof(GetConnectedDeviceDefinitionsAsync)} can be used to enumerate all devices without specifying definitions.");

            var retVal = new List<IDevice>();

            foreach (var deviceFactory in DeviceFactories)
            {
                foreach (var filterDeviceDefinition in deviceDefinitions)
                {
                    if (filterDeviceDefinition.DeviceType.HasValue && (deviceFactory.DeviceType != filterDeviceDefinition.DeviceType)) continue;

                    var connectedDeviceDefinitions = await deviceFactory.GetConnectedDeviceDefinitionsAsync(filterDeviceDefinition);
                    retVal.AddRange
                    (
                        connectedDeviceDefinitions.Select
                        (
                            connectedDeviceDefinition => deviceFactory.GetDevice(connectedDeviceDefinition)
                        ).
                        Where
                        (
                            device => device != null
                        )
                    );
                }
            }

            return retVal;
        }
        #endregion

        #region Public Static Methods
        public static bool IsDefinitionMatch(FilterDeviceDefinition filterDevice, ConnectedDeviceDefinition actualDevice)
        {
            if (actualDevice == null) throw new ArgumentNullException(nameof(actualDevice));

            if (filterDevice == null) return true;

            var vendorIdPasses = !filterDevice.VendorId.HasValue || filterDevice.VendorId == actualDevice.VendorId;
            var productIdPasses = !filterDevice.ProductId.HasValue || filterDevice.ProductId == actualDevice.ProductId;
            var deviceTypePasses = !filterDevice.DeviceType.HasValue || filterDevice.DeviceType == actualDevice.DeviceType;
            var usagePagePasses = !filterDevice.UsagePage.HasValue || filterDevice.UsagePage == actualDevice.UsagePage;

            var returnValue =
                vendorIdPasses &&
                productIdPasses &&
                deviceTypePasses &&
                usagePagePasses;

            return returnValue;
        }
        #endregion
    }
}
