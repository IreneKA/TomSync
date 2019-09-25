using System;

namespace TomSync.Models
{
    public class LastUser
    {
        public string Server { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public bool Remember { get; set; }

        //со значениями по умолчанию
        public LastUser()
        {
            Server = "";
            User = "";
            Pass = "";
            Remember = true;
        }

        public void SetServerFromUri(Uri serverUri)
        {
            Server = serverUri.DnsSafeHost;
        }
    }
}
