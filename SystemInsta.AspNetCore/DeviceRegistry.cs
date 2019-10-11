using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SystemInsta.AspNetCore
{
    public interface IDeviceRegistry
    {
        void Add(Device device);
        // Verify N devices of the same maker/model/os arch/, cpu arch, framework have already uploaded symbols
        bool AreSystemImagesWanted(Device device);
    }

    public class Device
    {
        // The unique id of the device. Used to upload symbols
        public string DeviceId { get; set; }
        public string AppBuild { get; set; }
        public string AppVersion { get; set; }

        // TODO: Hash all data that makes up the device to be the "DeviceModelId"
        public string Framework { get; set; }
        public string ProcessorArchitecture { get; set; }
        public string Name { get; set; }
        public string Board { get; set; }
        public string Brand { get; set; }
        public string Manufacturer { get; set; }
        public string OSDescription { get; set; }
        public string OSArchitecture { get; set; }
    }
}
