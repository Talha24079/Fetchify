using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fetchify.Helpers
{
    public static class LinkNameHelper
    {
        public static async Task<string> GenerateFileNameAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "downloaded_file";

            try
            {
                var decodedUrl = WebUtility.UrlDecode(url);

                // 1. Try to extract from common filename query params
                var match = Regex.Match(decodedUrl, @"[?&](?:filename|file|name|title)=([^&]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                    return SanitizeFileName(match.Groups[1].Value);

                // 2. Try from last segment of path
                var uri = new Uri(url);
                var lastSegment = Path.GetFileName(uri.LocalPath);
                if (!string.IsNullOrWhiteSpace(lastSegment) && lastSegment.Contains('.'))
                    return SanitizeFileName(lastSegment);

                // 3. Try to guess extension from MIME type
                string guessedExtension = await GuessExtensionFromMimeTypeAsync(url);
                return $"downloaded_file{guessedExtension}";
            }
            catch
            {
                return "downloaded_file";
            }
        }

        private static async Task<string> GuessExtensionFromMimeTypeAsync(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"; // Avoid 403 from some servers

                using var response = await request.GetResponseAsync();
                string contentType = response.ContentType?.ToLower() ?? "";

                return contentType switch
                {
                    "application/pdf" => ".pdf",
                    "application/zip" => ".zip",
                    "application/x-rar-compressed" => ".rar",
                    "application/octet-stream" => ".bin",
                    "application/msword" => ".doc",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                    "application/vnd.ms-excel" => ".xls",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "video/mp4" => ".mp4",
                    "audio/mpeg" => ".mp3",
                    "text/html" => ".html",
                    _ => ".bin"
                };
            }
            catch
            {
                return ".bin";
            }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
