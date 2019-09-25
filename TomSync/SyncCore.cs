using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TomSync.Logs;
using TomSync.Models;

namespace TomSync
{
    public class SyncCore
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly WebDavCore webDavCore = new WebDavCore();
        private bool isSync = false;
        public Uri CurrentUri { get; private set; }
        public string CurrentDirName { get; private set; }

        public async Task<bool> AuthorizationAsync(string server, string user, string pass)
        {
            string serverPath = Settings.GetFullServerPath(server);
            try
            {
                webDavCore.Server = new Uri(serverPath);
            }
            catch (Exception e)
            {
                LogController.Error(logger, e);
                return false;
            }

            webDavCore.SetUser(user, pass);

            bool result = await webDavCore.CheckAuthorizationAsync();
            if (result)
            {
                Settings.Server = webDavCore.Server;
                Settings.User = user;
                Settings.Pass = pass;
            }
            return result;
        }



        #region Навигация (List)
        /// <summary>
        /// Список элементов в корневой папке
        /// </summary>
        internal async Task<List<DirectoryItem>> RootListAsync()
        {
            CurrentUri = webDavCore.Server;
            CurrentDirName = "Home";
            return await webDavCore.ListAsync(webDavCore.Server);
        }
        /// <summary>
        /// Список элементов в текущей папке
        /// </summary>
        internal async Task<List<DirectoryItem>> ListAsync()
        {
            return await webDavCore.ListAsync(CurrentUri);
        }
        /// <summary>
        /// Список элементов в данной папке
        /// </summary>
        /// <param name="dirUri">Путь к папке на сервере.</param>
        internal async Task<List<DirectoryItem>> ListAsync(Uri dirUri)
        {
            CurrentUri = dirUri;
            CurrentDirName = CurrentUri.Segments.Last();
            return await webDavCore.ListAsync(CurrentUri);
        }
        /// <summary>
        /// Список элементов в данной папке
        /// </summary>
        /// <param name="dirItem">Экземпляр папки DirectoryItem.</param>
        internal async Task<List<DirectoryItem>> ListAsync(DirectoryItem dirItem)
        {
            if (dirItem.IsFolder == true)
            {
                CurrentUri = dirItem.Uri;
                CurrentDirName = dirItem.Name;
            }
            return await webDavCore.ListAsync(CurrentUri);
        }
        /// <summary>
        /// Список элементов в предыдущей (родительской) папке
        /// </summary>
        internal async Task<List<DirectoryItem>> BackAsync()
        {
            if (CurrentUri != webDavCore.Server)
            {
                CurrentUri = new Uri(CurrentUri.AbsoluteUri.Remove(CurrentUri.AbsoluteUri.Length - CurrentUri.Segments.Last().Length));
                CurrentDirName = CurrentUri.Segments.Last();
            }
            return await webDavCore.ListAsync(CurrentUri);
        }
        #endregion Навигация (List)

        #region Загрузка на сервер
        /// <summary>
        /// Загрузить файл/папку на сервер
        /// </summary>
        /// <param name="localPath">Локальный путь и имя файла/папки для загрузки.</param>
        internal async Task<bool> UploadAsync(string localPath, int backupCount = 0)
        {
            return await UploadAsync(localPath, CurrentUri, backupCount);
        }

        /// <summary>
        /// Загрузить файл/папку на сервер
        /// </summary>
        /// <param name="localPath">Локальный путь и имя файла/папки для загрузки.</param>
        /// <param name="uri">Путь к папке файла (без имени) на сервере.</param>
        internal async Task<bool> UploadAsync(string localPath, Uri uri, int backupCount = 0)
        {
            string path = ShortUrl(uri);
            try
            {
                if (File.GetAttributes(localPath).HasFlag(FileAttributes.Directory))
                {
                    return await UploadDirectoryAsync(localPath, path, backupCount) > 0 ? false : true;
                }
                else
                {
                    return await UploadFileAsync(localPath, path, backupCount);
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, "Не удалось загрузить файл или папку");
                return false;
            }
        }

        public string DirName(Uri uri)
        {
            string url = uri.ToString().Replace(Server.ToString(), string.Empty);
            return url == "" ? "../" : url;
        }
        public string ShortUrl(Uri uri)
        {
            return uri.ToString().Replace(Server.ToString(), string.Empty);
        }
        public string ShortCurrentUrl()
        {
            string url = CurrentUri.ToString().Replace(Server.ToString(), string.Empty);
            return url == "" ? "../" : url;
        }
        public Uri Server { get { return webDavCore.Server; } }

        /// <summary>
        /// Загрузить файл на сервер
        /// !!!не готово!!! Добавить учет версий файлов!
        /// </summary>
        /// <param name="filePath">Локальный путь и имя файла для загрузки.</param>
        /// <param name="path">Путь к папке файла (без имени) на сервере.</param>
        internal async Task<bool> UploadFileAsync(string filePath, string path = "", int backupCount = 0)
        {
            if (CurrentUri != null)
            {
                if (backupCount < 0) backupCount = 0;
                FileInfo fileInfo = new FileInfo(filePath);

                if (!await IsNeedUpload(fileInfo, path))
                    return true;

                // контроль версий
                await CheckFileVersions(path, fileInfo.Name, backupCount);

                path += $"/{fileInfo.Name}";
                return await webDavCore.UploadFileAsync(fileInfo.FullName, path);
            }
            return false;
        }

        /// <summary>
        /// Загрузить папку на сервер
        /// </summary>
        /// <param name="directoryPath">Локальный путь и имя папки для загрузки.</param>
        /// <param name="path">Путь к родительской папке (без имени загружаемой папки) на сервере.</param>
        internal async Task<int> UploadDirectoryAsync(string directoryPath, string path = "", int backupCount = 0)
        {
            if (CurrentUri != null)
            {
                int errors = 0;
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                path += (path.EndsWith("/") ? "" : "/") + directoryInfo.Name;
                if (!await webDavCore.ExistsAsync(path))
                {
                    if (!await webDavCore.CreateDirAsync(path)) errors++;
                }
                foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
                {
                    errors += await UploadDirectoryAsync(directory.FullName, path, backupCount);
                }
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    if (!await UploadFileAsync(file.FullName, path, backupCount)) errors++;
                }
                return errors;
            }
            return -1;
        }
        #endregion Загрузка на сервер

        #region Скачать с сервера
        /// <summary>
        /// Скачать файл/папку с сервера
        /// </summary>
        /// <param name="item">Экземпляр DirectoryItem.</param>
        /// <param name="localPath">Локальный путь к папке загрузки (без имени скачиваемого файла/папки).</param>
        internal async Task<bool> DownloadAsync(DirectoryItem item, string localPath)
        {
            if (item.IsFolder == true)
            {
                return await DownloadDirectoryAsync(item, localPath) > 0 ? false : true;
            }
            else
            {
                return await DownloadFileAsync(item, localPath);
            }
        }

        /// <summary>
        /// Скачать файл с сервера
        /// </summary>
        /// <param name="file">Экземпляр файла DirectoryItem.</param>
        /// <param name="localPath">Локальный путь к папке загрузки (без имени скачиваемого файла).</param>
        internal async Task<bool> DownloadFileAsync(DirectoryItem file, string localPath)
        {
            localPath += $"\\{file.Name}";
            return await webDavCore.DownloadFileAsync(file.Uri, localPath);
        }

        /// <summary>
        /// Скачать папку с сервера
        /// </summary>
        /// <param name="dir">Экземпляр папки DirectoryItem.</param>
        /// <param name="localPath">Локальный путь к папке загрузки (без имени скачиваемой папки).</param>
        internal async Task<int> DownloadDirectoryAsync(DirectoryItem dir, string localPath)
        {
            int errors = 0;
            localPath += $"\\{dir.Name}";
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }
            List<DirectoryItem> dirItems = await webDavCore.ListAsync(dir.Uri);
            foreach (DirectoryItem dirItem in dirItems)
            {
                if (dirItem.IsFolder == true)
                {
                    errors += await DownloadDirectoryAsync(dirItem, localPath);
                }
                else
                {
                    if (!await DownloadFileAsync(dirItem, localPath)) errors++;
                }
            }
            return errors;
        }
        #endregion Скачать с сервера

        #region Создать новую папку
        /// <summary>
        /// Создать новую папку в текущей папке на сервере
        /// </summary>
        /// <param name="dirName">Имя новой папки</param>
        internal async Task<bool> CreateDirAsync(string dirName)
        {
            bool result = false;
            if (CurrentUri != null)
            {
                string path = CurrentUri.ToString().Replace(webDavCore.Server.ToString(), string.Empty);
                path += (path.EndsWith("/") ? "" : "/") + dirName;
                result = await webDavCore.CreateDirAsync(path);
            }
            return result;
        }
        #endregion Создать новую папку

        #region Удалить
        /// <summary>
        /// Удалить файл/папку на сервере
        /// </summary>
        /// <param name="uri">Путь к файлу/папке на сервере.</param>
        internal async Task<bool> DeleteAsync(Uri uri)
        {
            return await webDavCore.DeleteAsync(uri);
        }

        /// <summary>
        /// Удалить файл/каталог на сервере
        /// </summary>
        /// <param name="uri">Экземпляр DirectoryItem, который удаляется.</param>
        internal async Task<bool> DeleteAsync(DirectoryItem item)
        {
            return await DeleteAsync(item.Uri);
        }

        /// <summary>
        /// Удалить файлы/каталоги на сервере
        /// </summary>
        /// <param name="uri">Список экземпляров DirectoryItem, которые удаляются.</param>
        internal async Task<int> DeleteAsync(List<DirectoryItem> items)
        {
            int errors = 0;
            foreach (DirectoryItem item in items)
            {
                if (!await DeleteAsync(item.Uri)) errors++;
            }
            return errors;
        }
        #endregion Удалить

        #region Переместить и переименовать
        /// <summary>
        /// Переместить файл/папку на сервере
        /// </summary>
        /// <param name="uri">Путь к файлу/папке на сервере.</param>
        /// <param name="dirName">Путь к папке, куда перемещается файл/папка</param>
        internal async Task<bool> MoveAsync(Uri uri, string dirName)
        {
            return await webDavCore.MoveAsync(uri, $"{dirName}/{uri.Segments.Last()}");
        }

        /// <summary>
        /// Переместить файл/папку на сервере
        /// </summary>
        /// <param name="item">Экземпляр DirectoryItem, который перемещается.</param>
        /// <param name="dirName">Путь к папке, куда перемещается файл/папка</param>
        internal async Task<bool> MoveAsync(DirectoryItem item, string dirName)
        {
            return await webDavCore.MoveAsync(item.Uri, $"{dirName}/{item.Name}");
        }

        /// <summary>
        /// Переименовать файл/папку на сервере
        /// </summary>
        /// <param name="uri">Путь к файлу/папке на сервере.</param>
        /// <param name="newName">Новое имя файла/папки</param>
        internal async Task<bool> RenameAsync(Uri uri, string newName)
        {
            return await webDavCore.RenameAsync(uri, newName);
        }
        #endregion Переместить и переименовать

        /// <summary>
        /// Запустить задачу синхронизации
        /// </summary>
        /// <param name="task">Задача синхронизации.</param>
        internal async Task<int> SyncAsync(SyncTask task)
        {
            int errors = 0;
            foreach (string path in task.LocalPaths)
            {
                bool result = await UploadAsync(path, task.ServerDirectoryUri, task.BackupCount);
                if (result)
                {
                    task.UpdateSyncDate();
                    //Settings.UpdateSyncDate(task); // если изначально не обращались к settings
                }
                else
                {
                    errors++;
                }
            }
            return errors;
        }

        /// <summary>
        /// Запустить список задач синхронизации
        /// </summary>
        /// <param name="tasks">Список задач синхронизации.</param>
        internal async Task<bool> SyncAsync(List<SyncTask> tasks)
        {
            LogController.Info(logger, "Запуск задач синхронизации");

            int errors = 0;
            foreach (SyncTask task in tasks)
            {
                errors += await SyncAsync(task);
            }

            bool result = errors > 0 ? false : true;
            if (result)
            {
                LogController.Info(logger, "Синхронизация успешно завершена");
            }
            else
            {
                LogController.Warn(logger, $"Синхронизация завершена с ошибками. Ошибок: {errors}");
            }

            return result;
        }

        /// <summary>
        /// Запустить все задачи синхронизации
        /// </summary>
        internal async Task<bool> SyncAllAsync()
        {
            if (isSync)
                return false;

            isSync = true;

            bool result = await SyncAsync(Settings.SyncTasks);
            Show_Message();
            Settings.SaveSyncTasks();

            isSync = false;
            return result;
        }

        internal async Task CheckForSync(SyncTask task)
        {
            if (GetIsNeedUpdate(task))
            {
                LogController.Info(logger, "Запуск задачи синхронизации");

                int errors = await SyncAsync(task);

                bool result = errors > 0 ? false : true;
                if (result)
                {
                    LogController.Info(logger, "Синхронизация успешно завершена");
                }
                else
                {
                    LogController.Warn(logger, $"Синхронизация завершена с ошибками. Ошибок: {errors}");
                }
            }
        }

        private bool GetIsNeedUpdate(SyncTask task)
        {
            if (!task.SyncTimer.IsEnabled) return false;

            switch (task.SyncTimer.Type)
            {
                case SyncTimerType.Once:
                    if (task.SyncTimer.StartDate < DateTime.Now)
                    {
                        task.SyncTimer.IsEnabled = false;
                        return true;
                    }
                    else
                        return false;

                case SyncTimerType.EveryDay:
                    if (task.LastSyncDate == null)
                        return true;
                    if (task.LastSyncDate.Value.Date.Add(TimeSpan.FromDays(1)) < DateTime.Now)
                        return true;
                    else
                        return false;

                case SyncTimerType.Custom:
                    if (task.LastSyncDate == null)
                        return true;
                    if (task.LastSyncDate.Value.Add(task.SyncTimer.Period) < DateTime.Now)
                        return true;
                    else
                        return false;

                default:
                    return false;
            }
        }

        internal async void CheckAllForSync()
        {
            if (isSync)
                return;

            isSync = true;

            foreach (SyncTask task in Settings.SyncTasks)
            {
                await CheckForSync(task);
            }

            isSync = false;
        }

        internal async Task<bool> CheckFileVersions(string path, string fileName, int backupCount)
        {
            List<DirectoryItem> items = await webDavCore.ListAsync(path);
            IEnumerable<DirectoryItem> sameItems = items.Where(i => i.Name.Contains(fileName));

            Dictionary<DateTime, DirectoryItem> versions = new Dictionary<DateTime, DirectoryItem>();
            foreach (DirectoryItem item in sameItems)
            {
                if (item.Name == fileName && backupCount > 0)
                {
                    item.Name += $"---{item.LastModified}";

                    path += $"/{fileName}";
                    await webDavCore.RenameAsync(path, item.Name);
                }

                string name = ("" + item.Name).Replace(fileName + "---", string.Empty);
                if (DateTime.TryParse(name, out DateTime dateTime))
                {
                    versions.Add(dateTime, item);
                }
            }

            for (; versions.Count > backupCount;)
            {
                DateTime olderDate = versions.Keys.Min();
                await webDavCore.DeleteAsync(versions[olderDate].Uri);
                versions.Remove(olderDate);
            }

            return true;
        }

        private async Task<bool> IsNeedUpload(FileInfo fileInfo, string dirPath)
        {
            string filePath = dirPath + $"/{fileInfo.Name}";
            if (!await webDavCore.ExistsAsync(filePath))
                return true;

            DirectoryItem serverItem = await webDavCore.GetItemAsync(filePath);
            if (serverItem.LastModified < fileInfo.LastWriteTime)
                return true;

            return false;
        }

        private void Show_Message()
        {
            StatusBar.SetMessage(LogController.LastMessage);
        }
    }
}