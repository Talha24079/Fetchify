using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Views;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Windows;
using WPF = System.Windows;

namespace Fetchify.Services
{
    public static class HttpServer
    {
        private static HttpListener? listener;

        public static void Start()
        {
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://localhost:12345/api/download/");
                listener.Start();
                listener.BeginGetContext(OnRequestReceived, null);
                Console.WriteLine("Fetchify HTTP listener started on http://localhost:12345/api/download/");
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show("Failed to start Fetchify listener: " + ex.Message);
            }
        }

        private static async void OnRequestReceived(IAsyncResult result)
        { 
            if (listener == null || !listener.IsListening) return;

            HttpListenerContext context = listener.EndGetContext(result);
            listener.BeginGetContext(OnRequestReceived, null); // Keep listening

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                using var reader = new StreamReader(request.InputStream);
                string body = await reader.ReadToEndAsync();

                var data = JsonSerializer.Deserialize<DownloadRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (data == null)
                {
                    WPF.MessageBox.Show("Data is null");
                }
                else if (string.IsNullOrWhiteSpace(data.Url))
                {
                    WPF.MessageBox.Show("URL is missing in request");
                }


                if (data != null && !string.IsNullOrWhiteSpace(data.Url))
                {
                    WPF.Application.Current.Dispatcher.Invoke(() =>
                    {
                        

                        var item = new DownloadItem
                        {
                            Url = data.Url,
                            FileName = Path.GetFileName(new Uri(data.Url).LocalPath),
                            Directory = SettingsManager.CurrentSettings.DefaultDownloadDirectory
                        };


                        var addWindow = new AddDownloadWindow();
                        addWindow.SetInitialDownload(item);
                        addWindow.Owner = WPF.Application.Current.MainWindow;
                        addWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                        // 👇 These two lines make sure it stays on top
                        addWindow.Topmost = true;
                        addWindow.Activate();  // Ensures it grabs focus

                        // Subscribe to event
                        addWindow.DownloadStarted += async download =>
                        {
                            var rpc = new Aria2RpcService();
                            string result = await rpc.AddDownloadAsync(download.Url, download.Directory, download.FileName);

                            if (result.StartsWith("Error"))
                                WPF.MessageBox.Show("Download failed: " + result);
                            else
                                WPF.MessageBox.Show("✅ Download added successfully!");
                        };

                        addWindow.ShowDialog();

                    });



                    response.StatusCode = 200;
                    await using var writer = new StreamWriter(response.OutputStream);
                    await writer.WriteAsync("Received");
                }
                else
                {
                    response.StatusCode = 400;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
            }

            response.Close();
        }

        private class DownloadRequest
        {
            public string Url { get; set; } = "";
        }
    }
}
