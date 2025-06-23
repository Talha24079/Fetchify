using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Services;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WPF = System.Windows;

namespace Fetchify.Views
{
    public partial class DownloadStatusWindow : WPF.Window
    {
        private readonly ActiveDownload download;
        private readonly DispatcherTimer updateTimer;
        private readonly DispatcherTimer networkRetryTimer;

        private readonly string originalUrl;
        private readonly string originalDirectory;
        private readonly string originalFileName;

        private bool isResuming = false;

        public Action<ActiveDownload>? OnDownloadRemoved { get; set; }

        public DownloadStatusWindow(ActiveDownload activeDownload)
        {
            InitializeComponent();
            download = activeDownload;
            DataContext = download;

            originalUrl = download.Url;
            originalDirectory = download.Directory;
            originalFileName = download.FileName;

            FileNameTextBlock.Text = download.FileName;
            TotalSizeTextBlock.Text = download.TotalSize;

            PauseButton.Click += PauseButton_Click;
            ResumeButton.Click += ResumeButton_Click;
            CancelButton.Click += CancelDownload_Click;

            updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            updateTimer.Tick += UpdateUI;
            updateTimer.Start();

            networkRetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            networkRetryTimer.Tick += NetworkRetryTimer_Tick;
            networkRetryTimer.Start();
        }

        private void UpdateUI(object? sender, System.EventArgs e)
        {
            DownloadedSizeTextBlock.Text = $"{(download.Progress * ParseSizeInMB(download.TotalSize) / 100.0):F2} MB";
            ProgressBar.Value = download.Progress;
            ProgressText.Text = $"{download.Progress}%";

            StatusTextBlock.Text = download.Status;
            SpeedTextBlock.Text = download.Speed;
            EtaTextBlock.Text = download.EstimatedTimeRemaining;

            string filePath = Path.Combine(download.Directory, download.FileName);
            string tempFilePath = filePath + ".aria2";

            // ❗Fix: Only check file existence if download has actually started
            if (download.Status == "active" && download.Progress > 0 &&
                (!File.Exists(filePath) || !File.Exists(tempFilePath)))
            {
                download.Status = "error";
                ErrorMessageTextBlock.Text = "Download interrupted. File or temp file missing.";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;
                updateTimer.Stop();
            }

            if (download.Status == "error")
            {
                ErrorMessageTextBlock.Text = !NetworkHelper.IsInternetAvailable()
                    ? "Download failed: Internet is disconnected."
                    : "Download failed: Invalid link, file not found, or deleted during download.";
                ErrorMessageTextBlock.Visibility = Visibility.Visible;

                ResumeButton.Content = "Retry";
            }
            else
            {
                ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
                ResumeButton.Content = "Resume";
            }

            StatusTextBlock.Foreground = download.Status switch
            {
                "paused" => new SolidColorBrush(Colors.Orange),
                "active" => new SolidColorBrush(Colors.Green),
                "error" or "removed" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Black)
            };

            PauseButton.IsEnabled = download.Status == "active";
            ResumeButton.IsEnabled = download.Status == "paused" || download.Status == "error";
            CancelButton.IsEnabled = download.Status != "complete" && download.Status != "removed";

            if (download.Status is "complete" or "error" or "removed")
                updateTimer.Stop();
        }

        private double ParseSizeInMB(string sizeText)
        {
            if (sizeText.EndsWith("MB") && double.TryParse(sizeText.Replace("MB", "").Trim(), out double mb))
                return mb;
            return 0;
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await Aria2Helper.PauseDownloadAsync(download.Gid);
            await Task.Delay(500);

            if (result || download.Status == "paused")
            {
                download.Status = "paused";
            }
            else
            {
                WPF.MessageBox.Show("Failed to pause download.");
            }
        }

        private async void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (download.Status == "error")
            {
                await RetryDownload();
                return;
            }

            if (isResuming) return;
            isResuming = true;

            try
            {
                if (await Aria2Helper.ResumeDownloadAsync(download.Gid))
                {
                    download.Status = "active";
                }
                else
                {
                    WPF.MessageBox.Show("Failed to resume download.");
                }
            }
            finally
            {
                isResuming = false;
            }
        }

        private async Task RetryDownload()
        {
            var confirm = WPF.MessageBox.Show("Retry this failed download from the beginning?", "Retry", WPF.MessageBoxButton.YesNo);
            if (confirm != WPF.MessageBoxResult.Yes) return;

            try
            {
                string path = Path.Combine(originalDirectory, originalFileName);
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".aria2")) File.Delete(path + ".aria2");
            }
            catch { }

            var rpc = new Aria2RpcService();
            string newGid = await rpc.AddDownloadAsync(originalUrl, originalDirectory, originalFileName);

            if (!string.IsNullOrWhiteSpace(newGid) && !newGid.StartsWith("Error"))
            {
                download.Gid = newGid;
                download.Status = "Queued";
                download.Progress = 0;
                download.Speed = "0 KB/s";
                download.EstimatedTimeRemaining = "--";

                ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
                updateTimer.Start();
            }
            else
            {
                WPF.MessageBox.Show("Retry failed: " + newGid);
            }
        }

        private async void CancelDownload_Click(object sender, RoutedEventArgs e)
        {
            var result = WPF.MessageBox.Show(
                $"Are you sure you want to cancel and remove {download.FileName}?",
                "Confirm Cancel", WPF.MessageBoxButton.YesNo, WPF.MessageBoxImage.Warning);

            if (result != WPF.MessageBoxResult.Yes) return;

            bool removed = await Aria2Helper.RemoveDownloadAsync(download.Gid);
            if (removed)
            {
                updateTimer.Stop();
                OnDownloadRemoved?.Invoke(download);
                WPF.MessageBox.Show("Download cancelled and removed.");
                Close();
            }
            else
            {
                WPF.MessageBox.Show("Failed to cancel the download.");
            }
        }

        private async void NetworkRetryTimer_Tick(object? sender, EventArgs e)
        {
            if (download.Status != "error" || !NetworkHelper.IsInternetAvailable())
                return;

            networkRetryTimer.Stop();

            var rpc = new Aria2RpcService();
            string newGid = await rpc.AddDownloadAsync(originalUrl, originalDirectory, originalFileName);

            if (!string.IsNullOrWhiteSpace(newGid) && !newGid.StartsWith("Error"))
            {
                download.Gid = newGid;
                download.Status = "Queued";
                download.Progress = 0;
                download.Speed = "0 KB/s";
                download.EstimatedTimeRemaining = "--";
                updateTimer.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            updateTimer.Stop();
            networkRetryTimer.Stop();
            base.OnClosed(e);
        }
    }
}
