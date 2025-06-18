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
        private readonly string _rpcUrl = "http://localhost:6800/jsonrpc";

        public Aria2RpcService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> AddDownloadAsync(string url, string directory, string fileName)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(url))
                return "Error: URL is required.";
            if (string.IsNullOrWhiteSpace(directory))
                return "Error: Directory is required.";

            // Build parameters
            var request = new
            {
                jsonrpc = "2.0",
                method = "aria2.addUri",
                id = "Fetchify",
                @params = new object[]
                {
            // Only include token if you're using one, otherwise remove it
            //"token:yourtoken", // REMOVE if you're not using a secret token in aria2.conf
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
                var response = await _httpClient.PostAsync(_rpcUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "Error: Request failed";

                if (responseBody.Contains("\"error\""))
                    return "Error: " + responseBody;

                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("result", out var resultProp))
                {
                    return resultProp.GetString(); // GID
                }

                return "Error: Unexpected response structure";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }


        public async Task<List<ActiveDownload>> GetActiveDownloadsAsync()
        {
            var rpcRequest = new
            {
                jsonrpc = "2.0",
                method = "aria2.tellActive",
                id = Guid.NewGuid().ToString(),
                @params = new object[] { }
            };

            var content = new StringContent(JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_rpcUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseString);
                if (!doc.RootElement.TryGetProperty("result", out var result)) return new List<ActiveDownload>();

                var downloads = new List<ActiveDownload>();

                foreach (var item in result.EnumerateArray())
                {
                    var fileName = item.GetProperty("files")[0].GetProperty("path").GetString();
                    var status = item.GetProperty("status").GetString();
                    var totalLength = long.Parse(item.GetProperty("totalLength").GetString());
                    var completedLength = long.Parse(item.GetProperty("completedLength").GetString());
                    var speed = long.Parse(item.GetProperty("downloadSpeed").GetString());
                    var eta = (speed > 0 && totalLength > completedLength)
                        ? $"{(totalLength - completedLength) / speed} sec"
                        : "--";
                    var progress = totalLength > 0 ? (int)((completedLength * 100) / totalLength) : 0;

                    downloads.Add(new ActiveDownload
                    {
                        Gid = item.GetProperty("gid").GetString(),
                        FileName = System.IO.Path.GetFileName(fileName),
                        Status = status,
                        Progress = progress,
                        Speed = $"{speed / 1024} KB/s",
                        EstimatedTimeRemaining = eta,
                        TotalSize = $"{totalLength / 1024 / 1024.0:F2} MB"

                    });
                }

                return downloads;
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Error fetching downloads: {ex.Message}");
                return new List<ActiveDownload>();
            }
        }
    }
}
