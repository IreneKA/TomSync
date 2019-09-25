using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomSync.Models
{
    public class SpeedLimiter
    {
        public bool IsLimitDownload { get; set; }
        public bool IsLimitUpload { get; set; }
        public int DownloadSpeedLimit { get; set; } // Килобайт в секунду
        public int UploadSpeedLimit { get; set; } // Килобайт в секунду
        public SpeedLimiter()
        {
            IsLimitDownload = false;
            IsLimitUpload = false;
            DownloadSpeedLimit = 1000;
            UploadSpeedLimit = 1000;
        }
    }
}
