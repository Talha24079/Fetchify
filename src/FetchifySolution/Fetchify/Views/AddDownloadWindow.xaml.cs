using Fetchify.Helpers;
using Fetchify.Models;
using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using WPF = System.Windows;

namespace Fetchify.Views
{
    public partial class AddDownloadWindow : Window
    {
        public string? DownloadUrl { get; private set; }
        public delegate void DownloadStartedHandler(DownloadItem item);
        public event DownloadStartedHandler? DownloadStarted;

        public AddDownloadWindow()
        {
            InitializeComponent();
            DirectoryTextBox.Text = SettingsManager.CurrentSettings.DefaultDownloadDirectory;
        }

        private void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text.Trim();
            string directory = DirectoryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url))
            {
                WPF.MessageBox.Show("Download URL cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                WPF.MessageBox.Show("Invalid download URL format.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                WPF.MessageBox.Show("Please select a valid target directory.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string fileName = FileNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = GetFileNameFromUrl(url);
            }


            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"file_{DateTime.Now.Ticks}.bin";
            }

            var item = new DownloadItem
            {
                Url = url,
                Directory = directory,
                FileName = fileName,
                Status = "Starting",
                Progress = 0,
                Speed = "",
                EstimatedTimeRemaining = ""
            };

            DownloadStarted?.Invoke(item);
            Close();
        }

        private string GetFileNameFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                string filename = query["filename"] ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(filename))
                    return filename;

                string extracted = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrWhiteSpace(extracted) ? "downloaded.file" : extracted;
            }
            catch
            {
                return "downloaded.file";
            }
        }

        private void BrowseDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new WinForms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    DirectoryTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public async Task SetInitialDownload(DownloadItem item)
        {
            item.FileName = await LinkNameHelper.GenerateFileNameAsync(item.Url);
            FileNameBox.Text = item.FileName;
            UrlTextBox.Text = item.Url;
            DirectoryTextBox.Text = item.Directory;
        }



    }
}
