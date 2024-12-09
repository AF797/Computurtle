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
        private static readonly string AlarmFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tutu2", "alarm.txt"); // 파일 경로를 설정합니다.

        // Windows API를 사용하여 입력 차단 및 창 제어
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
                    // 현재 창 핸들을 가져와서 복원 및 전면으로 가져오기
                    IntPtr hwnd = GetForegroundWindow();
                    if (IsIconic(hwnd))
                    {
                        ShowWindowAsync(hwnd, SW_RESTORE);
                    }
                    SetForegroundWindow(hwnd);

                    // 입력 차단 시작
                    BlockInput(true);

                    var alarmDialog = new ContentDialog
                    {
                        Title = "Alarm",
                        Content = $"{alarm.Name} - {alarm.AlarmTime:hh\\:mm}",
                        CloseButtonText = "Dismiss",
                        XamlRoot = this.XamlRoot,
                        DefaultButton = ContentDialogButton.Close
                    };

                    // 알람 창을 최상위 모달로 표시
                    await ShowContentDialogOnceAsync(alarmDialog);

                    // 알람이 울린 후 입력 차단 해제
                    BlockInput(false);

                    // 새로운 창을 열어서 동영상 재생
                    var alarmWindow = new Window();
                    var frame = new Frame();
                    frame.Navigate(typeof(VideoPlayerPage));
                    alarmWindow.Content = frame;

                    // 창 크기 설정
                    var appWindow = alarmWindow.AppWindow;
                    appWindow.Resize(new Windows.Graphics.SizeInt32(406, 720));

                    // 새 창 활성화
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
