using System.Windows;
using Fetchify.Models;
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
            string url = UrlTextBox.Text.Trim();         
            string directory = DirectoryTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(directory))
            {
                WPF.MessageBox.Show("URL and directory are required.");
                return;
            }

            string fileName = GetFileNameFromUrl(url);
            string outputArg = $"--out=\"{fileName}\"";


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
                string filename = query["filename"];

                if (!string.IsNullOrWhiteSpace(filename))
                    return filename;

                return System.IO.Path.GetFileName(uri.AbsolutePath);
            }
            catch
            {
                return "unknown.file";
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
        }
    }
}
