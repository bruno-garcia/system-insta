﻿using Tizen.Wearable.CircularUI.Forms;
using Xamarin.Forms;

namespace SystemInsta.Tizen.Wearable
{
    public class App : Application
    {
        public App()
        {
            var button = new Button
            {
                Text = "Share you system images!",
            };
            button.Clicked += TizenImageUploader.UploadSystemImages;

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
