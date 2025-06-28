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
            // ✅ Always add CORS headers for any request type
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Headers", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }


            try
            {
                using var reader = new StreamReader(request.InputStream);
                string body = await reader.ReadToEndAsync();

                // ✅ Handle Blob-based download
                if (context.Request.Url.AbsolutePath == "/api/download/blob")
                {
                    var blobData = JsonSerializer.Deserialize<BlobDownloadRequest>(body, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (blobData != null && !string.IsNullOrWhiteSpace(blobData.Data))
                    {
                        byte[] bytes = Convert.FromBase64String(blobData.Data);
                        string fileName = string.IsNullOrWhiteSpace(blobData.FileName)
                            ? DetectExtensionAndGenerateFileName(bytes)
                            : blobData.FileName;

                        string downloadDir = SettingsManager.CurrentSettings.DefaultDownloadDirectory;
                        string filePath = Path.Combine(downloadDir, fileName);

                        await File.WriteAllBytesAsync(filePath, bytes);

                        WPF.Application.Current.Dispatcher.Invoke(() =>
                        {
                            DownloadManager.Downloads.Add(new ActiveDownload
                            {
                                FileName = fileName,
                                Directory = downloadDir,
                                Progress = 100,
                                Status = "complete",
                                TotalSize = $"{bytes.Length / 1024f / 1024f:F2} MB",
                                Speed = "0 KB/s",
                                EstimatedTimeRemaining = "--"
                            });
                        });

                        response.StatusCode = 200;
                        await using var writer = new StreamWriter(response.OutputStream);
                        await writer.WriteAsync("Blob received");
                        response.Close();
                        return;
                    }

                    response.StatusCode = 400;
                    await using (var writer = new StreamWriter(response.OutputStream))
                    {
                        await writer.WriteAsync("Invalid blob payload");
                    }
                    response.Close();
                    return;
                }

                // ✅ Handle Normal URL-based downloads
                var data = JsonSerializer.Deserialize<DownloadRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null || string.IsNullOrWhiteSpace(data.Url))
                {
                    response.StatusCode = 400;
                    await using var invalidWriter = new StreamWriter(response.OutputStream);
                    await invalidWriter.WriteAsync("Invalid URL payload");
                    response.Close();
                    return;
                }

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
                    addWindow.Topmost = true;
                    addWindow.Activate();

                    if (WPF.Application.Current.MainWindow is MainWindow mainWin)
                    {
                        addWindow.DownloadStarted += mainWin.StartDownload;
                    }
                    else
                    {
                        WPF.MessageBox.Show("Main window not found. Cannot start download.");
                    }

                    addWindow.ShowDialog();
                });

                response.StatusCode = 200;
                await using var okWriter = new StreamWriter(response.OutputStream);
                await okWriter.WriteAsync("Download received");
                response.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HttpServer error: " + ex.Message);
                response.StatusCode = 500;
                await using var errorWriter = new StreamWriter(response.OutputStream);
                await errorWriter.WriteAsync("Internal Server Error");
                response.Close();
            }
        }

        private static string DetectExtensionAndGenerateFileName(byte[] bytes)
        {
            string extension = ".bin";

            if (bytes.Length >= 4)
            {
                if (bytes[0] == 0x25 && bytes[1] == 0x50) extension = ".pdf";         // %PDF
                else if (bytes[0] == 0x89 && bytes[1] == 0x50) extension = ".png";     // PNG
                else if (bytes[0] == 0xFF && bytes[1] == 0xD8) extension = ".jpg";     // JPG/JPEG
                else if (bytes[0] == 0x47 && bytes[1] == 0x49) extension = ".gif";     // GIF
                else if (bytes[0] == 0x42 && bytes[1] == 0x4D) extension = ".bmp";     // BMP
                else if (bytes[0] == 0x50 && bytes[1] == 0x4B) extension = ".zip";     // ZIP, DOCX, etc.
                else if (bytes[0] == 0x1F && bytes[1] == 0x8B) extension = ".gz";      // GZIP
                else if (bytes[0] == 0x4D && bytes[1] == 0x5A) extension = ".exe";     // EXE
            }

            return $"download_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        }

        private class BlobDownloadRequest
        {
            public string FileName { get; set; } = "unknown_file";
            public string Data { get; set; } = "";
        }

        private class DownloadRequest
        {
            public string Url { get; set; } = "";
        }
    }
}
