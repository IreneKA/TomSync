using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TomSync.Models;

namespace TomSync
{
    /// <summary>
    /// Логика взаимодействия для CreateTaskDialog.xaml
    /// </summary>
    public partial class CreateTaskDialog : Window
    {
        public SyncTask SyncTask { get; set; }

        private readonly SyncCore core;

        private readonly List<string> localPaths;
        private Uri selectedUri;
        private int backupCount;
        private SyncTimer syncTimer;

        public CreateTaskDialog(SyncCore syncCore)
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            backupCount = 0;
            syncTimer = new SyncTimer();
            core = syncCore;
            localPaths = new List<string>();
            LocalPaths_ListBox.ItemsSource = localPaths;
            ShowRootDir();
        }

        public CreateTaskDialog(SyncCore syncCore, SyncTask syncTask) : this(syncCore)
        {
            localPaths.AddRange(syncTask.LocalPaths);
            selectedUri = syncTask.ServerDirectoryUri;
            backupCount = syncTask.BackupCount;
            syncTimer = syncTask.SyncTimer;

            LocalPaths_ListBox.Items.Refresh();
            SelectedDirName_TexBlock.Text = syncTask.ServerDirName;
            backupCount = syncTask.BackupCount;
        }

        private void ShowDir(List<DirectoryItem> items)
        {
            DirName_TextBlock.Text = core.ShortCurrentUrl();
            ServerItem_ListView.ItemsSource = items.Where(i => i.IsFolder == true);
        }
        private async void ShowRootDir()
        {
            ShowDir(await core.RootListAsync());
        }
        private async void OpenDir()
        {
            if (ServerItem_ListView.SelectedItem != null)
            {
                DirectoryItem item = (DirectoryItem)ServerItem_ListView.SelectedItem;
                if (item.IsFolder == true)
                {
                    ShowDir(await core.ListAsync(item));
                }
            }
        }
        private async void Back()
        {
            ShowDir(await core.BackAsync());
        }

        private void CreateTask(List<string> localPaths, Uri serverDirUri, SyncTimer syncTimer, int backupCount)
        {
            SyncTask = SyncTask.CreateSyncTask(localPaths, serverDirUri, syncTimer, backupCount);
        }

        private void AddPath(bool isFolderPicker)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                Multiselect = true,
                IsFolderPicker = isFolderPicker
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (string file in dialog.FileNames)
                {
                    if (!localPaths.Exists(p => p == file))
                    {
                        localPaths.Add(file);
                    }
                }
                LocalPaths_ListBox.Items.Refresh();
            }
        }

        private void DeletePath()
        {
            if (LocalPaths_ListBox.SelectedItems.Count > 0)
            {
                foreach (object selectedItem in LocalPaths_ListBox.SelectedItems)
                {
                    string item = (string)selectedItem;
                    localPaths.Remove(item);
                }
                LocalPaths_ListBox.Items.Refresh();
            }
        }

        private void AddFile_Button_Click(object sender, RoutedEventArgs e)
        {
            AddPath(false);
        }

        private void AddDir_Button_Click(object sender, RoutedEventArgs e)
        {
            AddPath(true);
        }

        private void CreateSyncTask_Button_Click(object sender, RoutedEventArgs e)
        {
            if (localPaths == null || localPaths.Count < 1)
            {
                MessageBox.Show("Необходимо выбрать файлы или папки на компьютере");
                return;
            }
            if (selectedUri == null)
            {
                MessageBox.Show("Необходимо выбрать папку на сервере");
                return;
            }
            //if (!Int32.TryParse(BackupCount_TextBox.Text, out backupCount))
            //{
            //    backupCount = 0;
            //}
            CreateTask(localPaths, selectedUri, syncTimer, backupCount);
            DialogResult = true;
        }

        private void DeletePath_Button_Click(object sender, RoutedEventArgs e)
        {
            DeletePath();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDir();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void SelectCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            selectedUri = core.CurrentUri;
            SelectedDirName_TexBlock.Text = core.ShortCurrentUrl();
        }

        private void ServerItem_ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ServerItem_ListView.SelectedItem != null)
            {
                DirectoryItem dir = (DirectoryItem)ServerItem_ListView.SelectedItem;
                selectedUri = dir.Uri;
                SelectedDirName_TexBlock.Text = core.DirName(dir.Uri);
            }
        }

        private void AddParameters_Button_Click(object sender, RoutedEventArgs e)
        {
            TimeSettings dialog = new TimeSettings(syncTimer, backupCount);
            if (dialog.ShowDialog() == true)
            {
                backupCount = dialog.BackupCount;
                syncTimer = dialog.SyncTimer;
            }
        }
    }
}
