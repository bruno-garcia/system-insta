using System;
using System.Diagnostics;

namespace SystemInsta.Tizen
{
    public static class TizenImageUploader
    {
        public static async void UploadSystemImages(object sender, EventArgs e)
        {
            var logger = new LoggerAdapter<SystemImageUploader>();
            using (var uploader = new SystemImageUploader(logger: logger))
            {
                try
                {
                    await uploader.Run("/usr/lib");
                }
                catch (Exception ex)
                {
                    global::Tizen.Log.Error(nameof(TizenImageUploader), ex.ToString());
                    Debug.WriteLine("Blew up: " + ex);
                    throw;
                }
            }
        }
    }
}
