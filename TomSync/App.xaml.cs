using Hardcodet.Wpf.TaskbarNotification;
using System.Reflection;
using System.Windows;

namespace TomSync
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //create the notifyicon (it's a resource declared in NotifyIconResources.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Visibility = Visibility.Collapsed;
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
        }

        private void TaskbarIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            foreach (Window window in Windows)
            {
                window.Activate();
                window.Visibility = Visibility.Visible;
                window.Show();
            }
        }

        private void Close_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Logout_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void Logout()
        {
            Login login = new Login(false);
            foreach (Window window in Windows)
            {
                if (window != login)
                {
                    window.Close();
                }
            }
            login.Show();
        }
    }
}
