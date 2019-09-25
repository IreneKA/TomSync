using System;

namespace TomSync.Models
{
    public class DirectoryItem
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }
        public bool? IsFolder { get; set; }
        public DateTime? LastModified { get; set; }
        public long? ContentLength { get; set; }
        public long? QuotaUsedBytes { get; set; }
        public string Size { get { return SetFormat(ContentLength ?? QuotaUsedBytes); } }

        public static string SetFormat(long? bytes)
        {
            if (bytes == null) return "";

            double dbytes = (double)bytes;
            int i = GetCount(dbytes, 0);

            dbytes /= Math.Pow(1024, i);
            string size = Math.Round(dbytes, 2).ToString();

            switch (i)
            {
                case 0: size += " Б";
                    break;
                case 1:
                    size += " КБ";
                    break;
                case 2:
                    size += " МБ";
                    break;
                case 3:
                    size += " ГБ";
                    break;
                case 4:
                    size += " ТБ";
                    break;
                default:
                    break;
            }
            return size;
        }

        private static int GetCount(double bytes, int i)
        {
            if (bytes >= 1024)
            {
                i++;
                return GetCount(bytes / 1024,i);
            }
            return i;
        }

        //для работы двойного клика по ListViewItem
        public MainWindow Window { get; set; }
        public void ShowDirOrDownload()
        {
            if (Window!=null)
            {
                Window.ShowDirOrDownload(this);
            }
        }
    }
}
