using System;
using System.Threading.Tasks;
using static System.Console;

namespace SystemInsta.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            WriteLine("Starting...");

            var paths = new (string path, bool includeSubDirectories)[]
            {
                ("/lib/", true), ("/usr/lib/", true), ("/usr/local/lib", true)
            };

            var logger = new LoggerAdapter<SystemImageUploader>();
            using var uploader = new SystemImageUploader(logger: logger);
            foreach (var (path, includeSubDirectories) in paths)
            {
                try
                {
                    await uploader.Run(path, includeSubDirectories);
                }
                catch (Exception e)
                {
                    WriteLine($"Failed: {e}");
                }
            }
        }
    }
}