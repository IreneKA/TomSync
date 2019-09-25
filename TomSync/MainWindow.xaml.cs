using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using TomSync.Logs;
using TomSync.Logs.Models;
using TomSync.Models;

namespace TomSync
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SyncCore core;
        private SyncTimerController timerController;

        public MainWindow(SyncCore syncCore)
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            core = syncCore;

            Settings.LoadAddSettings();

            Settings.LoadSyncTasks();
            Tasks_ListView.ItemsSource = Settings.SyncTasks;

            StatusBar.ProgressBar = ProgressBar;
            StatusBar.ProgressTextBlock = ProgressTextBlock;
            StatusBar.MessageTextBlock = MessageTextBlock;
            StatusBar.QuotaTextBlock = Quota_TextBox;
            StatusBar.CloseStreamButton = CloseStream_Button;

            UserServer_TextBlock.Text = Settings.UserServer;

            ShowRootDir();

            timerController = new SyncTimerController(syncCore);
            timerController.Start();

            docViewer.Document = Settings.LoadHelp();
        }

        private void ShowTasks(List<SyncTask> tasks)
        {
            Tasks_ListView.Items.Refresh();
        }

        private async void Folder_Button_Click(object sender, RoutedEventArgs e)
        {
            Sync_Button.IsEnabled = false;
            bool result = await core.SyncAllAsync();
            Sync_Button.IsEnabled = true;

            ShowTasks(Settings.SyncTasks);
        }
        private void CreateTask_Button_Click(object sender, RoutedEventArgs e)
        {
            CreateTaskDialog dialog = new CreateTaskDialog(core)
            {
                Title = "Создание задачи"
            };
            if (dialog.ShowDialog() == true)
            {
                Settings.SyncTasks.Add(dialog.SyncTask);
                Settings.SaveSyncTasks();
                Tasks_ListView.Items.Refresh();
            }
        }

        private void LogFiles_ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateLogHistory();
        }

        private void UpdateLogHistory()
        {
            if (LogFiles_ListBox.SelectedItem != null)
            {
                Logs_ListView.ItemsSource = LogController.GetLogs(((LogFile)LogFiles_ListBox.SelectedItem).FullName);
            }
        }

        private void History_TabItem_Selected(object sender, RoutedEventArgs e)
        {
            LoadLogHistory();
        }

        private void LoadLogHistory()
        {
            var logFiles = LogController.GetLogFiles();
            LogFiles_ListBox.ItemsSource = logFiles;
            if (logFiles.Count > 0)
            {
                LogFiles_ListBox.SelectedIndex = 0;
            }
        }

        // Оптмизировать и разделить на подфункции
        private async void ListViewItem_MouseDoubleClickAsync(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (((System.Windows.Controls.ListViewItem)sender).Content is DirectoryItem item)
            {
                if (item.IsFolder == true)
                {
                    ShowDir(await core.ListAsync(item));
                }
                else
                {
                    DownloadItem(item);
                }
            }
        }

        private async void DownloadItem(DirectoryItem item)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                bool result = await core.DownloadAsync(item, dialog.SelectedPath);
                ResultShow(result);
            }
        }

        private void DeleteTask_Button_Click(object sender, RoutedEventArgs e)
        {
            DeleteTasks();
        }

        private void ChangeTask_Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeTask();
        }

        private void DeleteTasks()
        {
            ResultClear();
            if (Tasks_ListView.SelectedItems.Count > 0)
            {
                int errors = 0;
                foreach (var selectedItem in Tasks_ListView.SelectedItems)
                {
                    SyncTask item = (SyncTask)selectedItem;
                    if (!Settings.SyncTasks.Remove(item)) errors++;
                }
                bool result = errors > 0 ? false : true;
                if (result)
                {
                    Settings.SaveSyncTasks();
                }
                else
                {
                    Settings.LoadSyncTasks(); //При неудачном удалении загружаем последнюю версию файла
                }
                Tasks_ListView.Items.Refresh();
                ResultShow(result);
            }
        }

        private void ChangeTask()
        {
            if (Tasks_ListView.SelectedItems.Count != 1) return;

            SyncTask syncTask = Tasks_ListView.SelectedItem as SyncTask;

            if (syncTask == null) return;

            CreateTaskDialog dialog = new CreateTaskDialog(core, syncTask)
            {
                Title = "Изменение задачи"
            };
            if (dialog.ShowDialog() == true)
            {
                int i = Tasks_ListView.SelectedIndex;
                Settings.SyncTasks.RemoveAt(i);
                Settings.SyncTasks.Insert(i, dialog.SyncTask);
                Settings.SaveSyncTasks();
                Tasks_ListView.Items.Refresh();
            }
        }

        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.Source is System.Windows.Controls.TabControl)
            {
                StatusBar.ClearProgress();
            }
        }

        private void Cloud_TabItem_Selected(object sender, RoutedEventArgs e)
        {
            UpdateDir();
        }

        private void ClearLogs_Button_Click(object sender, RoutedEventArgs e)
        {
            ClearLogs();
        }

        private void ClearLogs()
        {
            Settings.ClearLogs();
        }

        private void DefaultLogPath_Button_Click(object sender, RoutedEventArgs e)
        {
            SetDefaultLogPath();
        }

        private void SetDefaultLogPath()
        {
            Settings.SetDefaultLogPath();
            Settings.SaveSyncTasks();
            ViewCurrentLogPath();
        }

        private void ChangeLogPath_Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeLogPath();
        }

        private void ChangeLogPath()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.SetLogPath(dialog.SelectedPath);
                Settings.SaveSyncTasks();
            }
            ViewCurrentLogPath();
        }

        private void Settings_TabItem_Selected(object sender, RoutedEventArgs e)
        {
            ViewCurrentLogPath();
            ViewCurrentSpeet();
        }

        private void ViewCurrentLogPath()
        {
            LogPath_TextBlock.Text = Settings.GetLogPath();
        }

        private void Logout_Hyperink_Click(object sender, RoutedEventArgs e)
        {
            Login login = new Login(false);
            login.Show();
            this.Close();
        }

        private void ViewCurrentSpeet()
        {
            var downloadCheckedButton = Download_Box.Children.OfType<System.Windows.Controls.RadioButton>()
                                          .FirstOrDefault(r => r.GroupName == "Download" && r.Name == Settings.SpeedLimiter.IsLimitDownload.ToString()+"D");
            downloadCheckedButton.IsChecked = true;

            Download_UpDown.Value = Settings.SpeedLimiter.DownloadSpeedLimit;

            var uploadCheckedButton = Upload_Box.Children.OfType<System.Windows.Controls.RadioButton>()
                                          .FirstOrDefault(r => r.GroupName == "Upload" && r.Name == Settings.SpeedLimiter.IsLimitUpload.ToString()+"U");
            uploadCheckedButton.IsChecked = true;

            Upload_UpDown.Value = Settings.SpeedLimiter.UploadSpeedLimit;
        }

        private void SpeedLimit_Button_Click(object sender, RoutedEventArgs e)
        {
            ChangeSpeed();
        }

        private void ChangeSpeed()
        {
            var downloadCheckedButton = Download_Box.Children.OfType<System.Windows.Controls.RadioButton>()
                                          .FirstOrDefault(r => r.GroupName == "Download" && r.IsChecked == true);
            switch (downloadCheckedButton.Name)
            {
                case "TrueD":
                    Settings.SpeedLimiter.IsLimitDownload = true;
                    if (Download_UpDown.Value != null)
                        Settings.SpeedLimiter.DownloadSpeedLimit = Download_UpDown.Value.Value;
                    break;
                case "FalseD":
                default:
                    Settings.SpeedLimiter.IsLimitDownload = false;
                    break;
            }

            var uploadCheckedButton = Upload_Box.Children.OfType<System.Windows.Controls.RadioButton>()
                                          .FirstOrDefault(r => r.GroupName == "Upload" && r.IsChecked == true);
            switch (uploadCheckedButton.Name)
            {
                case "TrueU":
                    Settings.SpeedLimiter.IsLimitUpload = true;
                    if (Upload_UpDown.Value != null)
                        Settings.SpeedLimiter.UploadSpeedLimit = Upload_UpDown.Value.Value;
                    break;
                case "FalseU":
                default:
                    Settings.SpeedLimiter.IsLimitUpload = false;
                    break;
            }

            Settings.SaveAddSettings();
        }

        private void CloseStream_Button_Click(object sender, RoutedEventArgs e)
        {
            StatusBar.CloseStream();
        }
    }
}
