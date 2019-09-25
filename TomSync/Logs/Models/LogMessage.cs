using System;

namespace TomSync.Logs.Models
{
    public class LogMessage
    {
        public DateTime DateTime { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }

        public LogMessage(string logMessage)
        {
            var parts = logMessage.Split('|');
            if (parts.Length == 4)
            {
                if (DateTime.TryParse(parts[0], out DateTime dateTime))
                {
                    DateTime = dateTime;
                }
                Level = parts[1];
                Logger = parts[2];
                Message = parts[3];
            }
            else
            {
                Message = logMessage;
            }
        }
    }
}
