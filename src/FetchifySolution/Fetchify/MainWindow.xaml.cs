using Fetchify.Helpers;
using Fetchify.Models;
using Fetchify.Services;
using Fetchify.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPF = System.Windows; 

namespace Fetchify
{
    public partial class MainWindow : WPF.Window
    {
        private DispatcherTimer refreshTimer;
        private Aria2RpcService rpcService = new();

        public MainWindow()
        {
            Aria2ProcessManager.StartAria2WithRPC();
            InitializeComponent();

            // Bind the DataGrid to the global downloads list
            DownloadDataGrid.ItemsSource = DownloadManager.Downloads;

            // Setup periodic refresh timer
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();

            _ = LoadPreviousDownloadsAsync();
        }

        private void AddDownload_Click(object sender, WPF.RoutedEventArgs e)
        {
            var addWindow = new AddDownloadWindow();
            addWindow.DownloadStarted += StartDownload;
            addWindow.Owner = this;
            addWindow.ShowDialog();
        }

        private async void StartDownload(DownloadItem item)
        {
            string gid = await rpcService.AddDownloadAsync(item.Url, item.Directory, item.FileName);

            if (!string.IsNullOrEmpty(gid) && !gid.StartsWith("Error"))
            {
                var activeDownload = new ActiveDownload
                {
                    Gid = gid,
                    FileName = item.FileName,
                    Status = "Queued",
                    Progress = item.Progress,
                    Speed = item.Speed,
                    EstimatedTimeRemaining = item.EstimatedTimeRemaining,
                    TotalSize = "0 MB",
                    Url = item.Url,
                    Directory = item.Directory
                };

                DownloadManager.Downloads.Add(activeDownload);

                var statusWindow = new DownloadStatusWindow(activeDownload)
                {
                    Owner = this
                };
                statusWindow.OnDownloadRemoved = async (removedDownload) =>
                {
                    if (removedDownload != null)
                    {
                        DownloadManager.Downloads.Remove(removedDownload);
                        await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                    }
                };
                statusWindow.Show();
            }
            else
            {
                WPF.MessageBox.Show("Failed to start download: " + gid);
            }
        }

        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var updatedList = await rpcService.GetAllDownloadsAsync();

                foreach (var updatedItem in updatedList)
                {
                    var existing = DownloadManager.Downloads.FirstOrDefault(d => d.Gid == updatedItem.Gid);
                    if (existing != null)
                    {
                        existing.Status = updatedItem.Status;
                        existing.Progress = updatedItem.Progress;
                        existing.Speed = updatedItem.Speed;
                        existing.EstimatedTimeRemaining = updatedItem.EstimatedTimeRemaining;
                        existing.TotalSize = updatedItem.TotalSize;
                    }
                    else
                    {
                        DownloadManager.Downloads.Add(updatedItem);
                    }
                }

                // Optionally remove downloads no longer present
                var gidsInUpdated = updatedList.Select(d => d.Gid).ToHashSet();
                for (int i = DownloadManager.Downloads.Count - 1; i >= 0; i--)
                {
                    if (!gidsInUpdated.Contains(DownloadManager.Downloads[i].Gid))
                    {
                        DownloadManager.Downloads.RemoveAt(i);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Refresh error: " + ex.Message);
            }
        }

        private async void PauseDownload_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
            {
                WPF.MessageBox.Show("Please select a download to pause.");
                return;
            }

            bool success = await Aria2Helper.PauseDownloadAsync(selected.Gid);
            WPF.MessageBox.Show(success ? "Paused successfully." : "Pause failed.");
            await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
        }

        private async void ResumeDownload_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
            {
                WPF.MessageBox.Show("Please select a download to resume.");
                return;
            }

            bool success = await Aria2Helper.ResumeDownloadAsync(selected.Gid);
            WPF.MessageBox.Show(success ? "Resumed successfully." : "Resume failed.");
            await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
        }

        private async Task LoadPreviousDownloadsAsync()
        {
            try
            {
                var savedDownloads = await DownloadHistoryManager.LoadDownloadsAsync();
                DownloadManager.Downloads.Clear();

                foreach (var download in savedDownloads)
                {
                    DownloadManager.Downloads.Add(download);
                }

                var liveDownloads = await rpcService.GetAllDownloadsAsync();

                foreach (var item in liveDownloads)
                {
                    var existing = DownloadManager.Downloads.FirstOrDefault(d => d.Gid == item.Gid);
                    if (existing != null)
                    {
                        existing.Status = item.Status;
                        existing.Progress = item.Progress;
                        existing.Speed = item.Speed;
                        existing.EstimatedTimeRemaining = item.EstimatedTimeRemaining;
                        existing.TotalSize = item.TotalSize;
                    }
                    else
                    {
                        DownloadManager.Downloads.Add(item);
                    }
                }

                await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading previous downloads: " + ex.Message);
            }
        }

        private async void RemoveDownload_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
            {
                WPF.MessageBox.Show("Please select a download to remove.");
                return;
            }

            var result = WPF.MessageBox.Show(
                $"Are you sure you want to remove the download: {selected.FileName}?\nThis will permanently remove it from Fetchify and aria2.",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            bool removed = await Aria2Helper.RemoveDownloadAsync(selected.Gid);

            if (removed)
            {
                DownloadManager.Downloads.Remove(selected);
                await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                WPF.MessageBox.Show("Download removed successfully.");
            }
            else
            {
                WPF.MessageBox.Show("Failed to remove the download from aria2.");
            }
        }

        private void DownloadDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DownloadDataGrid.SelectedItem is not ActiveDownload selected)
                return;

            var statusWindow = new DownloadStatusWindow(selected);
            statusWindow.OnDownloadRemoved = async (removedDownload) =>
            {
                if (removedDownload != null)
                {
                    DownloadManager.Downloads.Remove(removedDownload);
                    await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                }
            };
            statusWindow.Show();
        }

        protected override void OnClosed(EventArgs e)
        {
            DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads).Wait();
            base.OnClosed(e);
        }

        public void AddDownloadToList(ActiveDownload download)
        {
            DownloadManager.Downloads.Add(download);
        }

        public void OnDownloadRemovedHandler(ActiveDownload removedDownload)
        {
            if (removedDownload != null)
            {
                DownloadManager.Downloads.Remove(removedDownload);
                _ = DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
            }
        }


    }
}
