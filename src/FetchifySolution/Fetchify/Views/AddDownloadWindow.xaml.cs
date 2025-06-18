using System;
using System.IO;
using System.Windows;
using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Services;
using WinForms = System.Windows.Forms;
using WPF = System.Windows;

namespace Fetchify.Views
{
    public partial class AddDownloadWindow : Window
    {
        public string DownloadUrl { get; private set; }
        public delegate void DownloadStartedHandler(DownloadItem item);
        public event DownloadStartedHandler DownloadStarted;
        public AddDownloadWindow()
        {
            InitializeComponent();
        }

        private void StartDownload_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text.Trim();         // Make sure this is not empty
            string directory = DirectoryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(directory))
            {
                WPF.MessageBox.Show("URL and directory are required.");
                return;
            }

            string fileName = System.IO.Path.GetFileName(new Uri(url).AbsolutePath);
            string outputArg = $"--out=\"{fileName}\"";

            // This is only to call aria2c directly; not needed if using RPC
            // Aria2Helper.StartDownload(url, directory, outputArg);

            var item = new DownloadItem
            {
                Url = url,               // Make sure this exists in the model
                Directory = directory,   // Make sure this exists in the model
                FileName = fileName,
                Status = "Starting",
                Progress = 0,
                Speed = "",
                EstimatedTimeRemaining = ""
            };

            DownloadStarted?.Invoke(item);  // Must match delegate (object sender, DownloadItem item)
            Close();
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
        }
    }
}
