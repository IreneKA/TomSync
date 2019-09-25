using System;
using System.Windows.Threading;

namespace TomSync
{
    public class SyncTimerController
    {
        private DispatcherTimer controlTimer = new DispatcherTimer();
        private TimeSpan interval = TimeSpan.FromMinutes(1);
        private SyncCore syncCore;
        public SyncTimerController(SyncCore syncCore)
        {
            this.syncCore = syncCore;

            controlTimer.Interval = interval;
            controlTimer.Tick += new EventHandler(controlTimer_Tick);
        }

        private void controlTimer_Tick(object sender, EventArgs e)
        {
            syncCore.CheckAllForSync();
        }

        public void Start()
        {
            controlTimer.Start();
        }
        public void Stop()
        {
            controlTimer.Stop();
        }
    }
}
