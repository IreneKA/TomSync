using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using TomSync.Logs;
using TomSync.Models;
using TomSync.Parser;
using TomSync.Parser.Models;

namespace TomSync
{
    public static class Settings
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private const string defaultServer = "cloud.fctom.org";
        private const string baseDirLogPath = "${basedir}/";

        public static Uri Server { get; set; }
        public static string User { get; set; }
        public static string Pass { get; set; }
        public static bool Remember { get; set; }
        public static List<SyncTask> SyncTasks { get; set; }
        public static string LogPath { get; private set; }
        public static string FullLogPath { get; private set; }
        public static SpeedLimiter SpeedLimiter { get; set; }
        public static string FilesSavePath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) ;
                string pathSlash = (path.EndsWith("\\") ? path : path + "\\") + "TomSync\\";
                return pathSlash;
            }
        }
        public static string UserServer
        {
            get
            {
                if (Server != null && User != null)
                {
                    return User + "@" + Server.DnsSafeHost;
                }
                return null;
            }
        }

        public static void SetLogPath(string logPath)
        {
            SetLogPath(logPath, UserServer);
        }
        public static void SetLogPath(string logPath, string userServer)
        {
            LogPath = logPath;
            string logPathSlash = (logPath.EndsWith("\\")) ? logPath : logPath + "\\";
            FullLogPath = (logPath == baseDirLogPath ? "" : logPathSlash) + "logs/" + userServer + "/";

            var logFile = (from t in LogManager.Configuration.AllTargets
                           where t is FileTarget
                           select (FileTarget)t).FirstOrDefault();
            string fileName = (logPath == baseDirLogPath ? logPath : logPathSlash) + "logs/" + userServer + "/${shortdate}.log";
            logFile.FileName = fileName;

            LogController.Info(logger, $"Путь хранения логов изменен. Новый путь: {logPathSlash}");
        }

        public static LastUser LoadLastUser()
        {
            LastUser lastUser = XmlParser.LoadLastUser();
            return lastUser;
        }

        public static bool LoadSyncTasks()
        {
            SyncTasksModel model = XmlParser.LoadTasks(UserServer);
            SyncTasks = model.SyncTasks;
            SetLogPath(model.LogPath, UserServer);

            bool result = model.LogPath != "";
            return result;
        }

        public static bool LoadAddSettings()
        {
            AddSettingsModel model = XmlParser.LoadAddSettings();

            if (model.SpeedLimiter == null) return false;
            SpeedLimiter = model.SpeedLimiter;
            //Another settings...

            return true;
        }

        public static void SaveLastUser()
        {
            LastUser lastUser = new LastUser
            {
                User = User,
                Pass = Pass,
                Remember = true
            };
            lastUser.SetServerFromUri(Server);

            XmlParser.SaveLastUser(lastUser);
        }

        public static void SaveSyncTasks()
        {
            SyncTasksModel model = new SyncTasksModel
            {
                LogPath = LogPath,
                SyncTasks = SyncTasks,
                UserServer = UserServer
            };

            XmlParser.SaveTasks(model);
        }

        public static void SaveAddSettings()
        {
            AddSettingsModel model = new AddSettingsModel
            {
                SpeedLimiter = SpeedLimiter
            };

            XmlParser.SaveAddSettings(model);
        }


        // пока нет datacontext в listview. подумать, как заменить.
        public static void UpdateSyncDate(SyncTask task)
        {
            SyncTask syncTask = SyncTasks.Find(t => t == task);
            syncTask.UpdateSyncDate();
        }

        public static string GetFullServerPath(string server)
        {
            string serverPath = "";
            if (!server.StartsWith(@"http://") && !server.StartsWith(@"https://")) serverPath += @"https://";
            serverPath += server;
            if (!server.EndsWith(@"/remote.php/webdav/")) serverPath += @"/remote.php/webdav/";
            return serverPath;
        }

        //private static void NLogConfig(string logPath)
        //{
        //    LogPath = logPath;
        //    LogManager.Configuration.AddTarget(
        //        new FileTarget("filedata")
        //        {
        //            FileName = "$" + LogPath + "logs/${shortdate}.log",
        //            Layout = "${longdate} | ${uppercase:${level}} | ${logger} | ${message}"
        //        });
        //    LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, "filedata");
        //}

        public static string GetLogPath()
        {
            string logPath ="";
            try
            {
                var fileTarget = (from t in LogManager.Configuration.AllTargets
                               where t is FileTarget
                               select (FileTarget)t).FirstOrDefault();
                // Need to set timestamp here if filename uses date. 
                // For example - filename="${basedir}/logs/${shortdate}/trace.log"
                var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
                string fileName = fileTarget.FileName.Render(logEventInfo);
                if (!File.Exists(fileName))
                    throw new Exception("Log file does not exist.");

                logPath = fileName.Remove(fileName.IndexOf('/'));
            }
            catch (Exception e)
            {
                LogController.Error(logger, e);
            }
            return logPath;
        }

        public static void ClearLogs()
        {
            try
            {
                Directory.Delete(FullLogPath,true);
                string programLogPath = FilesSavePath + @"logs";
                Directory.Delete(programLogPath,true);
                LogController.Info(logger, "Файлы логов очищены");
            }
            catch (Exception e)
            {
                LogController.Error(logger, e);
            }
        }

        public static void SetDefaultLogPath()
        {
            SetLogPath(FilesSavePath);
        }

        public static FixedDocumentSequence LoadHelp()
        {
            FixedDocumentSequence result = new FixedDocumentSequence();
            try
            {
                using (XpsDocument doc = new XpsDocument("help.xps", FileAccess.Read))
                {
                    result = doc.GetFixedDocumentSequence();
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, "Не удалось загрузить справку.");
            }
            return result;
        }
    }
}
