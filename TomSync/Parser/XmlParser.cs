using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;
using TomSync.Logs;
using TomSync.Models;
using TomSync.Parser.Models;

namespace TomSync.Parser
{
    public static class XmlParser
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly byte[] key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
        private static readonly byte[] iv = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

        public static void SaveLastUser(LastUser lastUser)
        {
            if (!lastUser.Remember) return;

            XmlDocument lastUserDoc = new XmlDocument
            {
                PreserveWhitespace = true
            };
            XmlNode xmlDeclaration = lastUserDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = lastUserDoc.DocumentElement;
            lastUserDoc.InsertBefore(xmlDeclaration, root);

            XmlElement xRoot = lastUserDoc.CreateElement("Последний_пользователь");
            XmlElement server = lastUserDoc.CreateElement("Сервер");
            XmlElement login = lastUserDoc.CreateElement("Логин");
            XmlElement password = lastUserDoc.CreateElement("Пароль");

            xRoot.AppendChild(server);
            xRoot.AppendChild(login);
            xRoot.AppendChild(password);
            server.AppendChild(lastUserDoc.CreateTextNode(lastUser.Server));
            login.AppendChild(lastUserDoc.CreateTextNode(lastUser.User));
            password.AppendChild(lastUserDoc.CreateTextNode(lastUser.Pass));
            lastUserDoc.AppendChild(xRoot);

            try
            {
                RijndaelManaged Key = new RijndaelManaged
                {
                    Key = key,
                    IV = iv
                };
                Encrypt(Key, password);
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, "Не удалось cоздать симметричный ключ");
            }

            //XDocument lastUserDoc1 = new XDocument(
            //    new XElement("Последний_пользователь",
            //    new XElement("Сервер", lastUser.Server),
            //    new XElement("Логин", lastUser.User),
            //    new XElement("Пароль", lastUser.Pass)
            //    ));

            try
            {
                string path = Settings.FilesSavePath + "LastUser.xml";
                if (!Directory.Exists(Settings.FilesSavePath))
                    Directory.CreateDirectory(Settings.FilesSavePath);
                using (XmlTextWriter writer = new XmlTextWriter(File.OpenWrite(path), null))
                {
                    writer.Formatting = Formatting.Indented;
                    lastUserDoc.Save(writer);
                    LogController.Info(logger, "Файл LastUser.xml успешно сохранен");
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Не удалось сохранить файл LastUser.xml");
            }
        }

        public static void SaveTasks(SyncTasksModel model)
        {
            XDocument syncTasks = new XDocument();

            XElement tasks = new XElement("Задачи", new XAttribute("Файл_логов", model.LogPath));

            foreach (SyncTask task in model.SyncTasks)
            {
                XElement xSyncTask = new XElement("Задача");
                XElement localPaths = new XElement("Папки_на_компьютере");
                foreach (string path in task.LocalPaths)
                {
                    localPaths.Add(new XElement("Папка", path));
                }
                xSyncTask.Add(localPaths);
                xSyncTask.Add(new XElement("Папка_на_сервере", task.ServerDirectoryUri.ToString()));
                xSyncTask.Add(new XElement("Дата_создания", task.CreateDate.ToString()));
                XElement lastDate = new XElement("Дата_последней_синхронизации");
                if (task.LastSyncDate == null) lastDate.Value = ""; else lastDate.Value = task.LastSyncDate.ToString();
                xSyncTask.Add(lastDate);
                xSyncTask.Add(new XElement("Количество_версий", task.BackupCount.ToString()));

                XElement xTimer = new XElement("Таймер");
                xTimer.Add(new XElement("Синхронизация", task.SyncTimer.IsEnabled));
                xTimer.Add(new XElement("Дата_начала_синхнонизации", task.SyncTimer.StartDate));
                xTimer.Add(new XElement("Тип_синхронизации", task.SyncTimer.Type));
                xTimer.Add(new XElement("Период_синхронизации", task.SyncTimer.Period));
                xSyncTask.Add(xTimer);

                tasks.Add(xSyncTask);
            }
            syncTasks.Add(tasks);
            try
            {
                string path = Settings.FilesSavePath + model.UserServer + ".xml";
                if (!Directory.Exists(Settings.FilesSavePath))
                    Directory.CreateDirectory(Settings.FilesSavePath);
                syncTasks.Save(path);
            }
            catch(Exception e)
            {
                LogController.Error(logger, e, $"Не удалось сохранить файл {model.UserServer}.xml");
            }
            LogController.Info(logger, $"Файл {model.UserServer}.xml успешно сохранен");
        }

        public static void SaveAddSettings(AddSettingsModel model)
        {
            XDocument xAddSettingsDoc = new XDocument();
            XElement xSettings = new XElement("Доп_настройки");

            XElement xSpeedLimiter = new XElement("Ограничение_скорости");
            xSpeedLimiter.Add(new XElement("Ограничение_скачивания", model.SpeedLimiter.IsLimitDownload));
            xSpeedLimiter.Add(new XElement("Ограничение_загрузки", model.SpeedLimiter.IsLimitUpload));
            xSpeedLimiter.Add(new XElement("Скорость_скачивания", model.SpeedLimiter.DownloadSpeedLimit));
            xSpeedLimiter.Add(new XElement("Скорость_загрузки", model.SpeedLimiter.UploadSpeedLimit));
            xSettings.Add(xSpeedLimiter);

            xAddSettingsDoc.Add(xSettings);
            
            try
            {
                string path = Settings.FilesSavePath + "AddSettings.xml";
                if (!Directory.Exists(Settings.FilesSavePath))
                    Directory.CreateDirectory(Settings.FilesSavePath);
                xAddSettingsDoc.Save(path);
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Не удалось сохранить файл AddSettings.xml");
            }
            LogController.Info(logger, $"Файл AddSettings.xml успешно сохранен");
        }

        public static LastUser LoadLastUser()
        {
            LastUser lastUser = new LastUser();

            try
            {
                XmlDocument lastUseDoc = new XmlDocument
                {
                    PreserveWhitespace = true
                };
                string filePath = Settings.FilesSavePath + "LastUser.xml";
                lastUseDoc.Load(filePath);

                XmlElement encryptedElement = lastUseDoc.GetElementsByTagName("EncryptedData")[0] as XmlElement;
                RijndaelManaged Key = new RijndaelManaged
                {
                    Key = key,
                    IV = iv
                };
                Decrypt(Key, encryptedElement);

                XmlElement server = lastUseDoc.GetElementsByTagName("Сервер")[0] as XmlElement;
                lastUser.Server = server.FirstChild.Value;
                XmlElement user = lastUseDoc.GetElementsByTagName("Логин")[0] as XmlElement;
                lastUser.User = user.FirstChild.Value;
                XmlElement pass = lastUseDoc.GetElementsByTagName("Пароль")[0] as XmlElement;
                lastUser.Pass = pass.FirstChild.Value;
                lastUser.Remember = true;

                //lastUser.Server = lastUseDoc.Root.Element("Сервер").Value;
                //lastUser.User = lastUseDoc.Root.Element("Логин").Value;
                //lastUser.Pass = lastUseDoc.Root.Element("Пароль").Value;

                LogController.Info(logger, "Файл LastUser.xml успешно загружен");
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, "Не удалось загрузить данные последнего пользователя");
            }

            return lastUser;
        }

        public static SyncTasksModel LoadTasks(string userServer)
        {
            SyncTasksModel model = new SyncTasksModel
            {
                UserServer = userServer
            };

            try
            {
                string filePath = Settings.FilesSavePath + userServer + ".xml";
                XDocument syncTasks = XDocument.Load(filePath);

                string logPath = syncTasks.Root.Attribute("Файл_логов").Value;

                List<SyncTask> tasks = new List<SyncTask>();
                foreach (XElement task in syncTasks.Root.Elements("Задача"))
                {
                    List<string> paths = new List<string>();
                    foreach (XElement path in task.Element("Папки_на_компьютере").Elements("Папка"))
                    {
                        paths.Add(path.Value);
                    }

                    Uri serverDirectoryUri = new Uri(task.Element("Папка_на_сервере").Value);

                    DateTime createDate = DateTime.MinValue;
                    if (DateTime.TryParse(task.Element("Дата_создания").Value, out DateTime cdate))
                        createDate = cdate;

                    DateTime? lastSyncDate = null;
                    if (DateTime.TryParse(task.Element("Дата_последней_синхронизации").Value, out DateTime ldate))
                        lastSyncDate = ldate;

                    int backupCount = int.Parse(task.Element("Количество_версий").Value);

                    SyncTimer syncTimer = new SyncTimer();
                    XElement timer = task.Element("Таймер");
                    if (bool.TryParse(timer.Element("Синхронизация").Value, out bool isEnabled))
                        syncTimer.IsEnabled = isEnabled;

                    if (DateTime.TryParse(timer.Element("Дата_начала_синхнонизации").Value, out DateTime startDate))
                        syncTimer.StartDate = startDate;
                    else
                        syncTimer.StartDate = DateTime.MinValue;

                    string timerType = timer.Element("Тип_синхронизации").Value;
                    switch (timerType)
                    {
                        case "EveryDay":
                            syncTimer.Type = SyncTimerType.EveryDay;
                            break;
                        case "Custom":
                            syncTimer.Type = SyncTimerType.Custom;
                            break;
                        case "Once":
                        default:
                            syncTimer.Type = SyncTimerType.Once;
                            break;
                    }

                    if (TimeSpan.TryParse(timer.Element("Период_синхронизации").Value, out TimeSpan period))
                        syncTimer.Period = period;

                    SyncTask syncTask = new SyncTask(paths, serverDirectoryUri, createDate, syncTimer, lastSyncDate, backupCount);
                    tasks.Add(syncTask);
                }

                model.SyncTasks = tasks;
                model.LogPath = logPath;

                LogController.Info(logger, $"Файл {userServer}.xml успешно загружен");
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Не удалось загрузить файл {userServer}.xml");
            }

            return model;
        }

        public static AddSettingsModel LoadAddSettings()
        {
            AddSettingsModel model = new AddSettingsModel();
            try
            {
                string filePath = Settings.FilesSavePath + "AddSettings.xml";
                XDocument syncTasks = XDocument.Load(filePath);

                SpeedLimiter speedLimiter = new SpeedLimiter();
                XElement xSpeedLimiter = syncTasks.Root.Element("Ограничение_скорости");
                if (bool.TryParse(xSpeedLimiter.Element("Ограничение_скачивания").Value, out bool isLimitDownload))
                    speedLimiter.IsLimitDownload = isLimitDownload;
                if (bool.TryParse(xSpeedLimiter.Element("Ограничение_загрузки").Value, out bool isLimitUpload))
                    speedLimiter.IsLimitUpload = isLimitUpload;
                if (int.TryParse(xSpeedLimiter.Element("Скорость_скачивания").Value, out int downloadSpeedLimit))
                    speedLimiter.DownloadSpeedLimit = downloadSpeedLimit;
                if (int.TryParse(xSpeedLimiter.Element("Скорость_загрузки").Value, out int uploadSpeedLimit))
                    speedLimiter.UploadSpeedLimit = uploadSpeedLimit;
                model.SpeedLimiter = speedLimiter;
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Не удалось загрузить файл AddSettings.xml");
            }

            return model;
        }

        private static void Encrypt(SymmetricAlgorithm key, XmlElement elementToEncrypt)
        {
            EncryptedXml eXml = new EncryptedXml();
            byte[] encryptedElement = eXml.EncryptData(elementToEncrypt, key, false);

            EncryptedData edElement = new EncryptedData
            {
                Type = EncryptedXml.XmlEncElementUrl
            };

            string encryptionMethod = null;

            if (key is TripleDES)
            {
                encryptionMethod = EncryptedXml.XmlEncTripleDESUrl;
            }
            else if (key is DES)
            {
                encryptionMethod = EncryptedXml.XmlEncDESUrl;
            }
            if (key is Rijndael)
            {
                switch (key.KeySize)
                {
                    case 128:
                        encryptionMethod = EncryptedXml.XmlEncAES128Url;
                        break;
                    case 192:
                        encryptionMethod = EncryptedXml.XmlEncAES192Url;
                        break;
                    case 256:
                        encryptionMethod = EncryptedXml.XmlEncAES256Url;
                        break;
                }
            }
            else
            {
                // Throw an exception if the transform is not in the previous categories
                throw new CryptographicException("The specified algorithm is not supported for XML Encryption.");
            }

            edElement.EncryptionMethod = new EncryptionMethod(encryptionMethod);

            edElement.CipherData.CipherValue = encryptedElement;

            EncryptedXml.ReplaceElement(elementToEncrypt, edElement, false);
        }

        public static void Decrypt(SymmetricAlgorithm alg, XmlElement encryptedElement)
        {
            // Create an EncryptedData object and populate it.
            EncryptedData edElement = new EncryptedData();
            edElement.LoadXml(encryptedElement);

            // Create a new EncryptedXml object.
            EncryptedXml exml = new EncryptedXml();

            // Decrypt the element using the symmetric key.
            byte[] rgbOutput = exml.DecryptData(edElement, alg);

            // Replace the encryptedData element with the plaintext XML element.
            exml.ReplaceData(encryptedElement, rgbOutput);
        }
    }
}
