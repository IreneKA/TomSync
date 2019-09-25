using System.IO;
using System.Windows.Controls;
using TomSync.Models;

namespace TomSync
{
    public static class StatusBar
    {
        public static ProgressBar ProgressBar { get; set; }
        public static TextBlock ProgressTextBlock { get; set; }
        public static TextBlock MessageTextBlock { get; set; }
        public static TextBlock QuotaTextBlock { get; set; }
        public static Button CloseStreamButton { get; set; }
        private static Stream currentStream;
        public static void SetProgress(double progress)
        {
            if (progress < 0)
                progress = 0;
            else if (progress > 100)
                progress = 100;

            if (ProgressBar != null)
                ProgressBar.Value = progress;

            if (ProgressTextBlock != null)
                ProgressTextBlock.Text = $"{progress.ToString("0.00")}%";
        }

        internal static void ClearProgress()
        {
            if (ProgressBar != null)
                ProgressBar.Value = 0;

            if (ProgressTextBlock != null)
                ProgressTextBlock.Text = "";

            if (MessageTextBlock != null)
                MessageTextBlock.Text = "";
        }

        internal static void StartLoading()
        {
            if (ProgressBar != null)
                ProgressBar.IsIndeterminate = true;
        }

        internal static void StopLoading()
        {
            if (ProgressBar != null)
                ProgressBar.IsIndeterminate = false;
        }

        internal static void SetMessage(string message)
        {
            if (MessageTextBlock != null)
                MessageTextBlock.Text = message;
        }

        internal static void SetQuota(QuotaLimiter quota)
        {
            if (QuotaTextBlock != null)
            {
                string message = $"Использовано {DirectoryItem.SetFormat(quota.QuotaUsedBytes)} "
                    + (quota.IsLimited ? $"из {DirectoryItem.SetFormat(quota.QuotaBytes)}" : "");
                QuotaTextBlock.Text = message;
            }
        }

        internal static void SetStream(Stream stream)
        {
            currentStream = stream;
            ShowStopButton();
        }
        internal static void CloseStream()
        {
            currentStream.Dispose();
            HideStopButton();
        }

        internal static void ShowStopButton()
        {
            if (CloseStreamButton!=null)
            {
                CloseStreamButton.Visibility = System.Windows.Visibility.Visible;
                CloseStreamButton.IsEnabled = true;
            }
        }

        internal static void HideStopButton()
        {
            if (CloseStreamButton != null)
            {
                CloseStreamButton.Visibility = System.Windows.Visibility.Hidden;
                CloseStreamButton.IsEnabled = false;
            }
        }
    }
}
