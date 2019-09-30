using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Support.V7.App;
using Android.Util;
using Android.Widget;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;
using Exception = Java.Lang.Exception;
using StringBuilder = Java.Lang.StringBuilder;

namespace SystemInsta.Xamarin.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private HttpClient _client;
        private const string backendUrl = "http://sentry.garcia.in.eu.ngrok.io/image";

        private static string Tag = "SystemInsta";
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _client = new HttpClient();

            var button = FindViewById<Button>(Resource.Id.button);
            button.Click += (sender, args) =>
            {
                var androidId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
                var framework = RuntimeInformation.FrameworkDescription;
                var procArch = RuntimeInformation.ProcessArchitecture;
                var deviceName = global::Android.Provider.Settings.System.GetString(Application.Context.ContentResolver, "device_name");
                var board = Build.Board;
                var brand = Build.Brand;
                var manufacturer = Build.Manufacturer;
                var osDesc = RuntimeInformation.OSDescription;
                var osArch = RuntimeInformation.OSArchitecture;
                string versionName;
                int versionCode;
                using (var info = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName,
                    PackageInfoFlags.MetaData))
                {
                    versionName = info.VersionName;
                    versionCode = info.VersionCode;
                }

                Log.Info(Tag, $@"Android Id: {androidId}
Framework: '{framework}'
Processor Architecture: {procArch}'
Device Name: {deviceName}'
Board: {board}'
Brand: {brand}'
Manufacturer: {manufacturer}'
Processor Architecture: {procArch}'
OS Description: {osDesc}'
OS Architecture: {osArch}'
App Version: {versionName}'
App Build: {versionCode}'");

                var paths = new[] {"/system/lib", "/system/lib64", "/system/"};
                Parallel.ForEach(paths, path => Run(path)
                    .ContinueWith(l =>
                    {
                        if (l.IsFaulted)
                        {
                            Log.Error(Tag, $"Failed: {l.Exception}");
                        }
                    }));
            };
        }

        private async Task Run(string path)
        {
            if (!Directory.Exists(path))
            {
                Log.Warn(Tag, $"Path {path} doesn't exist.");
                return;
            }

            var files = Directory.GetFiles(path);
            Log.Info(Tag, $"Path {path} has {files.Length} files to process");

            foreach (var file in files.AsParallel())
            {
                Log.Info(Tag, $"File: {file}.");
                if (!ELFReader.TryLoad(file, out var elf))
                {
                    Log.Warn(Tag, $"Couldn't load': {file} with ELF reader.");
                    continue;
                }

                var hasBuildId = elf.TryGetSection(".note.gnu.build-id", out var buildId);
                if (!hasBuildId)
                {
                    Log.Warn(Tag, $"No Debug Id in {file}");
                    continue;
                }

                var hasUnwindingInfo = elf.TryGetSection(".eh_frame", out _);
                var hasDwarfDebugInfo = elf.TryGetSection(".debug_frame", out _);

                if (!hasUnwindingInfo && !hasDwarfDebugInfo)
                {
                    Log.Warn(Tag, $"No unwind nor DWARF debug info in {file}");
                    continue;
                }

                try
                {
                    await ProcessFile(file, hasUnwindingInfo, hasDwarfDebugInfo, buildId, elf);
                }
                catch (Exception e)
                {
                    Log.Error(Tag, $"Failed sending file. {e}");
                }
            }
        }

        private async Task ProcessFile(string file, bool hasUnwindingInfo, bool hasDwarfDebugInfo, ISection buildId, IELF elf)
        {
            Log.Info(Tag, $"Contains unwinding info: {hasUnwindingInfo}");
            Log.Info(Tag, $"Contains DWARF debug info: {hasDwarfDebugInfo}");

            var builder = new StringBuilder();
            var bytes = buildId.GetContents().Skip(16);

            foreach (var @byte in bytes)
            {
                builder.Append(@byte.ToString("x2"));
            }

            var buildIdHex = builder.ToString();

            if (!(await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, backendUrl)
                { Headers = {{"debug-id", buildIdHex}} }))
                .IsSuccessStatusCode)
            {
                return;
            }

            // Better would be if `ELF` class would expose its buffer so we don't need to read the file twice.
            // Ideally ELF would read headers as a stream which we could reset to 0 after reading heads
            // and ensuring it's what we need.
            using (var fileStream = File.OpenRead(file))
            {
                var postResult = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, backendUrl)
                {
                    Headers = {{"debug-id", buildIdHex}},
                    Content = new MultipartFormDataContent(
                        // TODO: add a proper boundary
                        $"Upload----WebKitFormBoundary7MA4YWxkTrZu0gW--")
                    {
                        {new StreamContent(fileStream), file}
                    }
                });

                if (!postResult.IsSuccessStatusCode)
                {
                    Log.Error(Tag, $"{postResult.StatusCode} for file {file}");
                    if (postResult.Headers.TryGetValues("X-Error-Code", out var code))
                    {
                        Log.Error(Tag, $"Code: {code}");
                    }
                }
                else
                {
                    Log.Info(Tag, $"Sent file: {file}");
                }
            }

#if DEBUG
            Log.Info(Tag, $"Build Id: {buildIdHex}");

            Log.Info(Tag, $"Class: {elf.Class}.");
            Log.Info(Tag, $"HasSectionsStringTable: {elf.HasSectionsStringTable}.");
            Log.Info(Tag, $"HasSectionHeader: {elf.HasSectionHeader}.");
            foreach (var section in elf.Sections)
            {
                Log.Info(Tag, $"Section: {section}.");
            }

            Log.Info(Tag, $"HasSegmentHeader: {elf.HasSegmentHeader}.");
            foreach (var segment in elf.Segments)
            {
                Log.Info(Tag, $"Segment: {segment}.");
            }

            Log.Info(Tag, $"Endianess: {elf.Endianess}.");
            Log.Info(Tag, $"Machine: {elf.Machine}.");
            Log.Info(Tag, $"Type: {elf.Type}.");
#endif
        }
    }
}