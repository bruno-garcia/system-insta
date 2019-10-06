using System;
using System.Diagnostics;
using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;

namespace SystemInsta.Tizen.Wearable
{
    public class App : Application
    {
        public App()
        {
            Button button = new Button
            {
                Text = "Share you system images!",
            };
            button.Clicked += UploadSystemImages;

            MainPage = new CirclePage
            {
                Content = new StackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Children = {
                        button
                    }
                }
            };
        }

        private async void UploadSystemImages(object sender, EventArgs e)
        {
            var logger = new LoggerAdapter<SystemImageUploader>();
            var uploader = new SystemImageUploader(
                new Uri("http://sentry.garcia.in.eu.ngrok.io/image"),
                logger: logger);
            try
            {
                await uploader.Run("/usr/lib");
            }
            catch (Exception ex)
            {
                global::Tizen.Log.Error("App", ex.ToString());
                Debug.WriteLine("Blew up: " + ex);
                throw;
            }
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
