using System.Windows;
using TomSync.Models;

namespace TomSync
{
    /// <summary>
    /// Логика взаимодействия для Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private readonly SyncCore core = new SyncCore();

        public Login():this(true)
        {
        }

        public Login(bool autoLogin)
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            LastUser lastUser = Settings.LoadLastUser();

            if (autoLogin 
                && lastUser.Remember
                && !string.IsNullOrEmpty(lastUser.User)
                && !string.IsNullOrEmpty(lastUser.Server)
                && !string.IsNullOrEmpty(lastUser.Pass))
            {
                AuthorizationAsync(lastUser.Server, lastUser.User, lastUser.Pass, lastUser.Remember);
            }

            if (lastUser.Remember)
            {
                //AuthorizationAsync(Settings.Server.AbsoluteUri, Settings.User, Settings.Pass);
                Server_TextBox.Text = lastUser.Server;
                Login_TextBox.Text = lastUser.User;
                Password_TextBox.Password = lastUser.Pass;
                Remember_CheckBox.IsChecked = lastUser.Remember;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string server = Server_TextBox.Text;
            string login = Login_TextBox.Text;
            string pass = Password_TextBox.Password;
            bool? remember = Remember_CheckBox.IsChecked;

            Auth_Button.IsEnabled = false;
            AuthorizationAsync(server, login, pass, remember);

        }
        private async void AuthorizationAsync(string server, string user, string password, bool? remember)
        {

            if (await core.AuthorizationAsync(server, user, password))
            {
                MainWindow mainWindow = new MainWindow(core);
                mainWindow.Show();
                if (remember == true)
                {
                    Settings.SaveLastUser();
                }
                Close();
            }
            else { Error_TextBlock.Text = "Не удалось подключиться к серверу. Проверьте правильность введенных данных"; }
            Auth_Button.IsEnabled = true;
        }
    }
}
