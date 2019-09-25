using System;
using System.Collections.Generic;

namespace TomSync.Models
{
    public class SyncTask
    {
        public List<string> LocalPaths { get; set; }
        public Uri ServerDirectoryUri { get; set; }
        public DateTime CreateDate { get; private set; }
        public DateTime? LastSyncDate { get; private set; }
        public int BackupCount { get; set; }
        public SyncTimer SyncTimer { get; set; }
        public string PathsString
        {
            get
            {
                string paths = "";
                if (LocalPaths != null && LocalPaths.Count > 0)
                {
                    paths += LocalPaths[0];
                    for (int i = 1; i < LocalPaths.Count; i++)
                    {
                        paths += "\n" + LocalPaths[i];
                    }
                }
                return paths;
            }
        }
        public string ServerDirName
        {
            get
            {
                string url = ServerDirectoryUri.ToString().Replace(Settings.Server.ToString(), string.Empty);
                return url == "" ? "../" : url;
            }
        }

        public SyncTask(List<string> localPaths, Uri serverDirUri, DateTime createDate, SyncTimer syncTimer, DateTime? lastSyncDate = null,  int backupCount = 0)
        {
            LocalPaths = localPaths;
            ServerDirectoryUri = serverDirUri;
            CreateDate = createDate;
            LastSyncDate = lastSyncDate;
            BackupCount = backupCount;
            SyncTimer = syncTimer;
        }

        public static SyncTask CreateSyncTask(List<string> localPaths, Uri serverDirUri, SyncTimer syncTimer, int backupCount = 0)
        {
            SyncTask syncTask = new SyncTask(localPaths, serverDirUri, DateTime.Now, syncTimer, null, backupCount);
            return syncTask;
        }

        public void UpdateSyncDate()
        {
            LastSyncDate = DateTime.Now;
        }
    }
}
