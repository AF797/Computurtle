using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Foundation;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace tutu2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class calender : Page
    {
        private DateTime currentDisplayedMonth;
        private DateTime selectedDate;
        private readonly Dictionary<DateTime, List<Event>> eventsByDate = new();
        private Event selectedEventToEdit = null;

        // Variables for dragging functionality
        private bool isDragging = false;
        private Point clickPosition;
        public calender()
        {
            InitializeComponent();
            currentDisplayedMonth = DateTime.Now;
            selectedDate = DateTime.Now; // �ʱ� ���� ��¥ ����
            UpdateMonthYearLabel();
            LoadEventsFromFile();
            GenerateCalendar(currentDisplayedMonth);
        }
        #region �޷� �� �� �̵� ���� �޼���
        private void UpdateMonthYearLabel()
        {
            MonthYearLabel.Text = $"{currentDisplayedMonth:yyyy�� MM��}";
        }

        private void GenerateCalendar(DateTime month)
        {
            CalendarBodyGrid.Children.Clear();
            DateTime firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            int startDay = (int)firstDayOfMonth.DayOfWeek;

            int day = 1;

            for (int row = 0; row < 6; row++) // �ִ� 6��
            {
                for (int col = 0; col < 7; col++) // ������ (7��)
                {
                    if ((row == 0 && col < startDay) || day > daysInMonth)
                        continue; // ����/���� ���� �� ĭ �ǳʶٱ�

                    var currentDate = new DateTime(month.Year, month.Month, day);
                    var dayBorder = CreateDayBorder(currentDate);
                    Grid.SetRow(dayBorder, row);
                    Grid.SetColumn(dayBorder, col);
                    CalendarBodyGrid.Children.Add(dayBorder);

                    day++;
                }
            }
        }

        private Border CreateDayBorder(DateTime date)
        {
            Border dayBorder = new()
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1),
                Tag = date,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Black)
            };

            var dayPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            var dayText = new TextBlock
            {
                Text = date.Day.ToString(),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            dayPanel.Children.Add(dayText);
            AddEventLabelsToDayPanel(date, dayPanel);
            dayBorder.Child = dayPanel;

            // ��¥ Ŭ�� �̺�Ʈ �ڵ鷯
            dayBorder.Tapped += DayBorder_Tapped;
            return dayBorder;
        }

        private void AddEventLabelsToDayPanel(DateTime date, StackPanel panel)
        {
            if (!eventsByDate.TryGetValue(date, out var eventList))
                return;

            foreach (var ev in eventList)
            {
                var eventPanel = CreateEventPanel(ev);
                panel.Children.Add(eventPanel);
            }
        }

        private StackPanel CreateEventPanel(Event ev)
        {
            StackPanel eventPanel = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock eventText = new()
            {
                Text = ev.Title,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                FontSize = 10,
                Margin = new Thickness(0, 5, 0, 0)
            };

            Button deleteButton = new()
            {
                Content = "����",
                Tag = ev,
                Width = 20,
                Height = 10,
                Margin = new Thickness(5, 5, 0, 5)
            };
            deleteButton.Click += DeleteEventButton_Click;

            eventPanel.Children.Add(eventText);
            eventPanel.Children.Add(deleteButton);
            return eventPanel;
        }
        #endregion

        #region �̺�Ʈ ���� �޼���
        private void DayBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is DateTime date)
            {
                selectedDate = date;

                double popupHorizontalOffset = -200;
                double popupVerticalOffset = -200;

                // �˾� ��ġ ������Ʈ
                AddEventPopup.HorizontalOffset = popupHorizontalOffset;
                AddEventPopup.VerticalOffset = popupVerticalOffset;

                // �˾� ���� ä��� (��: ��¥�� �´� �̺�Ʈ ��������)
                ConfigurePopupForSelectedDate();

                // �˾� ����
                AddEventPopup.IsOpen = true;

                // �巡�� ��� ����
                isDragging = false; // �巡�� ���� �ʱ�ȭ
                AddEventPopup.PointerPressed += PopupPointerPressed;
                AddEventPopup.PointerMoved += PopupPointerMoved;
                AddEventPopup.PointerReleased += PopupPointerReleased;
            }
        }

        private bool IsPointerOverInputFields(PointerRoutedEventArgs e)
        {
            // �Է� �ʵ�� ��ư�� ������ �������� Ŭ���Ǿ����� Ȯ��
            var inputFields = new UIElement[]
            {
                TitleTextBox, DescriptionTextBox, EventTimePicker, ParticipantsTextBox, LocationTextBox, DeleteEventButton
            };

            foreach (var field in inputFields)
            {
                if (field != null)
                {
                    // Ŭ���� ��ġ�� �����ɴϴ�.
                    var pointerPosition = e.GetCurrentPoint(field).Position;

                    // FrameworkElement�� ĳ�����Ͽ� ActualWidth, ActualHeight ���
                    if (field is FrameworkElement frameworkElement)
                    {
                        // ����� ���� ũ��� ��ġ�� �����ɴϴ�.
                        var bounds = frameworkElement.TransformToVisual(AddEventPopup).TransformBounds(new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight));

                        // Ŭ�� ��ġ�� �ʵ� ���� �ȿ� �ִ��� Ȯ��
                        if (bounds.Contains(pointerPosition))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void PopupPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // Ŭ���� ���� �Է� �ʵ尡 �ƴϸ� �巡�׸� ����
            if (!IsPointerOverInputFields(e))
            {
                isDragging = true;
                clickPosition = e.GetCurrentPoint(AddEventPopup).Position;
            }

            // Ŭ���� �ʵ忡 ��Ŀ���� ����
            if (e.OriginalSource is TextBox clickedTextBox)
            {
                clickedTextBox.Focus(FocusState.Programmatic);
            }
        }


        private void PopupPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging)
            {
                var currentPosition = e.GetCurrentPoint(AddEventPopup).Position;

                // �̵��� ���̸�ŭ �˾� ��ġ ������Ʈ
                double offsetX = currentPosition.X - clickPosition.X;
                double offsetY = currentPosition.Y - clickPosition.Y;

                AddEventPopup.HorizontalOffset += offsetX;
                AddEventPopup.VerticalOffset += offsetY;

                // ���� Ŭ�� ��ġ ������Ʈ
                clickPosition = currentPosition;
            }
        }

        private void PopupPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDragging = false; // �巡�� ���� ����
        }

        private void ConfigurePopupForSelectedDate()
        {
            if (eventsByDate.TryGetValue(selectedDate, out var eventList) && eventList.Count > 0)
            {
                selectedEventToEdit = eventList[0]; // ù ��° �̺�Ʈ ����
                FillPopupFields(selectedEventToEdit);
                DeleteEventButton.Visibility = Visibility.Visible;
            }
            else
            {
                ClearPopupFields();
                DeleteEventButton.Visibility = Visibility.Collapsed;
            }
        }

        private void FillPopupFields(Event ev)
        {
            TitleTextBox.Text = ev.Title;
            DescriptionTextBox.Text = ev.Description;
            EventTimePicker.Time = ev.EventTime.TimeOfDay;
            ParticipantsTextBox.Text = ev.Participants;
            LocationTextBox.Text = ev.Location;
        }

        private void ClearPopupFields()
        {
            TitleTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            EventTimePicker.Time = TimeSpan.Zero;
            ParticipantsTextBox.Text = string.Empty;
            LocationTextBox.Text = string.Empty;
        }

        private void AddEventButton_Click(object sender, RoutedEventArgs e)
        {
            var newEvent = new Event
            {
                Title = TitleTextBox.Text,
                Description = DescriptionTextBox.Text,
                EventTime = DateTime.Today.Add(EventTimePicker.Time),
                Participants = ParticipantsTextBox.Text,
                Location = LocationTextBox.Text
            };

            if (!eventsByDate.ContainsKey(selectedDate))
            {
                eventsByDate[selectedDate] = new List<Event>();
            }
            eventsByDate[selectedDate].Add(newEvent);
            GenerateCalendar(currentDisplayedMonth);
            AddEventPopup.IsOpen = false;

            // �̺�Ʈ ���Ͽ� ����
            SaveEventsToFile();
        }

        private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedEventToEdit == null) return;

            eventsByDate[selectedDate].Remove(selectedEventToEdit);
            selectedEventToEdit = null;
            GenerateCalendar(currentDisplayedMonth);
            AddEventPopup.IsOpen = false;

            // �̺�Ʈ ���Ͽ� ����
            SaveEventsToFile();
        }


        private void ClosePopupButton_Click(object sender, RoutedEventArgs e)
        {
            AddEventPopup.IsOpen = false;
        }
        #endregion

        #region �� �̵� ��ư
        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            currentDisplayedMonth = currentDisplayedMonth.AddMonths(-1);
            UpdateMonthYearLabel();
            GenerateCalendar(currentDisplayedMonth);
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            currentDisplayedMonth = currentDisplayedMonth.AddMonths(1);
            UpdateMonthYearLabel();
            GenerateCalendar(currentDisplayedMonth);
        }
        #endregion

        #region ������ Ŭ����
        public class Event
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime EventTime { get; set; }
            public string Participants { get; set; }
            public string Location { get; set; }
        }
        #endregion
        private void SaveEventsToFile()
        {
            string filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "events.txt");

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var date in eventsByDate.Keys)
                {
                    foreach (var ev in eventsByDate[date])
                    {
                        writer.WriteLine($"{date:yyyy-MM-dd}|{ev.Title}|{ev.Description}|{ev.EventTime:HH:mm}|{ev.Participants}|{ev.Location}");
                    }
                }
            }
        }

        private void LoadEventsFromFile()
        {
            string filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "events.txt");

            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split('|');
                        var date = DateTime.Parse(parts[0]);
                        var ev = new Event
                        {
                            Title = parts[1],
                            Description = parts[2],
                            EventTime = DateTime.Parse(parts[3]),
                            Participants = parts[4],
                            Location = parts[5]
                        };

                        if (!eventsByDate.ContainsKey(date))
                        {
                            eventsByDate[date] = new List<Event>();
                        }
                        eventsByDate[date].Add(ev);
                    }
                }
            }
        }


    }
}
