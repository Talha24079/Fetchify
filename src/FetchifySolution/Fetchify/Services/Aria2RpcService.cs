using Fetchify.Helpers;
using Fetchify.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WPF = System.Windows;

namespace Fetchify.Services
{
    public class Aria2RpcService
    {
        private readonly HttpClient _httpClient;
        private string RpcUrl => $"http://{SettingsManager.CurrentSettings.Aria2RpcHost}:{SettingsManager.CurrentSettings.Aria2RpcPort}/jsonrpc";

        private static bool _activeErrorShown = false;
        private static bool _waitingErrorShown = false;
        private static bool _stoppedErrorShown = false;

        public Aria2RpcService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15) // Step 1: Limit timeout to prevent 100-second hangs
            };
        }

        public async Task<string> AddDownloadAsync(string url, string directory, string fileName)
        {
            if (string.IsNullOrWhiteSpace(url)) return "Error: URL is required.";
            if (string.IsNullOrWhiteSpace(directory)) return "Error: Directory is required.";

            var request = new
            {
                jsonrpc = "2.0",
                method = "aria2.addUri",
                id = "Fetchify",
                @params = new object[]
                {
                    new string[] { url },
                    new Dictionary<string, string>
                    {
                        { "dir", directory },
                        { "out", fileName }
                    }
                }
            };

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(RpcUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "Error: Request failed";

                if (responseBody.Contains("\"error\""))
                    return "Error: " + responseBody;

                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("result", out var resultProp))
                    return resultProp.GetString();

                return "Error: Unexpected response structure";
            }
            catch (TaskCanceledException ex)
            {
                return "Error: Request timed out. Check connection or aria2 status.";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error adding download: " + ex.Message);
                return "Error: " + ex.Message;
            }
        }

        public async Task<List<ActiveDownload>> GetActiveDownloadsAsync() => await GetDownloadsByMethod("aria2.tellActive");
        public async Task<List<ActiveDownload>> GetAllDownloadsAsync()
        {
            var allDownloads = new List<ActiveDownload>();
            allDownloads.AddRange(await GetDownloadsByMethod("aria2.tellActive"));
            allDownloads.AddRange(await GetDownloadsByMethod("aria2.tellWaiting"));
            allDownloads.AddRange(await GetDownloadsByMethod("aria2.tellStopped"));
            return allDownloads;
        }

        private async Task<List<ActiveDownload>> GetDownloadsByMethod(string method)
        {
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = method,
                id = Guid.NewGuid().ToString(),
                @params = method switch
                {
                    "aria2.tellStopped" => new object[] { 0, 1000 },
                    "aria2.tellWaiting" => new object[] { 0, 1000 },
                    _ => new object[] { }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(RpcUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                if (!doc.RootElement.TryGetProperty("result", out var result))
                    return new List<ActiveDownload>();

                var downloads = new List<ActiveDownload>();

                foreach (var item in result.EnumerateArray())
                {
                    var fileName = item.GetProperty("files")[0].GetProperty("path").GetString();
                    var status = item.GetProperty("status").GetString();
                    var totalLength = long.Parse(item.GetProperty("totalLength").GetString());
                    var completedLength = long.Parse(item.GetProperty("completedLength").GetString());
                    var speed = long.Parse(item.GetProperty("downloadSpeed").GetString());
                    var progress = totalLength > 0 ? (int)((completedLength * 100) / totalLength) : 0;

                    string eta = "--";
                    if (speed > 0 && totalLength > completedLength)
                    {
                        var etaSeconds = (totalLength - completedLength) / speed;
                        eta = etaSeconds switch
                        {
                            >= 3600 => $"{etaSeconds / 3600}h {(etaSeconds % 3600) / 60}m",
                            >= 60 => $"{etaSeconds / 60}m {etaSeconds % 60}s",
                            > 0 => $"{etaSeconds}s",
                            _ => "--"
                        };
                    }

                    string formattedSpeed = speed switch
                    {
                        >= 1024 * 1024 => $"{speed / 1024 / 1024.0:F2} MB/s",
                        >= 1024 => $"{speed / 1024.0:F2} KB/s",
                        _ => $"{speed} B/s"
                    };

                    downloads.Add(new ActiveDownload
                    {
                        Gid = item.GetProperty("gid").GetString(),
                        FileName = System.IO.Path.GetFileName(fileName),
                        Status = status,
                        Progress = progress,
                        Speed = formattedSpeed,
                        EstimatedTimeRemaining = eta,
                        TotalSize = $"{totalLength / 1024 / 1024.0:F2} MB"
                    });

                }

                return downloads;
            }
            catch (TaskCanceledException ex)
            {
                ShowOnce(method, $"Request timed out while calling {method}. Check your network or Aria2 server.");
            }
            catch (Exception ex)
            {
                ShowOnce(method, $"Error fetching downloads ({method}): {ex.Message}");
            }

            return new List<ActiveDownload>();
        }

        private void ShowOnce(string method, string message)
        {
            if (method == "aria2.tellActive" && !_activeErrorShown)
            {
                _activeErrorShown = true;
                WPF.MessageBox.Show(message);
            }
            else if (method == "aria2.tellWaiting" && !_waitingErrorShown)
            {
                _waitingErrorShown = true;
                WPF.MessageBox.Show(message);
            }
            else if (method == "aria2.tellStopped" && !_stoppedErrorShown)
            {
                _stoppedErrorShown = true;
                WPF.MessageBox.Show(message);
            }
            else
            {
                Console.WriteLine($"Silent error ({method}): {message}");
            }
        }

        private async Task<bool> SendRpc(object requestObj)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestObj), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(RpcUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendRpc failed: " + ex.Message);
                return false;
            }
        }

        public async Task<ActiveDownload?> GetDownloadStatusAsync(string gid)
        {
            var downloads = await GetAllDownloadsAsync();
            return downloads.FirstOrDefault(d => d.Gid == gid);
        }

    }
}
