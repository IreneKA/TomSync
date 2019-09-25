using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TomSync.Models;

namespace TomSync
{
    /// <summary>
    /// Логика взаимодействия для TimeSettings.xaml
    /// </summary>
    public partial class TimeSettings : Window
    {
        public int BackupCount { get; set; }
        public SyncTimer SyncTimer { get; set; }
        private bool timerIsEnabled = false;
        private DateTime timerStartDate = DateTime.Now;
        private SyncTimerType timerType = SyncTimerType.Once;
        private TimeSpan timerPeriod = TimeSpan.Zero;

        public TimeSettings()
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
        }
        public TimeSettings(SyncTimer syncTimer, int backupCount):this()
        {
            SyncTimer = syncTimer;

            BackupCount_TextBox.Text = backupCount.ToString();
            IsEnabled_CheckBox.IsChecked = syncTimer.IsEnabled;
            startDatePicker.SelectedDate = syncTimer.StartDate;
            startTimePicker.Value = syncTimer.StartDate;
            var checkedButton = container.Children.OfType<RadioButton>()
                                          .FirstOrDefault(r => r.Name == syncTimer.Type.ToString());
            checkedButton.IsChecked = true;
            Days.Value = syncTimer.Period.Days;
            Hours.Value = syncTimer.Period.Hours;
            Minutes.Value = syncTimer.Period.Minutes;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OkTimer_Button_Click(object sender, RoutedEventArgs e)
        {

            if (int.TryParse(BackupCount_TextBox.Text, out int backupCount))
                BackupCount = backupCount;
            else
                BackupCount = 0;

            if (IsEnabled_CheckBox.IsChecked != null)
                timerIsEnabled = IsEnabled_CheckBox.IsChecked.Value;

            if (timerIsEnabled)
            {
                if (startDatePicker.SelectedDate != null)
                    timerStartDate = startDatePicker.SelectedDate.Value.Date;
                if (startTimePicker.Value != null)
                    timerStartDate = timerStartDate.Date.AddTicks(startTimePicker.Value.Value.TimeOfDay.Ticks);

                var checkedButton = container.Children.OfType<RadioButton>()
                                          .FirstOrDefault(r => r.GroupName == "TimerType" && r.IsChecked == true);
                switch (checkedButton.Name)
                {
                    case "EveryDay":
                        timerType = SyncTimerType.EveryDay;
                        break;
                    case "Custom":
                        timerType = SyncTimerType.Custom;
                        break;
                    case "Once":
                    default:
                        timerType = SyncTimerType.Once;
                        break;
                }

                if (timerType == SyncTimerType.Custom)
                {
                    if (Days.Value != null)
                        timerPeriod=timerPeriod.Add(TimeSpan.FromDays(Days.Value.Value));
                    if (Hours.Value != null)
                        timerPeriod=timerPeriod.Add(TimeSpan.FromHours(Hours.Value.Value));
                    if (Minutes.Value != null)
                        timerPeriod=timerPeriod.Add(TimeSpan.FromMinutes(Minutes.Value.Value));
                }
            }

            SyncTimer = new SyncTimer
            {
                IsEnabled = timerIsEnabled,
                StartDate = timerStartDate,
                Type = timerType,
                Period = timerPeriod
            };

            DialogResult = true;
        }

        private void CancelTimer_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Custom_Checked(object sender, RoutedEventArgs e)
        {
            TimerPeriodPicker.Visibility = Visibility.Visible;
        }

        private void Custom_Unchecked(object sender, RoutedEventArgs e)
        {
            TimerPeriodPicker.Visibility = Visibility.Hidden;
        }

        private void IsEnabled_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TimerSettings.IsEnabled = true;
        }

        private void IsEnabled_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            TimerSettings.IsEnabled = false;
        }
    }
}
