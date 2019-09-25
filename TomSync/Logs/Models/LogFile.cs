namespace TomSync.Logs.Models
{
    public class LogFile
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}
