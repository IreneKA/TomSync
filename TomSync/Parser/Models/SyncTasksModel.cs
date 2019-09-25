using System;
using System.Collections.Generic;
using TomSync.Models;

namespace TomSync.Parser.Models
{
    public class SyncTasksModel
    {
        public List<SyncTask> SyncTasks { get; set; }
        public string LogPath { get; set; }
        public string UserServer { get; set; }

        //со значениями по умолчанию
        public SyncTasksModel()
        {
            SyncTasks = new List<SyncTask>();
            //LogPath = "${basedir}/";
            string logPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string logPathSlash = (logPath.EndsWith("\\") ? logPath : logPath + "\\") + "TomSync\\";
            LogPath = logPathSlash;
            UserServer = "";
        }
    }
}
