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

        private int stalledCounter = 0;
        private int lastProgress = -1;

        private bool isResuming = false;
        private bool isPausing = false; // Add this to prevent multiple pause operations

        public Action<ActiveDownload>? OnDownloadRemoved { get; set; }

        public DownloadStatusWindow(ActiveDownload activeDownload)
        {
            InitializeComponent();

            download = activeDownload ?? throw new ArgumentNullException(nameof(activeDownload));
            DataContext = download;

            originalUrl = download.Url ?? "";
            originalDirectory = download.Directory ?? "";
            originalFileName = download.FileName ?? "";

            FileNameTextBlock.Text = originalFileName;
            TotalSizeTextBlock.Text = download.TotalSize ?? "Unknown";

            if (string.IsNullOrWhiteSpace(download.Directory))
                download.Directory = originalDirectory;

            // Explicitly attach event handlers to ensure they're connected
            PauseButton.Click += PauseButton_Click;
            ResumeButton.Click += ResumeButton_Click;
            CancelButton.Click += CancelDownload_Click;

            // Debug output to check initial status
            System.Diagnostics.Debug.WriteLine($"Initial download status: {download.Status}");
            System.Diagnostics.Debug.WriteLine($"Initial download GID: {download.Gid}");

            // Set initial button states
            UpdateButtonStates();

            // Debug button states after update
            System.Diagnostics.Debug.WriteLine($"PauseButton - Enabled: {PauseButton.IsEnabled}, Visible: {PauseButton.Visibility}");
            System.Diagnostics.Debug.WriteLine($"ResumeButton - Enabled: {ResumeButton.IsEnabled}, Visible: {ResumeButton.Visibility}");

            updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            updateTimer.Tick += UpdateUI;
            updateTimer.Start();

            networkRetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            networkRetryTimer.Tick += NetworkRetryTimer_Tick;
            networkRetryTimer.Start();
        }

        private void UpdateUI(object? sender, System.EventArgs e)
        {
            try
            {
                double totalSizeMB = ParseSizeInMB(download.TotalSize);
                double downloadedMB = (download.Progress * totalSizeMB) / 100.0;

                DownloadedSizeTextBlock.Text = FormatSize(downloadedMB);
                TotalSizeTextBlock.Text = FormatSize(totalSizeMB);

                if (download.Progress == lastProgress)
                {
                    stalledCounter++;
                }
                else
                {
                    stalledCounter = 0;
                    lastProgress = download.Progress;
                }

                ProgressBar.Value = download.Progress;
                ProgressText.Text = $"{download.Progress}%";

                StatusTextBlock.Text = download.Status;
                SpeedTextBlock.Text = download.Speed;
                EtaTextBlock.Text = download.EstimatedTimeRemaining;

                if (string.IsNullOrEmpty(download.Directory) || string.IsNullOrEmpty(download.FileName))
                {
                    WPF.MessageBox.Show("Download path or file name is missing.");
                    return;
                }

                string filePath = Path.Combine(download.Directory, download.FileName);

                if (download.Progress == 100 && File.Exists(filePath))
                {
                    download.Status = "complete";
                    ErrorContainer.Visibility = Visibility.Collapsed;
                    updateTimer.Stop();
                }
                else if (download.Status == "active" && download.Progress > 0 && !File.Exists(filePath))
                {
                    download.Status = "error";
                    ErrorMessageTextBlock.Text = "Download interrupted. File missing.";
                    ErrorContainer.Visibility = Visibility.Visible;
                    updateTimer.Stop();
                }

                if (download.Status == "error")
                {
                    ErrorMessageTextBlock.Text = !NetworkHelper.IsInternetAvailable()
                        ? "Download failed: Internet is disconnected."
                        : "Download failed: Invalid link, file not found, or deleted during download.";
                    ErrorContainer.Visibility = Visibility.Visible;

                    ResumeButton.Content = "🔄 Retry";
                }
                else
                {
                    ErrorContainer.Visibility = Visibility.Collapsed;
                    ResumeButton.Content = "▶️ Resume";
                }

                StatusTextBlock.Foreground = download.Status switch
                {
                    "paused" => new SolidColorBrush(Colors.Orange),
                    "active" => new SolidColorBrush(Colors.Green),
                    "error" or "removed" => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Black)
                };

                UpdateButtonStates();

                if (download.Status is "complete" or "error" or "removed")
                    updateTimer.Stop();

                if (download.Status == "active" && stalledCounter >= 30)
                {
                    download.Status = "error";
                    ErrorMessageTextBlock.Text = "Download appears to be stalled.";
                    ErrorContainer.Visibility = Visibility.Visible;
                    updateTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show("Error in UpdateUI: " + ex.Message);
                updateTimer.Stop();
            }
        }

        private void UpdateButtonStates()
        {
            System.Diagnostics.Debug.WriteLine($"UpdateButtonStates called - Status: {download.Status}");

            // Update button states based on current download status
            PauseButton.IsEnabled = download.Status == "active" && !isPausing;
            ResumeButton.IsEnabled = (download.Status == "paused" || download.Status == "error") && !isResuming;
            CancelButton.IsEnabled = download.Status != "complete" && download.Status != "removed";

            // Update button visibility for better UX
            switch (download.Status?.ToLower())
            {
                case "paused":
                    PauseButton.Visibility = Visibility.Collapsed;
                    ResumeButton.Visibility = Visibility.Visible;
                    break;

                case "active":
                    PauseButton.Visibility = Visibility.Visible;
                    ResumeButton.Visibility = Visibility.Collapsed;
                    break;

                case "queued":
                case "waiting":
                    // For queued/waiting downloads, show pause button but it might not be enabled yet
                    PauseButton.Visibility = Visibility.Visible;
                    ResumeButton.Visibility = Visibility.Collapsed;
                    // Enable pause button for queued downloads too
                    PauseButton.IsEnabled = !isPausing;
                    break;

                case "error":
                    PauseButton.Visibility = Visibility.Collapsed;
                    ResumeButton.Visibility = Visibility.Visible;
                    break;

                case "complete":
                case "removed":
                    PauseButton.Visibility = Visibility.Collapsed;
                    ResumeButton.Visibility = Visibility.Collapsed;
                    break;

                default:
                    // For unknown statuses, show pause button and enable it
                    PauseButton.Visibility = Visibility.Visible;
                    ResumeButton.Visibility = Visibility.Collapsed;
                    PauseButton.IsEnabled = !isPausing;
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"After update - PauseButton: Enabled={PauseButton.IsEnabled}, Visible={PauseButton.Visibility}");
            System.Diagnostics.Debug.WriteLine($"After update - ResumeButton: Enabled={ResumeButton.IsEnabled}, Visible={ResumeButton.Visibility}");
        }

        private double ParseSizeInMB(string sizeText)
        {
            if (sizeText == "Unknown") return 0;
            if (sizeText.EndsWith("MB") && double.TryParse(sizeText.Replace("MB", "").Trim(), out double mb))
                return mb;
            return 0;
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"PauseButton_Click called - Status: {download.Status}, GID: {download.Gid}");

            if (isPausing)
            {
                System.Diagnostics.Debug.WriteLine("Already pausing, returning");
                return;
            }

            isPausing = true;

            try
            {
                // Provide immediate feedback
                PauseButton.IsEnabled = false;
                PauseButton.Content = "⏸️ Pausing...";

                System.Diagnostics.Debug.WriteLine($"Attempting to pause download with GID: {download.Gid}");

                // Check if GID is valid
                if (string.IsNullOrWhiteSpace(download.Gid))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: GID is null or empty!");
                    WPF.MessageBox.Show("Cannot pause download: Invalid download ID");
                    return;
                }

                var result = await Aria2Helper.PauseDownloadAsync(download.Gid);

                System.Diagnostics.Debug.WriteLine($"Pause result: {result}");

                // Give some time for the pause to take effect
                await Task.Delay(1000);

                if (result)
                {
                    download.Status = "paused";
                    await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                    System.Diagnostics.Debug.WriteLine("Download paused successfully");
                }
                else
                {
                    // Try to check current status from Aria2
                    var rpc = new Aria2RpcService();
                    var status = await rpc.GetDownloadStatusAsync(download.Gid);

                    if (status?.Status == "paused")
                    {
                        download.Status = "paused";
                        await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                        System.Diagnostics.Debug.WriteLine("Download was actually paused (confirmed via status check)");
                    }
                    else
                    {
                        WPF.MessageBox.Show($"Failed to pause download. Current status: {status?.Status ?? "Unknown"}");
                        System.Diagnostics.Debug.WriteLine($"Failed to pause download. Status: {status?.Status}");
                    }
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Error pausing download: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception in PauseButton_Click: {ex}");
            }
            finally
            {
                isPausing = false;
                PauseButton.Content = "⏸️ Pause";
                UpdateButtonStates();
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
                ResumeButton.IsEnabled = false;
                ResumeButton.Content = "▶️ Resuming...";

                System.Diagnostics.Debug.WriteLine($"Attempting to resume download with GID: {download.Gid}");

                if (await Aria2Helper.ResumeDownloadAsync(download.Gid))
                {
                    download.Status = "active";
                    await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                    System.Diagnostics.Debug.WriteLine("Download resumed successfully");
                }
                else
                {
                    WPF.MessageBox.Show("Failed to resume download.");
                    System.Diagnostics.Debug.WriteLine("Failed to resume download");
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Error resuming download: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception in ResumeButton_Click: {ex}");
            }
            finally
            {
                isResuming = false;
                ResumeButton.Content = "▶️ Resume";
                UpdateButtonStates();
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
                download.Url = originalUrl;
                download.Directory = originalDirectory;
                download.FileName = originalFileName;
                download.Gid = newGid;
                download.Status = "Queued";
                download.Progress = 0;
                download.Speed = "0 KB/s";
                download.EstimatedTimeRemaining = "--";

                ErrorContainer.Visibility = Visibility.Collapsed;
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

        private string FormatSize(double sizeInMB)
        {
            if (sizeInMB >= 1024)
                return $"{(sizeInMB / 1024.0):F2} GB";
            if (sizeInMB >= 1)
                return $"{sizeInMB:F2} MB";
            return $"{(sizeInMB * 1024.0):F0} KB";
        }
    }
}