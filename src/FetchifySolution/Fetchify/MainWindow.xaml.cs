    using Fetchify.Helpers;
    using Fetchify.Models;
    using Fetchify.Services;
    using Fetchify.Views;
    using System.Drawing;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Threading;
    using WPF = System.Windows;
    namespace Fetchify
    {
        public partial class MainWindow : WPF.Window
        {
            private DispatcherTimer refreshTimer;
            private Aria2RpcService rpcService = new();
            private System.Windows.Forms.NotifyIcon trayIcon;
            private bool allowClose = false;
            private DispatcherTimer autoSaveTimer;
            public MainWindow()
            {
                SettingsManager.Load();

                if (SettingsManager.CurrentSettings.AutoStartAria2)
                {
                    Aria2ProcessManager.StartAria2WithRPC();
                }

                InitializeComponent();
                InitializeTrayIcon();
                HttpServer.Start();
                SetupAutoSave();
                if (SettingsManager.CurrentSettings.LaunchMinimizedToTray)
                {
                    this.WindowState = System.Windows.WindowState.Minimized;
                    this.ShowInTaskbar = false;
                    this.Hide();
                }

                this.DataContext = DownloadManager.Instance;
                DownloadDataGrid.ItemsSource = DownloadManager.Downloads;

                refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                refreshTimer.Tick += RefreshTimer_Tick;
                refreshTimer.Start();

                _ = LoadPreviousDownloadsAsync();
            }

            private void InitializeTrayIcon()
            {
                trayIcon = new System.Windows.Forms.NotifyIcon();
                trayIcon.Icon = new System.Drawing.Icon("icon.ico");
                trayIcon.Visible = true;
                trayIcon.Text = "Fetchify - Download Manager";

                // Context menu for tray
                var contextMenu = new System.Windows.Forms.ContextMenuStrip();
                contextMenu.Items.Add("Show", null, (s, e) =>
                {
                    this.Show();
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.ShowInTaskbar = true;
                    this.Activate();
                });

                contextMenu.Items.Add("Exit", null, (s, e) =>
                {
                    trayIcon.Visible = false;
                    WPF.Application.Current.Shutdown();
                });

                trayIcon.ContextMenuStrip = contextMenu;

                trayIcon.DoubleClick += (s, e) =>
                {
                    this.Show();
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.ShowInTaskbar = true;
                    this.Activate();
                };
            }


            private void AddDownload_Click(object sender, WPF.RoutedEventArgs e)
            {
                var addWindow = new AddDownloadWindow();
                addWindow.DownloadStarted += StartDownload;
                addWindow.Owner = this;
                addWindow.ShowDialog();
            }

            public async void StartDownload(DownloadItem item)
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
                    // Store currently selected GID
                    string? selectedGid = (DownloadDataGrid.SelectedItem as ActiveDownload)?.Gid;

                    var updatedList = await rpcService.GetAllDownloadsAsync();

                    foreach (var updatedItem in updatedList)
                    {
                        var existing = DownloadManager.Downloads.FirstOrDefault(d => d.Gid == updatedItem.Gid);
                        if (existing != null)
                        {
                            if (existing.Status == "paused")
                                continue;

                            if (updatedItem.Progress > 0)
                                existing.Progress = updatedItem.Progress;

                            if (!string.IsNullOrWhiteSpace(updatedItem.TotalSize) && updatedItem.TotalSize != "0 MB")
                                existing.TotalSize = updatedItem.TotalSize;

                            // Optionally update speed and ETA only if the status is "active"
                            if (updatedItem.Status == "active")
                            {
                                existing.Speed = updatedItem.Speed;
                                existing.EstimatedTimeRemaining = updatedItem.EstimatedTimeRemaining;
                            }

                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DownloadManager.Downloads.Add(updatedItem);
                            });
                        }
                    }

                    var gidsInUpdated = updatedList.Select(d => d.Gid).ToHashSet();

                    for (int i = DownloadManager.Downloads.Count - 1; i >= 0; i--)
                    {
                        var item = DownloadManager.Downloads[i];
                        if (!gidsInUpdated.Contains(item.Gid))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DownloadManager.Downloads.RemoveAt(i);
                            });
                        }
                    }

                    // Restore selection if still present
                    if (!string.IsNullOrEmpty(selectedGid))
                    {
                        var matchingItem = DownloadManager.Downloads.FirstOrDefault(d => d.Gid == selectedGid);
                        if (matchingItem != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                DownloadDataGrid.SelectedItem = matchingItem;
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Refresh error: " + ex.Message);
                }
            }

            private async void PauseDownload_Click(object sender, WPF.RoutedEventArgs e)
            {
                if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
                {
                    WPF.MessageBox.Show("Please select a download to pause.");
                    return;
                }

                bool success = await Aria2Helper.PauseDownloadAsync(selected.Gid);

                // Wait briefly to let aria2 update its state
                await Task.Delay(1000);

                // Fetch latest status just to be sure
                var refreshed = await rpcService.GetDownloadStatusAsync(selected.Gid);
                if (refreshed != null)
                {
                    selected.Status = refreshed.Status;
                    selected.Progress = refreshed.Progress;
                    selected.TotalSize = refreshed.TotalSize;
                    selected.Speed = refreshed.Speed;
                    selected.EstimatedTimeRemaining = refreshed.EstimatedTimeRemaining;
                }

                await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());

                WPF.MessageBox.Show(success ? "Paused successfully." : "Pause failed.");
            }

            private void SetupAutoSave()
            {
                autoSaveTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(30)
                };
                autoSaveTimer.Tick += async (s, e) =>
                {
                    await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                };
                autoSaveTimer.Start();
            }

            private async void ResumeDownload_Click(object sender, WPF.RoutedEventArgs e)
            {
                if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
                {
                    WPF.MessageBox.Show("Please select a download to resume.");
                    return;
                }
                selected.Status = "active"; 
                await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                bool success = await Aria2Helper.ResumeDownloadAsync(selected.Gid);
                WPF.MessageBox.Show(success ? "Resumed successfully." : "Resume failed.");
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

                        if (download.Status == "paused")
                        {
                            await Aria2Helper.PauseDownloadAsync(download.Gid);
                        }
                    }

                    var liveDownloads = await rpcService.GetAllDownloadsAsync();
              
                    foreach (var item in liveDownloads)
                    {
                        var existing = DownloadManager.Downloads.FirstOrDefault(d => d.Gid == item.Gid);
                        if (existing != null)
                        {
                            // If paused, do NOT update it from aria2 (aria2 reports 0 for paused)
                            if (existing.Status == "paused")
                                continue;

                            // If aria2 returns empty or zero progress, skip
                            if (item.TotalSize == "0 MB" || item.Progress == 0)
                                continue;
                            if (!string.IsNullOrWhiteSpace(item.TotalSize) && item.TotalSize != "0 MB")
                                existing.TotalSize = item.TotalSize;

                            // Optional: if the saved status was "active", keep syncing speed and ETA
                            if (existing.Status == "active")
                            {
                                existing.Speed = item.Speed;
                                existing.EstimatedTimeRemaining = item.EstimatedTimeRemaining;
                            }
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

            private async void RemoveDownload_Click(object sender, WPF.RoutedEventArgs e)
            {
                if (DownloadDataGrid.SelectedItem is not ActiveDownload selected || string.IsNullOrWhiteSpace(selected.Gid))
                {
                    WPF.MessageBox.Show("Please select a download to remove.");
                    return;
                }

                var result = WPF.MessageBox.Show(
                    $"Are you sure you want to remove the download: {selected.FileName}?\nThis will permanently remove it from Fetchify and aria2.",
                    "Confirm Removal",
                    WPF.MessageBoxButton.YesNo,
                    WPF.MessageBoxImage.Warning);

                if (result != WPF.MessageBoxResult.Yes)
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

            public void AddDownloadToList(ActiveDownload download)
            {
                DownloadManager.Downloads.Add(download);
            }

            private void OpenSettings_Click(object sender, WPF.RoutedEventArgs e)
            {
                var window = new SettingsWindow
                {
                    Owner = this
                };
                window.ShowDialog();
            }

            protected override async void OnClosed(EventArgs e)
            {
                base.OnClosed(e);
                await DownloadHistoryManager.SaveDownloadsAsync(DownloadManager.Downloads.ToList());
                WPF.Application.Current.Shutdown();
            }

            protected override void OnStateChanged(EventArgs e)
            {
                base.OnStateChanged(e);
                if (this.WindowState == System.Windows.WindowState.Minimized)
                {
                    Hide();
                    trayIcon.Visible = true;
                }
            }

            private void RestoreFromTray()
            {
                Show();
                WindowState = System.Windows.WindowState.Normal;
                Activate();
                trayIcon.Visible = false;
            }

            private void DownloadDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {

            }
        }
    }
