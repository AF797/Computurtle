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
            selectedDate = DateTime.Now; // 초기 선택 날짜 설정
            UpdateMonthYearLabel();
            LoadEventsFromFile();
            GenerateCalendar(currentDisplayedMonth);
        }
        #region 달력 및 월 이동 관련 메서드
        private void UpdateMonthYearLabel()
        {
            MonthYearLabel.Text = $"{currentDisplayedMonth:yyyy년 MM월}";
        }

        private void GenerateCalendar(DateTime month)
        {
            CalendarBodyGrid.Children.Clear();
            DateTime firstDayOfMonth = new DateTime(month.Year, month.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            int startDay = (int)firstDayOfMonth.DayOfWeek;

            int day = 1;

            for (int row = 0; row < 6; row++) // 최대 6주
            {
                for (int col = 0; col < 7; col++) // 일주일 (7일)
                {
                    if ((row == 0 && col < startDay) || day > daysInMonth)
                        continue; // 이전/다음 달의 빈 칸 건너뛰기

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

            // 날짜 클릭 이벤트 핸들러
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
                Content = "삭제",
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

        #region 이벤트 관련 메서드
        private void DayBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border clickedBorder && clickedBorder.Tag is DateTime date)
            {
                selectedDate = date;

                double popupHorizontalOffset = -200;
                double popupVerticalOffset = -200;

                // 팝업 위치 업데이트
                AddEventPopup.HorizontalOffset = popupHorizontalOffset;
                AddEventPopup.VerticalOffset = popupVerticalOffset;

                // 팝업 내용 채우기 (예: 날짜에 맞는 이벤트 가져오기)
                ConfigurePopupForSelectedDate();

                // 팝업 열기
                AddEventPopup.IsOpen = true;

                // 드래그 기능 설정
                isDragging = false; // 드래그 상태 초기화
                AddEventPopup.PointerPressed += PopupPointerPressed;
                AddEventPopup.PointerMoved += PopupPointerMoved;
                AddEventPopup.PointerReleased += PopupPointerReleased;
            }
        }

        private bool IsPointerOverInputFields(PointerRoutedEventArgs e)
        {
            // 입력 필드와 버튼을 포함한 영역에서 클릭되었는지 확인
            var inputFields = new UIElement[]
            {
                TitleTextBox, DescriptionTextBox, EventTimePicker, ParticipantsTextBox, LocationTextBox, DeleteEventButton
            };

            foreach (var field in inputFields)
            {
                if (field != null)
                {
                    // 클릭된 위치를 가져옵니다.
                    var pointerPosition = e.GetCurrentPoint(field).Position;

                    // FrameworkElement로 캐스팅하여 ActualWidth, ActualHeight 사용
                    if (field is FrameworkElement frameworkElement)
                    {
                        // 요소의 실제 크기와 위치를 가져옵니다.
                        var bounds = frameworkElement.TransformToVisual(AddEventPopup).TransformBounds(new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight));

                        // 클릭 위치가 필드 영역 안에 있는지 확인
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
            // 클릭된 곳이 입력 필드가 아니면 드래그를 시작
            if (!IsPointerOverInputFields(e))
            {
                isDragging = true;
                clickPosition = e.GetCurrentPoint(AddEventPopup).Position;
            }

            // 클릭된 필드에 포커스를 설정
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

                // 이동된 차이만큼 팝업 위치 업데이트
                double offsetX = currentPosition.X - clickPosition.X;
                double offsetY = currentPosition.Y - clickPosition.Y;

                AddEventPopup.HorizontalOffset += offsetX;
                AddEventPopup.VerticalOffset += offsetY;

                // 이전 클릭 위치 업데이트
                clickPosition = currentPosition;
            }
        }

        private void PopupPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDragging = false; // 드래그 상태 해제
        }

        private void ConfigurePopupForSelectedDate()
        {
            if (eventsByDate.TryGetValue(selectedDate, out var eventList) && eventList.Count > 0)
            {
                selectedEventToEdit = eventList[0]; // 첫 번째 이벤트 선택
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

            // 이벤트 파일에 저장
            SaveEventsToFile();
        }

        private void DeleteEventButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedEventToEdit == null) return;

            eventsByDate[selectedDate].Remove(selectedEventToEdit);
            selectedEventToEdit = null;
            GenerateCalendar(currentDisplayedMonth);
            AddEventPopup.IsOpen = false;

            // 이벤트 파일에 저장
            SaveEventsToFile();
        }


        private void ClosePopupButton_Click(object sender, RoutedEventArgs e)
        {
            AddEventPopup.IsOpen = false;
        }
        #endregion

        #region 월 이동 버튼
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

        #region 데이터 클래스
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
