using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using TomSync.Models;

namespace TomSync
{
    public partial class MainWindow : Window
    {
        private void ResultShow(bool result)
        {
            //if (result)
            //{
            //    ResultTextBlock.Text = "Success";
            //}
            //else
            //{
            //    ResultTextBlock.Text = "Failure";
            //}
            UpdateDir();
        }
        private void ResultClear()
        {
            ResultTextBlock.Text = "";
        }
        public async void ShowDirOrDownload(DirectoryItem item)
        {
            if (item.IsFolder == true)
            {
                ShowDir(await core.ListAsync(item));
            }
            else
            {
                //bool result = false;
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool result = await core.DownloadAsync(item, dialog.SelectedPath);
                    ResultShow(result);
                }
            }
        }
        private void ShowDir(List<DirectoryItem> items)
        {
            //для работы двойного клика по ListViewItem
            items.ForEach(item => item.Window = this);

            DirNameTextBlock.Text = core.ShortCurrentUrl();
            ServerItemList.ItemsSource = items;
        }
        private async void ShowRootDir()
        {
            ShowDir(await core.RootListAsync());
        }
        private async void UpdateDir()
        {
            ShowDir(await core.ListAsync());
        }
        private async void OpenDir()
        {
            if (ServerItemList.SelectedItems.Count == 1)
            {
                DirectoryItem item = (DirectoryItem)ServerItemList.SelectedItem;
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

        private async void Upload(bool isFolderPicker)
        {
            bool result = false;
            ResultClear();
            var dialog = new CommonOpenFileDialog();
            dialog.Multiselect = true;
            dialog.IsFolderPicker = isFolderPicker;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (var file in dialog.FileNames)
                {
                    result = await core.UploadAsync(file);
                }
                ResultShow(result);
            }
        }

        private async void DownloadSelectedItems()
        {
            bool result = false;
            ResultClear();
            if (ServerItemList.SelectedItems.Count > 0)
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var selectedItem in ServerItemList.SelectedItems)
                    {
                        DirectoryItem item = (DirectoryItem)selectedItem;
                        result = await core.DownloadAsync(item, dialog.SelectedPath);
                    }
                    ResultShow(result);
                }
            }
        }

        private async void Delete()
        {
            ResultClear();
            if (ServerItemList.SelectedItems.Count > 0)
            {
                int errors = 0;
                foreach (var selectedItem in ServerItemList.SelectedItems)
                {
                    DirectoryItem item = (DirectoryItem)selectedItem;
                    if (!await core.DeleteAsync(item)) errors++;
                }
                bool result = errors > 0 ? false : true;
                ResultShow(result);
            }
        }

        private async void Rename()
        {
            bool result = false;
            ResultClear();
            if (ServerItemList.SelectedItems.Count == 1)
            {
                //статичное имя, можно добавить форму ввода
                string newName = "new Name";
                DirectoryItem item = (DirectoryItem)ServerItemList.SelectedItem;
                result = await core.RenameAsync(item.Uri, newName);
                ResultShow(result);
            }
        }

        private async void Move()
        {
            bool result = false;
            ResultClear();
            if (ServerItemList.SelectedItems.Count > 0)
            {
                //статичное имя, можно добавить форму ввода
                string dirPath = "sync";
                int errors = 0;
                foreach (var selectedItem in ServerItemList.SelectedItems)
                {
                    DirectoryItem item = (DirectoryItem)selectedItem;
                    if (!await core.MoveAsync(item.Uri, dirPath)) errors++;
                }
                result = errors > 0 ? false : true;
                ResultShow(result);
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDir();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Back();
        }

        private void UploadFileButton_Click(object sender, RoutedEventArgs e)
        {
            Upload(false);
        }

        private void UploadDirButton_Click(object sender, RoutedEventArgs e)
        {
            Upload(true);
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadSelectedItems();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            Rename();
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            Move();
        }
    }
}
