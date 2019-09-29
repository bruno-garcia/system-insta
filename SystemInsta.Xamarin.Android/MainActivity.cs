using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using Java.IO;
using Console = System.Console;

using System;
using Android.OS;
using static Android.Provider.Settings;
using Java.Interop;
using Android.Runtime;
using Android.Content.Res;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;

namespace SystemInsta.Xamarin.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private static string Tag = "SystemInsta";
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var button = FindViewById<Button>(Resource.Id.button);
            button.Click += (sender, args) =>
            {
                var androidId = Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
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
                Parallel.ForEach(paths, path =>
                {
                    if (Directory.Exists(path))
                    {
                        var fileCount = Directory.GetFiles(path).Length;
                        Log.Info(Tag, $"Path {path} has {fileCount} files to process");

                        var files = Directory.EnumerateFiles(path);
                        Log.Info(Tag, $"Enumerating {path}.");
                        foreach (var file in files)
                        {
                            Log.Info(Tag, $"File: {file}.");
                        }
                    }
                    else
                    {
                        Log.Warn(Tag, $"Path {path} doesn't exist.");
                    }
                });
            };
        }
    }
}