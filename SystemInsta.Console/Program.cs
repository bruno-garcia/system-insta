using System.Threading.Tasks;

namespace SystemInsta.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Doing some work.");
            await Task.Run(async () =>
            {
                var paths = new[] {"/sbin/"};
                var uploader = new SystemImageUploader();

                foreach (var path in paths)
                {
                    try
                    {
                        await uploader.Run(path);
                    }
                    catch (System.Exception e)
                    {
                        System.Console.WriteLine($"Failed: {e}");
                    }
                }
            });
        }
    }

    class Logger : ILogger
    {

    }
}