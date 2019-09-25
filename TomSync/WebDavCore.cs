using Born2Code.Net;
using DecaTec.WebDav;
using DecaTec.WebDav.WebDavArtifacts;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TomSync.Logs;
using TomSync.Models;

namespace TomSync
{
    public class WebDavCore
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();
        private HttpClientHandler httpClientHandler;
        private readonly Version version = HttpVersion.Version11;
        private QuotaLimiter quota;

        #region Параметры подключения WebDAV 
        public Uri Server { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public void SetUser(string user, string pass)
        {
            User = user;
            Pass = pass;

            httpClientHandler = new HttpClientHandler()
            {
                PreAuthenticate = true,
                // Учетные данные пользователя. 
                Credentials = new NetworkCredential(User, Pass)
            };
            LogController.Info(logger, $"Задан пользователь {User}");
        }

        private Uri GetRelativeUri(string path)
        {
            try
            {
                return new Uri(path, UriKind.Relative);
            }
            catch (Exception e)
            {
                LogController.Error(logger, e);
                return null;
            }
        }
        #endregion Параметры подключения WebDAV 

        #region Операции WebDAV

        public async Task<bool> CheckAuthorizationAsync()
        {
            LogController.Info(logger, $"Попытка авторизации. user: {User}, server {Server}");
            try
            {
                using (WebDavSession session = new WebDavSession(httpClientHandler, version))
                {
                    IList<WebDavSessionItem> item = await session.ListAsync(Server);
                    LogController.Info(logger, $"Авторизация прошла успешно. user: {User}, server: {Server}");
                    return true;
                }
            }
            //catch (HttpRequestException e)
            //{
            //    LogController.Error(logger, e);
            //    return false;
            //}
            //catch (WebDavException e)
            //{
            //    LogController.Error(logger, e);
            //    return false;
            //}
            catch (Exception e)
            {
                LogController.Error(logger, e, "Ошибка авторизации");
                return false;
            }
        }

        #region List (PROPFIND)

        /// <summary>
        /// Список файлов в корневом каталоге
        /// </summary>
        public async Task<List<DirectoryItem>> ListAsync()
        {
            return await ListAsync(Server);
        }
        /// <summary>
        /// Список файлов в данном каталоге
        /// </summary>
        /// <param name="dirUrl">Путь к каталогу на сервере.</param>
        public async Task<List<DirectoryItem>> ListAsync(string dirUrl)
        {
            //return await ListAsync(new Uri(dirPath, UriKind.Relative));
            return await ListAsync(new Uri(Server, dirUrl));
        }
        /// <summary>
        /// Список файлов в данном каталоге
        /// </summary>
        /// <param name="dirUri">Путь к каталогу на сервере.</param>
        public async Task<List<DirectoryItem>> ListAsync(Uri dirUri)
        {
            List<DirectoryItem> list = new List<DirectoryItem>();
            
            Progress_Start();

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    IList<WebDavSessionItem> items = await session.ListAsync(dirUri);
                    foreach (WebDavSessionItem item in items)
                    {
                        // обработка списка
                        DirectoryItem dirItem = new DirectoryItem
                        {
                            Name = item.Name,
                            Uri = item.Uri,
                            LastModified = item.LastModified,
                            ContentLength = item.ContentLength,
                            QuotaUsedBytes = item.QuotaUsedBytes,
                            IsFolder = item.IsFolder
                        };
                        list.Add(dirItem);
                    }
                    LogController.Info(logger, $"Получено содержимое каталога {dirUri}");
                }
                await LoadQuotaAsync();
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка, не удалось получить содержимое каталога {dirUri}");
            }

            Progress_Stop();

            return list;
        }

        public async Task<DirectoryItem> GetItemAsync(string filePath)
        {
            return await GetItemAsync(new Uri(Server, filePath));
        }

        // переделать через PropFind
        public async Task<DirectoryItem> GetItemAsync(Uri fileUri)
        {
            DirectoryItem dirItem = new DirectoryItem();

            Progress_Start();

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    string fileUrl = fileUri.ToString();
                    string dirUrl = fileUrl.Remove(fileUrl.LastIndexOf("/"));
                    IList<WebDavSessionItem> items = await session.ListAsync(dirUrl);

                    string fileName = fileUrl.Remove(0, fileUrl.LastIndexOf("/") + 1);
                    WebDavSessionItem sessionItem = items.FirstOrDefault(i => i.Name == fileName);

                    if (sessionItem != null)
                    {
                        dirItem = new DirectoryItem
                        {
                            Name = sessionItem.Name,
                            Uri = sessionItem.Uri,
                            LastModified = sessionItem.LastModified,
                            ContentLength = sessionItem.ContentLength,
                            QuotaUsedBytes = sessionItem.QuotaUsedBytes,
                            IsFolder = sessionItem.IsFolder
                        };
                    }

                    LogController.Info(logger, $"Получен файл {fileUri}");
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка, не удалось получить файл {fileUri}");
            }

            Progress_Stop();

            return dirItem;
        }

        #endregion List (PROPFIND)

        #region UploadFile (PUT)
        /// <summary>
        /// Загрузить файл на сервер
        /// </summary>
        /// <param name="localFilePath">Локальный путь и имя файла для загрузки.</param>
        /// <param name="remoteFileUrl">Путь к файлу и имя файла на сервере.</param>
        public async Task<bool> UploadFileAsync(string localFilePath, string remoteFileUrl)
        {
            //return await ListAsync(new Uri(dirPath, UriKind.Relative));
            return await UploadFileAsync(localFilePath, GetRelativeUri(remoteFileUrl));
        }

        /// <summary>
        /// Загрузить файл на сервер
        /// !!! добавить прогресс !!!
        /// </summary>
        /// <param name="localFilePath">Локальный путь и имя файла для загрузки.</param>
        /// <param name="remoteFileUri">Путь к файлу и имя файла на сервере.</param>
        public async Task<bool> UploadFileAsync(string localFilePath, Uri remoteFileUri)
        {
            bool result = false;

            Progress_Clear();
            Show_Message($"Загрузка файла {localFilePath}");

            if (quota.IsLimited)
            {
                FileInfo fileInfo = new FileInfo(localFilePath);
                if (quota.QuotaAvailableBytes < fileInfo.Length)
                {
                    LogController.Warn(logger, $"Недостаточно места на сервере");
                    Progress_Complete();
                    Show_Message();
                    return false;
                }
            }

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    Progress<WebDavProgress> progress = new Progress<WebDavProgress>();
                    progress.ProgressChanged += Progress_ProgressChanged;
                    session.Timeout = TimeSpan.FromDays(1);

                    Stream stream = File.OpenRead(localFilePath);
                    if (Settings.SpeedLimiter.IsLimitUpload)
                        stream = new ThrottledStream(stream, Settings.SpeedLimiter.UploadSpeedLimit*1024);
                    Set_Stream(stream);

                    string contentType = MimeMapping.GetMimeMapping(localFilePath);
                    result = await session.UploadFileWithProgressAsync(remoteFileUri, stream, contentType, progress, CancellationToken.None);

                    if (result)
                    {
                        LogController.Info(logger, $"Файл успешно загружен: {localFilePath}");
                        Progress_Complete();
                    }
                    else
                    {
                        LogController.Warn(logger, $"Файл не загружен: {localFilePath}");
                    }

                    Close_Stream();
                    //stream.Dispose();
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка загрузки файла: {localFilePath}");
            }

            Show_Message();

            return result;
        }
        #endregion UploadFile (PUT)

        #region  DownloadFile (GET)
        /// <summary>
        /// Скачать файл с сервера
        /// </summary>
        /// <param name="remoteFileUrl">Исходный путь и имя файла на сервере.</param>
        /// <param name="localFilePath">Путь и имя файла для загрузки в локальной файловой системе.</param>
        public async Task<bool> DownloadFileAsync(string remoteFileUrl, string localFilePath)
        {
            return await DownloadFileAsync(GetRelativeUri(remoteFileUrl), localFilePath);
        }

        /// <summary>
        /// Скачать файл с сервера
        /// </summary>
        /// <param name="remoteFileUri">Исходный путь и имя файла на сервере.</param>
        /// <param name="localFilePath">Путь и имя файла для загрузки в локальной файловой системе.</param>
        public async Task<bool> DownloadFileAsync(Uri remoteFileUri, string localFilePath)
        {
            bool result = false;

            Progress_Clear();
            Show_Message($"Скачивание файла {localFilePath}");

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    Progress<WebDavProgress> progress = new Progress<WebDavProgress>();
                    progress.ProgressChanged += Progress_ProgressChanged;

                    Stream stream = File.Create(localFilePath);
                    if (Settings.SpeedLimiter.IsLimitDownload)
                        stream = new ThrottledStream(stream, Settings.SpeedLimiter.DownloadSpeedLimit * 1024);
                    Set_Stream(stream);

                    result = await session.DownloadFileWithProgressAsync(remoteFileUri, stream, progress, CancellationToken.None);
                    if (result)
                    {
                        LogController.Info(logger, $"Файл успешно скачен: {remoteFileUri}");
                        Progress_Complete();
                    }
                    else
                    {
                        LogController.Warn(logger, $"Файл не скачен: {remoteFileUri}");
                    }

                    Close_Stream();
                    //stream.Dispose();
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка скачивания файла: {remoteFileUri}");
            }

            Show_Message();

            return result;
        }

        #endregion  DownloadFile (GET)

        #region CreateDir (MKCOL)
        /// <summary>
        /// Создать каталог на сервере
        /// </summary>
        /// <param name="dirUrl">Путь к каталогу на сервере.</param>
        public async Task<bool> CreateDirAsync(string dirUrl)
        {
            return await CreateDirAsync(GetRelativeUri(dirUrl));
        }

        /// <summary>
        /// Создать каталог на сервере
        /// </summary>
        /// <param name="dirUri">Путь к каталогу на сервере.</param>
        public async Task<bool> CreateDirAsync(Uri dirUri)
        {
            bool result = false;

            Progress_Start();
            Show_Message($"Создание каталога {dirUri}");

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    result = await session.CreateDirectoryAsync(dirUri);
                    if (result)
                    {
                        LogController.Info(logger, $"Каталог успешно создан: {dirUri}");
                    }
                    else
                    {
                        LogController.Warn(logger, $"Каталог не создан: {dirUri}");
                    }
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка создания каталога: {dirUri}");
            }

            Progress_Stop();
            Show_Message();

            return result;
        }
        #endregion CreateDir (MKCOL)

        #region Delete (DELETE)
        /// <summary>
        /// Удалить файл/каталог на сервере
        /// </summary>
        /// <param name="url">Путь к файлу/каталогу на сервере.</param>
        public async Task<bool> DeleteAsync(string url)
        {
            return await DeleteAsync(GetRelativeUri(url));
        }

        /// <summary>
        /// Удалить файл/каталог на сервере
        /// </summary>
        /// <param name="uri">Путь к файлу/каталогу на сервере.</param>
        public async Task<bool> DeleteAsync(Uri uri)
        {
            bool result = false;

            Progress_Start();
            Show_Message($"Удаление файла/каталога {uri}");

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    result = await session.DeleteAsync(uri);
                    if (result)
                    {
                        LogController.Info(logger, $"Успешно удалено: {uri}");
                    }
                    else
                    {
                        LogController.Warn(logger, $"Не удалено: {uri}");
                    }
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка удаления: {uri}");
            }

            Progress_Stop();
            Show_Message();

            return result;
        }
        #endregion Delete (DELETE)

        #region Move
        /// <summary>
        /// Переместить файл/каталог на сервере
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="destinationUrl"></param>
        public async Task<bool> MoveAsync(string sourceUrl, string destinationUrl)
        {
            return await MoveAsync(GetRelativeUri(sourceUrl), GetRelativeUri(destinationUrl));
        }

        /// <summary>
        /// Переместить файл/каталог на сервере
        /// </summary>
        /// <param name="sourceUri"></param>
        /// <param name="destinationUrl"></param>
        public async Task<bool> MoveAsync(Uri sourceUri, string destinationUrl)
        {
            return await MoveAsync(sourceUri, GetRelativeUri(destinationUrl));
        }

        /// <summary>
        /// Переместить файл/каталог на сервере
        /// </summary>
        /// <param name="sourceUri"></param>
        /// <param name="destinationUri"></param>
        public async Task<bool> MoveAsync(Uri sourceUri, Uri destinationUri)
        {
            bool result = false;

            Progress_Start();
            Show_Message($"Перемешщение  из {sourceUri} в {destinationUri}");

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    result = await session.MoveAsync(sourceUri, destinationUri);
                    if (result)
                    {
                        LogController.Info(logger, $"Успешно перемещено из {sourceUri} в {destinationUri}");
                    }
                    else
                    {
                        LogController.Warn(logger, $"Не перемещено из {sourceUri} в {destinationUri}");
                    }
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка перемещения из {sourceUri} в {destinationUri}");
            }

            Progress_Stop();
            Show_Message();

            return result;
        }
        #endregion Move

        #region Rename
        /// <summary>
        /// Переименовать файл/каталог на сервере
        /// </summary>
        /// <param name="url"></param>
        /// <param name="newName"></param>
        public async Task<bool> RenameAsync(string url, string newName)
        {
            return await RenameAsync(new Uri(Server, url), newName);
        }

        /// <summary>
        /// Переименовать файл/каталог на сервере
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="newName"></param>
        public async Task<bool> RenameAsync(Uri uri, string newName)
        {
            Uri newUri = new Uri(uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length));
            newUri = new Uri(newUri, newName);

            bool result = false;

            Progress_Start();
            Show_Message($"Переименование из {uri} в {newUri}");

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    result = await session.MoveAsync(uri, newUri);
                    if (result)
                    {
                        LogController.Info(logger, $"Успешно переименовано из {uri} в {newUri}");
                    }
                    else
                    {
                        LogController.Warn(logger, $"Не переименовано из {uri} в {newUri}");
                    }
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка переименованования из {uri} в {newUri}");
            }

            Progress_Stop();
            Show_Message();

            return result;
        }
        #endregion Rename

        #region Exists
        public async Task<bool> ExistsAsync(string path)
        {
            return await ExistsAsync(GetRelativeUri(path));
        }
        public async Task<bool> ExistsAsync(Uri uri)
        {
            bool result = false;

            Progress_Start();
            Show_Message($"Поиск {uri}");

            try
            {
                using (WebDavSession session = new WebDavSession(Server, httpClientHandler, version))
                {
                    result = await session.ExistsAsync(uri);
                    if (result)
                    {
                        LogController.Info(logger, $"Сервер содержит {uri}");
                    }
                    else
                    {
                        LogController.Info(logger, $"Сервер не содержит {uri}");
                    }
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка, не удалось проверить существование {uri}");
            }

            Progress_Stop();
            Show_Message();

            return result;
        }
        #endregion Exists

        public async Task<bool> LoadQuotaAsync()
        {
            quota = new QuotaLimiter();

            Progress_Start();

            try
            {
                // ||=========||====\\
                // ||*костыль*||     ||========{| 
                // ||=========||====//
                using (WebDavClient client = new WebDavClient(
                    new HttpClientHandler()
                    {
                        PreAuthenticate = true,
                        // Учетные данные пользователя. 
                        Credentials = new NetworkCredential(User, Pass)
                    },
                    version))
                {
                    var response = await client.PropFindAsync(Server);
                    var multistatus = await WebDavResponseContentParser.ParseMultistatusResponseContentAsync(response.Content);
                    foreach (var responseItem in multistatus.Response)
                    {
                        if (responseItem.Href == "/remote.php/webdav/")
                            foreach (var item in responseItem.Items)
                            {
                                var propStat = item as Propstat;
                                var prop = propStat.Prop;
                                quota.QuotaAvailableBytes = prop.QuotaAvailableBytes;
                                quota.QuotaUsedBytes = prop.QuotaUsedBytes;
                            }
                    }

                    //LogController.Info(logger, $"Получена квота");
                    Show_Quota(quota);
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Ошибка, не удалось получить квоту");
                return false;
            }

            Progress_Stop();

            return true;
        }

        #endregion Операции WebDAV

        private long previous = 0;
        private void Progress_ProgressChanged(object sender, WebDavProgress e)
        {
            // Do not report every single update of the progress.
            if (e.Bytes - previous > 1024)
            {
                previous = e.Bytes;
                StatusBar.SetProgress((double)e.Bytes / e.TotalBytes * 100);
            }
        }
        private void Progress_Complete()
        {
            previous = 0;
            StatusBar.SetProgress(100);
        }

        private void Progress_Clear()
        {
            previous = 0;
            StatusBar.ClearProgress();
        }

        private void Progress_Start()
        {
            StatusBar.StartLoading();
        }

        private void Progress_Stop()
        {
            StatusBar.StopLoading();
        }

        private void Show_Message()
        {
            StatusBar.SetMessage(LogController.LastMessage);
        }

        private void Show_Message(string message)
        {
            StatusBar.SetMessage(message);
        }

        private void Show_Quota(QuotaLimiter quota)
        {
            StatusBar.SetQuota(quota);
        }

        private void Set_Stream(Stream stream)
        {
            StatusBar.SetStream(stream);
        }

        private void Close_Stream()
        {
            StatusBar.CloseStream();
        }
    }
}
