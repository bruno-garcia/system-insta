using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Support.V7.App;
using Android.Util;
using Android.Widget;

namespace SystemInsta.Xamarin.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private static readonly string Tag = "SystemInsta";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var button = FindViewById<Button>(Resource.Id.button);
            button.Click += (sender, args) =>
            {
                DebugSystemInformation();

                Task.Run(async () =>
                {
                    var paths = new[] {"/system/lib", "/system/lib64", "/system/"};
                    var logger = new LoggerAdapter<SystemImageUploader>();
                    var uploader = new SystemImageUploader(logger: logger);

                    foreach (var path in paths)
                    {
                        try
                        {
                            await uploader.Run(path);
                        }
                        catch (Exception e)
                        {
                            Log.Error(Tag, $"Failed: {e}");
                        }
                    }
                });
            };
        }

        private static void DebugSystemInformation()
        {
            var androidId = Settings.Secure.GetString(Application.Context.ContentResolver, Settings.Secure.AndroidId);
            var framework = RuntimeInformation.FrameworkDescription;
            var procArch = RuntimeInformation.ProcessArchitecture;
            var deviceName =
                global::Android.Provider.Settings.System.GetString(Application.Context.ContentResolver, "device_name");
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
        }
    }
}