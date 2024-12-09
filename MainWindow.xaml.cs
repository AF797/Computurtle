using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using System;
using System.Security.Claims;
using System.Threading;
using System.Timers;
using WinRT.Interop;
using Microsoft.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Dispatching;
using System.Windows.Forms;  // NotifyIcon 사용
using System.Drawing;  // Icon 사용

namespace tutu2
{
    public sealed partial class MainWindow : Window
    {
        private System.Timers.Timer _timer;
        private TimeSpan _elapsedTime;
        private AppWindow appWindow;

        public MainWindow()
        {
            this.InitializeComponent(); // Ensure InitializeComponent is called

            // Use WinRT.Interop to get the window handle
            var windowHandle = WindowNative.GetWindowHandle(this);
            appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(windowHandle));

            // Set window size
            appWindow.Resize(new SizeInt32(250, 400));  // Set size to 250x400

            // Disable resize and maximize
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }

            // Set window as frameless and transparent
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Add event handler for the window close
            appWindow.Closing += AppWindow_Closing;

            StartTimer();
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;  // Prevent window from closing
            this.HideWindow();
        }

        private void HideWindow()
        {
            appWindow.Hide();
        }

        private void ShowWindow()
        {
            appWindow.Show();
        }

        private void StartTimer()
        {
            _elapsedTime = TimeSpan.Zero;

            _timer = new System.Timers.Timer(1000); // Execute every second
            _timer.Elapsed += (sender, e) =>
            {
                _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(1));
                DispatcherQueue.TryEnqueue(() =>
                {
                    TimerDisplay.Text = _elapsedTime.ToString(@"hh\:mm\:ss");
                    UpdateImageBasedOnTime();
                });
            };
            _timer.Start();
        }

        private void UpdateImageBasedOnTime()
        {
            BitmapImage newImageSource = new BitmapImage(new Uri("ms-appx:///Assets/1.jpg"));

            // 시간에 따라 이미지를 변경
            if (_elapsedTime.TotalSeconds < 10)
            {
                newImageSource = new BitmapImage(new Uri("ms-appx:///Assets/1.jpg"));
            }
            else if (_elapsedTime.TotalSeconds < 20)
            {
                newImageSource = new BitmapImage(new Uri("ms-appx:///Assets/2.jpg"));
            }
            else if (_elapsedTime.TotalSeconds < 30)
            {
                newImageSource = new BitmapImage(new Uri("ms-appx:///Assets/3.jpg"));
            }
            else if (_elapsedTime.TotalSeconds < 40)
            {
                newImageSource = new BitmapImage(new Uri("ms-appx:///Assets/4.jpg"));
            }
            else
            {
                newImageSource = new BitmapImage(new Uri("ms-appx:///Assets/5.jpg"));
            }

            // 현재 이미지와 새 이미지가 다를 경우에만 업데이트
            if (DynamicImage.Source == null || !((BitmapImage)DynamicImage.Source).UriSource.Equals(newImageSource.UriSource))
            {
                DynamicImage.Source = newImageSource;
            }
        }

        private void OpenAlarmWindow_Click(object sender, RoutedEventArgs e)
        {
            var alarmWindow = new Window();
            var frame = new Frame();
            frame.Navigate(typeof(Alarm));
            alarmWindow.Content = frame;

            // Set size using AppWindow
            var appWindow = alarmWindow.AppWindow;
            appWindow.Resize(new SizeInt32(400, 600));

            // Activate alarm window
            alarmWindow.Activate();
        }

        private void OpenCalendarWindow_Click(object sender, RoutedEventArgs e)
        {
            var calendarWindow = new Window();
            var frame = new Frame();
            frame.Navigate(typeof(calender));
            calendarWindow.Content = frame;

            // Set size using AppWindow
            var appWindow = calendarWindow.AppWindow;
            //appWindow.Resize(new SizeInt32(400, 600));

            // Activate alarm window
            calendarWindow.Activate();
        }
    }
}
