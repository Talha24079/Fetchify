using Fetchify.Helpers;
using Fetchify.Models;
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
        public Action<ActiveDownload>? OnDownloadRemoved { get; set; }
        public DownloadStatusWindow(ActiveDownload activeDownload)
        {
            InitializeComponent();
            this.download = activeDownload;
            DataContext = this.download;

            // Set initial values
            FileNameTextBlock.Text = download.FileName;
            TotalSizeTextBlock.Text = download.TotalSize;

            updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            updateTimer.Tick += UpdateUI;
            updateTimer.Start();
        }


        private void UpdateUI(object sender, System.EventArgs e)
        {
            // Calculate downloaded size from progress and total size
            DownloadedSizeTextBlock.Text = $"{(download.Progress * ParseSizeInMB(download.TotalSize) / 100.0):F2} MB";

            // Update progress bar
            ProgressBar.Value = download.Progress;
            ProgressText.Text = $"{download.Progress}%";

            // Update status, speed, and ETA
            StatusTextBlock.Text = download.Status;
            SpeedTextBlock.Text = download.Speed;
            EtaTextBlock.Text = download.EstimatedTimeRemaining;

            // Optional: Color status text
            switch (download.Status)
            {
                case "paused":
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                    break;
                case "active":
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                    break;
                case "error":
                case "removed":
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    break;
                default:
                    StatusTextBlock.Foreground = new SolidColorBrush(Colors.Black);
                    break;
            }

            // Stop refreshing if download is final
            if (download.Status == "complete" || download.Status == "error" || download.Status == "removed")
            {
                updateTimer.Stop();
            }
        }



        private double ParseSizeInMB(string sizeText)
        {
            if (sizeText.EndsWith("MB") && double.TryParse(sizeText.Replace("MB", "").Trim(), out double mb))
                return mb;
            return 0;
        }

        private async void PauseButton_Click(object sender, WPF.RoutedEventArgs e)
        {
            bool success = await Aria2Helper.PauseDownloadAsync(download.Gid);
            if (success)
            {
                download.Status = "paused";
            }
            else
            {
                WPF.MessageBox.Show("Failed to pause download.");
            }
        }

        private async void ResumeButton_Click(object sender, WPF.RoutedEventArgs e)
        {
            bool success = await Aria2Helper.ResumeDownloadAsync(download.Gid);
            if (success)
            {
                download.Status = "active";
            }
            else
            {
                WPF.MessageBox.Show("Failed to resume download.");
            }
        }

        private async void CancelDownload_Click(object sender, RoutedEventArgs e)
        {
            var result = WPF.MessageBox.Show(
                $"Are you sure you want to cancel and remove {download.FileName}?",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            bool removed = await Aria2Helper.RemoveDownloadAsync(download.Gid);
            await Aria2Helper.RemoveDownloadAsync(download.Gid); // Always try cleanup

            if (removed)
            {
                OnDownloadRemoved?.Invoke(download); // Notify MainWindow
                updateTimer.Stop();
                WPF.MessageBox.Show("Download cancelled and removed.");
                this.Close();
            }
            else
            {
                WPF.MessageBox.Show("Failed to cancel the download.");
            }
        }


    }

}
