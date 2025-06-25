using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fetchify.Helpers
{
    public static class Aria2Helper
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string rpcUrl = "http://localhost:6800/jsonrpc";

        public static async Task<string?> StartDownloadAsync(string url, string directory, string outputFileName = "")
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("URL and directory must not be empty.");

            var options = new Dictionary<string, string> { { "dir", directory } };
            if (!string.IsNullOrEmpty(outputFileName))
                options["out"] = outputFileName;

            var request = new
            {
                jsonrpc = "2.0",
                method = "aria2.addUri",
                id = "start",
                @params = new object[]
                {
                    new[] { url },
                    options
                }
            };

            try
            {
                var response = await httpClient.PostAsync(rpcUrl, Serialize(request));
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("result", out var result))
                        return result.GetString();
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
            return await SendRequestExpectingResult("aria2.pause", gid);
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

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(rpcUrl, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return false;

                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("error", out _))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> RemoveDownloadAsync(string gid)
        {
            bool removed = await SendRequestExpectingResult("aria2.remove", gid);

            // Regardless of success, try to remove the result
            await SendRequestExpectingResult("aria2.removeDownloadResult", gid);

            // Check if download is still present (e.g. in active/waiting/stopped)
            bool stillExists = await DownloadStillExistsAsync(gid);

            return removed || !stillExists; // Success if either removal worked or it’s already gone
        }

        private static async Task<bool> SendRequestExpectingResult(string method, string gid)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = method,
                id = method,
                @params = new object[] { gid }
            };

            try
            {
                var response = await httpClient.PostAsync(rpcUrl, Serialize(request));
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return false;

                using var doc = JsonDocument.Parse(responseBody);
                return doc.RootElement.TryGetProperty("result", out _);
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> DownloadStillExistsAsync(string gid)
        {
            var checkMethods = new[] { "aria2.tellActive", "aria2.tellWaiting", "aria2.tellStopped" };

            foreach (var method in checkMethods)
            {
                var request = new
                {
                    jsonrpc = "2.0",
                    method = method,
                    id = method,
                    @params = method == "aria2.tellStopped" || method == "aria2.tellWaiting"
                        ? new object[] { 0, 1000 }
                        : Array.Empty<object>()
                };

                try
                {
                    var response = await httpClient.PostAsync(rpcUrl, Serialize(request));
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        continue;

                    using var doc = JsonDocument.Parse(responseBody);
                    if (!doc.RootElement.TryGetProperty("result", out var result))
                        continue;

                    foreach (var item in result.EnumerateArray())
                    {
                        if (item.TryGetProperty("gid", out var gidProp) && gidProp.GetString() == gid)
                            return true;
                    }
                }
                catch
                {
                    // If one check fails, try the others
                    continue;
                }
            }

            return false; // GID not found in any list
        }

        private static StringContent Serialize(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }


    }
}
