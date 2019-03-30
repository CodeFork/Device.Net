﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Device.Net.UnitTests
{
    public abstract class MockFactoryBase : IDeviceFactory
    {
        public abstract bool IsConnected { get; }

        public abstract string DeviceId { get; }

        public abstract DeviceType DeviceType { get; }

        public ILogger Logger { get; protected set; }

        public abstract uint ProductId { get; }
        public abstract uint VendorId { get; }

        public Task<IEnumerable<ConnectedDeviceDefinition>> GetConnectedDeviceDefinitionsAsync(FilterDeviceDefinition deviceDefinition)
        {
            var result = new List<ConnectedDeviceDefinition>();

            if (IsConnected)
            {
                Console.WriteLine("I'm saying the device is connected...");

                result.Add(new ConnectedDeviceDefinition(DeviceId) { ProductId = ProductId, VendorId = VendorId });
            }
            else
            {
                Console.WriteLine("I'm saying no device...");
            }

            return Task.FromResult<IEnumerable<ConnectedDeviceDefinition>>(result);
        }

        public abstract IDevice GetDevice(ConnectedDeviceDefinition deviceDefinition);
    }
}