using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TomSync.Models
{
    public class SyncTimer
    {
        public bool IsEnabled { get; set; }
        public DateTime StartDate { get; set; }
        public SyncTimerType Type { get; set; }
        public TimeSpan Period { get; set; }

        public SyncTimer()
        {
            IsEnabled = false;
            StartDate = DateTime.Now;
            Type = SyncTimerType.Once;
            Period = TimeSpan.Zero;
        }
    }

    public enum SyncTimerType
    {
        Once,
        EveryDay,
        Custom
    }
}
