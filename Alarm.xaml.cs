using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace tutu2
{
    public sealed partial class Alarm : Page
    {
        public class AlarmItem
        {
            public string Name { get; set; }
            public TimeSpan AlarmTime { get; set; }

            public override string ToString()
            {
                return $"{Name};{AlarmTime}";
            }

            public static AlarmItem FromString(string data)
            {
                var parts = data.Split(';');
                return new AlarmItem
                {
                    Name = parts[0],
                    AlarmTime = TimeSpan.Parse(parts[1])
                };
            }
        }

        private List<AlarmItem> alarms = new List<AlarmItem>();
        private DispatcherTimer alarmTimer;
        private static readonly string AlarmFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tutu2", "alarm.txt"); // ���� ��θ� �����մϴ�.

        // Windows API�� ����Ͽ� �Է� ���� �� â ����
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BlockInput(bool fBlockIt);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        public Alarm()
        {
            this.InitializeComponent();
            LoadAlarmsFromFile();

            alarmTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            alarmTimer.Tick += CheckAlarms;
            alarmTimer.Start();
        }

        private void LoadAlarmsFromFile()
        {
            string directory = Path.GetDirectoryName(AlarmFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(AlarmFilePath))
            {
                var lines = File.ReadAllLines(AlarmFilePath);
                alarms = lines.Select(AlarmItem.FromString).ToList();

                AlarmsListBox.Items.Clear();
                foreach (var item in alarms)
                {
                    AddAlarmToListBox(item);
                }
            }
        }

        private void SaveAlarmsToFile()
        {
            string directory = Path.GetDirectoryName(AlarmFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var lines = alarms.Select(a => a.ToString()).ToArray();
            File.WriteAllLines(AlarmFilePath, lines);
        }

        private async void SetAlarmButton_Click(object sender, RoutedEventArgs e)
        {
            string alarmName = AlarmNameTextBox.Text;

            if (string.IsNullOrEmpty(alarmName) || AlarmTimePicker.SelectedTime == null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Please enter both alarm name and time.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            TimeSpan selectedTime = AlarmTimePicker.SelectedTime.Value;

            var alarmItem = new AlarmItem
            {
                Name = alarmName,
                AlarmTime = selectedTime
            };

            alarms.Add(alarmItem);
            alarms = alarms.OrderBy(a => a.AlarmTime).ToList();

            AlarmsListBox.Items.Clear();
            foreach (var item in alarms)
            {
                AddAlarmToListBox(item);
            }

            SaveAlarmsToFile();

            var successDialog = new ContentDialog
            {
                Title = "Success",
                Content = $"Alarm '{alarmName}' set for {selectedTime:hh\\:mm}.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await successDialog.ShowAsync();

            AlarmNameTextBox.Text = string.Empty;
            AlarmTimePicker.SelectedTime = null;
        }

        private void DeleteAlarm(AlarmItem alarmItem)
        {
            alarms.Remove(alarmItem);

            AlarmsListBox.Items.Clear();
            foreach (var item in alarms)
            {
                AddAlarmToListBox(item);
            }

            SaveAlarmsToFile();
        }

        private void AddAlarmToListBox(AlarmItem item)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var alarmTextBlock = new TextBlock
            {
                Text = $"{item.Name}: {item.AlarmTime:hh\\:mm}",
                VerticalAlignment = VerticalAlignment.Center
            };

            var deleteButton = new Button
            {
                Content = "X",
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            deleteButton.Click += (s, args) => DeleteAlarm(item);

            stackPanel.Children.Add(alarmTextBlock);
            stackPanel.Children.Add(deleteButton);

            AlarmsListBox.Items.Add(stackPanel);
        }

        private async void CheckAlarms(object sender, object e)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            foreach (var alarm in alarms)
            {
                if (alarm.AlarmTime <= currentTime && alarm.AlarmTime.Add(TimeSpan.FromSeconds(1)) > currentTime)
                {
                    // ���� â �ڵ��� �����ͼ� ���� �� �������� ��������
                    IntPtr hwnd = GetForegroundWindow();
                    if (IsIconic(hwnd))
                    {
                        ShowWindowAsync(hwnd, SW_RESTORE);
                    }
                    SetForegroundWindow(hwnd);

                    // �Է� ���� ����
                    BlockInput(true);

                    var alarmDialog = new ContentDialog
                    {
                        Title = "Alarm",
                        Content = $"{alarm.Name} - {alarm.AlarmTime:hh\\:mm}",
                        CloseButtonText = "Dismiss",
                        XamlRoot = this.XamlRoot,
                        DefaultButton = ContentDialogButton.Close
                    };

                    // �˶� â�� �ֻ��� ��޷� ǥ��
                    await ShowContentDialogOnceAsync(alarmDialog);

                    // �˶��� �︰ �� �Է� ���� ����
                    BlockInput(false);

                    // ���ο� â�� ��� ������ ���
                    var alarmWindow = new Window();
                    var frame = new Frame();
                    frame.Navigate(typeof(VideoPlayerPage));
                    alarmWindow.Content = frame;

                    // â ũ�� ����
                    var appWindow = alarmWindow.AppWindow;
                    appWindow.Resize(new Windows.Graphics.SizeInt32(406, 720));

                    // �� â Ȱ��ȭ
                    alarmWindow.Activate();

                    alarms.Remove(alarm);
                    AlarmsListBox.Items.Clear();
                    foreach (var item in alarms)
                    {
                        AddAlarmToListBox(item);
                    }
                    SaveAlarmsToFile();
                    break;
                }
            }
        }
        private static async Task ShowContentDialogOnceAsync(ContentDialog dialog)
        {
            await dialog.ShowAsync();
        }
    }
}
