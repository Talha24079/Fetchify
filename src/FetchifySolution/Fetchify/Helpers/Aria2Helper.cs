using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Fetchify.Helpers
{
    public static class Aria2Helper
    {
        public static async Task<string?> StartDownloadAsync(string url, string directory, string outputFileName = "")
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("URL and directory must not be empty.");
            }

            var options = new Dictionary<string, string>
    {
        { "dir", directory }
    };

            if (!string.IsNullOrEmpty(outputFileName))
            {
                options["out"] = outputFileName;
            }

            var request = new
            {
                jsonrpc = "2.0",
                method = "aria2.addUri",
                id = "start",
                @params = new object[]
                {
            new[] { url },  // URL list
            options
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(rpcUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("result", out var resultElement))
                    {
                        return resultElement.GetString(); // GID
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartDownloadAsync Error: " + ex.Message);
                return null;
            }
        }


        public static async Task<bool> PauseDownloadAsync(string gid)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "aria2.pause",
                id = "pause",
                @params = new object[] { gid }
            };
            return await SendRequestAsync(request);
        }

        public static async Task<bool> ResumeDownloadAsync(string gid)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = "aria2.unpause",
                id = "unpause",
                @params = new object[] { gid }
            };
            return await SendRequestAsync(request);
        }

        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string rpcUrl = "http://localhost:6800/jsonrpc";

        private static async Task<bool> SendRequestAsync(object requestObject)
        {
            var json = JsonSerializer.Serialize(requestObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(rpcUrl, content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RemoveDownloadAsync(string gid)
        {
            var success = false;

            // Step 1: Try to force remove the download (active/paused)
            var forceRemoveRequest = new
            {
                jsonrpc = "2.0",
                method = "aria2.remove",
                id = "forceRemove",
                @params = new object[] { gid }
            };

            try
            {
                var forceContent = new StringContent(JsonSerializer.Serialize(forceRemoveRequest), Encoding.UTF8, "application/json");
                var forceResponse = await httpClient.PostAsync(rpcUrl, forceContent);
                success = forceResponse.IsSuccessStatusCode;
            }
            catch
            {
                // Ignore removal errors at this stage
            }

            // Step 2: Attempt to remove download result (useful if already completed or paused and in history)
            var removeResultRequest = new
            {
                jsonrpc = "2.0",
                method = "aria2.removeDownloadResult",
                id = "removeResult",
                @params = new object[] { gid }
            };

            try
            {
                var resultContent = new StringContent(JsonSerializer.Serialize(removeResultRequest), Encoding.UTF8, "application/json");
                var resultResponse = await httpClient.PostAsync(rpcUrl, resultContent);
                success = success || resultResponse.IsSuccessStatusCode;
            }
            catch
            {
                // Silent fail
            }

            return success;
        }

    }
}
