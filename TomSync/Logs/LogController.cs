using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using TomSync.Logs.Models;

namespace TomSync.Logs
{
    public static class LogController
    {
        public static string LastMessage { get; private set; } = "";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static void Error(Logger logger, Exception exception, string addMessage = "", bool isShowMessage = false)
        {
            string errorMessage = $"{ addMessage}. {exception.Message.Replace('\n',' ')}";
            logger.Error(errorMessage);
            LastMessage = errorMessage;
            if (isShowMessage) MessageBox.Show(errorMessage);
        }

        public static void Warn(Logger logger, string warnMessage = "", bool isShowMessage = false)
        {
            logger.Warn(warnMessage);
            LastMessage = warnMessage;
            if (isShowMessage) MessageBox.Show(warnMessage);
        }

        public static void Info(Logger logger, string infoMessage)
        {
            logger.Info(infoMessage);
            LastMessage = infoMessage;
        }

        public static List<LogMessage> GetLogs(string logFilePath)
        {
            List<LogMessage> logs = new List<LogMessage>();
            try
            {
                foreach (string line in File.ReadAllLines(logFilePath, Encoding.Default))
                {
                    logs.Add(new LogMessage(line));
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Не удалось прочитать файл: {logFilePath}");
            }
            logs.Reverse();
            return logs;
        }

        public static List<LogFile> GetLogFiles()
        {
            List<LogFile> logFiles = new List<LogFile>();

            string logDirPath = Settings.FullLogPath;
            try
            {
                DirectoryInfo logDirInfo = new DirectoryInfo(logDirPath);
                foreach (FileInfo logFile in logDirInfo.GetFiles())
                {
                    logFiles.Add(new LogFile
                    {
                        Name = logFile.Name,
                        FullName = logFile.FullName
                    });
                }
            }
            catch (Exception e)
            {
                LogController.Error(logger, e, $"Не удалось найти файлы в папке: {logDirPath}");
            }
            logFiles.Reverse();
            return logFiles;
        }
    }
}
