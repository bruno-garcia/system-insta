using System.IO;

namespace SystemInsta.AspNetCore
{
    public interface ISystemImageRegistry
    {
        bool SymbolsWanted(string debugId);
        void AddSystemImage(SystemImage image);
    }

    public class SystemImage
    {
        // Expected to be 'registered' in the backend before receiving data
        public string DeviceId { get; set; }
        public string DebugId { get; set; }
        public string Hash { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool HasUnwindInformation { get; set; }
        public bool HasDebugInformation { get; set; }
        public Stream Data { get; set; }
    }
}

// PE, PDBs etc also have unwind info and or debug info? What has debugId?
