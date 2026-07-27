using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using System.Windows.Media.Animation;
using System.Windows.Media;
using DesktopCat.Services;
using DesktopCat.UI;
using Forms = System.Windows.Forms;

namespace DesktopCat
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private DispatcherTimer _hideBubbleTimer;
        private DispatcherTimer _animationTimer;
        private DispatcherTimer _schedulerTimer;
        private Forms.NotifyIcon? _notifyIcon;
        private Storyboard? _currentAnimation;
        private TodoTask? _currentActiveTask;
        private System.Collections.Generic.Queue<TodoTask> _notificationQueue = new System.Collections.Generic.Queue<TodoTask>();
        private DispatcherTimer _leftClickAnimTimer;
        private DateTime _lastLeftClickAnimTime = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            _settings = SettingsManager.Load();
            SettingsManager.OnSettingsChanged += (s) => {
                Dispatcher.Invoke(() => {
                    _settings = s;
                    ApplySettings();
                });
            };

            _hideBubbleTimer = new DispatcherTimer();
            _hideBubbleTimer.Tick += (s, e) => {
                NotificationBubble.Visibility = Visibility.Collapsed;
                _currentActiveTask = null;
                _hideBubbleTimer.Stop();
                CheckNotificationQueue();
            };

            _animationTimer = new DispatcherTimer();
            _animationTimer.Tick += (s, e) => {
                SetIdleAnimation();
                _animationTimer.Stop();
            };

            _schedulerTimer = new DispatcherTimer();
            _schedulerTimer.Interval = TimeSpan.FromSeconds(30); // Проверка каждые 30 сек
            _schedulerTimer.Tick += SchedulerTimer_Tick;
            _schedulerTimer.Start();

            _leftClickAnimTimer = new DispatcherTimer();
            _leftClickAnimTimer.Interval = TimeSpan.FromSeconds(10);
            _leftClickAnimTimer.Tick += (s, e) => {
                SetIdleAnimation();
                _leftClickAnimTimer.Stop();
            };

            SetupTrayIcon();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void SchedulerTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            bool settingsChanged = false;

            foreach (var task in _settings.TodoList)
            {
                // Сбрасываем флаг завершения для постоянных задач, если наступил новый день
                if (task.IsPermanent && task.IsCompleted && task.ScheduledTime.Date < now.Date)
                {
                    task.IsCompleted = false;
                    // Обновляем дату задачи на сегодня, чтобы она снова сработала
                    task.ScheduledTime = new DateTime(now.Year, now.Month, now.Day, task.ScheduledTime.Hour, task.ScheduledTime.Minute, 0);
                    settingsChanged = true;
                }

                // Проверяем, наступило ли время задачи
                if (!task.IsCompleted &&
                    task.ScheduledTime.Hour == now.Hour &&
                    task.ScheduledTime.Minute == now.Minute &&
                    task.ScheduledTime.Date <= now.Date)
                {
                    if (_settings.AreNotificationsEnabled)
                    {
                        // Добавляем в очередь
                        _notificationQueue.Enqueue(task);
                    }

                    // Отмечаем как "временно" показанную (чтобы таймер не спамил каждую секунду)
                    // Но по-настоящему Completed она станет при нажатии галочки (или останется висеть в списке)
                    task.IsCompleted = true;
                    settingsChanged = true;
                }
            }

            if (settingsChanged)
            {
                SettingsManager.Save(_settings);
            }

            CheckNotificationQueue();
        }

        private void CheckNotificationQueue()
        {
            // Если пузырь сейчас пуст и в очереди есть задачи
            if (_currentActiveTask == null && _notificationQueue.Count > 0)
            {
                var nextTask = _notificationQueue.Dequeue();
                _currentActiveTask = nextTask;
                ShowBubble($"Напоминание:\n{nextTask.Title}", true);

                // Воспроизводим звук если включен
                if (_settings.IsSoundEnabled)
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }
            }
        }

        private void SetIdleAnimation()
        {
            // Возвращаем основную картинку/GIF
            LoadSkin(_settings.CatSkin);
        }

        private void SetActiveAnimation()
        {
            // Если есть отдельная картинка/GIF для активности - включаем её
            if (!string.IsNullOrEmpty(_settings.CatActiveSkin))
            {
                LoadSkin(_settings.CatActiveSkin);
            }
            else
            {
                // Fallback, если активного скина нет - просто оставляем текущий
                LoadSkin(_settings.CatSkin);
            }
        }

        private void LoadSkin(string skinName)
        {
            try
            {
                string packUri = $"pack://application:,,,/Assets/{skinName}";
                string localUri = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", skinName);

                Uri uri;
                if (System.IO.File.Exists(localUri))
                {
                    uri = new Uri(localUri);
                }
                else
                {
                    uri = new Uri(packUri);
                }

                var img = new System.Windows.Media.Imaging.BitmapImage(uri);
                WpfAnimatedGif.ImageBehavior.SetAnimatedSource(CatSprite, img);
            }
            catch
            {
                // Fallback
            }
        }

        private void StartRotateAnimation(double fromAngle, double toAngle, double durationSeconds)
        {
            if (_currentAnimation != null)
            {
                _currentAnimation.Stop();
            }

            var animation = new DoubleAnimation
            {
                From = fromAngle,
                To = toAngle,
                Duration = new Duration(TimeSpan.FromSeconds(durationSeconds)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            Storyboard.SetTargetName(animation, "CatRotation");
            Storyboard.SetTargetProperty(animation, new PropertyPath(RotateTransform.AngleProperty));

            _currentAnimation = new Storyboard();
            _currentAnimation.Children.Add(animation);

            _currentAnimation.Begin(this, true);
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();

            try
            {
                var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))?.Stream;
                if (iconStream != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Desktop Cat";

            var contextMenu = new Forms.ContextMenuStrip();

            var settingsItem = new Forms.ToolStripMenuItem("Настройки");
            settingsItem.Click += (s, e) => OpenSettings();

            var exitItem = new Forms.ToolStripMenuItem("Выход");
            exitItem.Click += (s, e) => System.Windows.Application.Current.Shutdown();

            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OpenSettings(int tabIndex = 0)
        {
            var sw = new SettingsWindow(tabIndex);
            if (sw.ShowDialog() == true)
            {
                _settings = SettingsManager.Load();
                ApplySettings();
            }
        }

        private void ApplySettings()
        {
            double newSize = _settings.CatSize > 0 ? _settings.CatSize : 150.0;
            this.Width = newSize + 250; // Увеличенный запас для широких облачков текста
            this.Height = newSize + 250; // Значительный запас сверху для всплывающего облачка уведомлений и анимации
            CatSprite.Width = newSize;
            CatSprite.Height = newSize;

            DevModeBorder.Visibility = _settings.IsDevMode ? Visibility.Visible : Visibility.Hidden;

            // Применяем настройки облачка
            double scale = (_settings.CloudSize > 0 ? _settings.CloudSize : 200.0) / 200.0;
            CloudScale.ScaleX = scale;
            CloudScale.ScaleY = scale;
            CloudTransform.X = _settings.CloudOffsetX;
            CloudTransform.Y = _settings.CloudOffsetY;

            CatSprite.Opacity = _settings.AreNotificationsEnabled ? 1.0 : 0.5;

            LoadSkin(_settings.CatSkin);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettings();
            SetIdleAnimation();

            // Запускаем очень медленное и плавное покачивание один раз при старте
            StartRotateAnimation(-2, 2, 4.0);

            if (_settings.LastX >= 0 && _settings.LastY >= 0)
            {
                this.Left = _settings.LastX;
                this.Top = _settings.LastY;
            }
            else
            {
                CenterCatOnScreen();
            }

            ShowBubble("Мяу! Я готов.");
        }

        public void CenterCatOnScreen()
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Left + (desktopWorkingArea.Width - this.Width) / 2;
            this.Top = desktopWorkingArea.Top + (desktopWorkingArea.Height - this.Height) / 2;
            _settings.LastX = this.Left;
            _settings.LastY = this.Top;
            SettingsManager.Save(_settings);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _settings.LastX = this.Left;
            _settings.LastY = this.Top;
            SettingsManager.Save(_settings);

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }

        private void ShowBubble(string text, bool showActionButtons = false)
        {
            NotificationText.Text = text;
            NotificationActionPanel.Visibility = showActionButtons ? Visibility.Visible : Visibility.Collapsed;
            NotificationBubble.Visibility = Visibility.Visible;

            _hideBubbleTimer.Interval = TimeSpan.FromSeconds(_settings.BubbleDurationSeconds > 0 ? _settings.BubbleDurationSeconds : 15);
            _hideBubbleTimer.Stop();
            _hideBubbleTimer.Start();

            SetActiveAnimation();
            _animationTimer.Interval = TimeSpan.FromSeconds(_settings.ActiveAnimationDuration > 0 ? _settings.ActiveAnimationDuration : 5);
            _animationTimer.Stop();
            _animationTimer.Start();

            StartCloudBobbingAnimation();
        }

        private void StartCloudBobbingAnimation()
        {
            var bobbingAnim = new DoubleAnimation
            {
                From = -5,
                To = 5,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            CloudBobbingTransform.BeginAnimation(TranslateTransform.YProperty, bobbingAnim);
        }

        private void NotifCompleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentActiveTask != null)
            {
                // Начисляем опыт
                _settings.Exp += 10;
                _settings.TotalTasksCompleted++;
                if (_settings.Exp >= _settings.Level * 50)
                {
                    _settings.Exp -= _settings.Level * 50;
                    _settings.Level++;
                }

                // Задача уже отмечена как Completed в таймере, если не перманентная - удаляем
                if (!_currentActiveTask.IsPermanent)
                {
                    _settings.TodoList.RemoveAll(t => t.Id == _currentActiveTask.Id);
                }
                SettingsManager.Save(_settings);
            }
            NotificationBubble.Visibility = Visibility.Collapsed;
            _hideBubbleTimer.Stop();
            _currentActiveTask = null;
            CheckNotificationQueue();
        }

        public void ToggleTestBubble(bool show)
        {
            if (show)
            {
                NotificationText.Text = "Это тестовое уведомление. Настройте размеры и отступы!";
                NotificationActionPanel.Visibility = Visibility.Collapsed;
                NotificationBubble.Visibility = Visibility.Visible;
                StartCloudBobbingAnimation();
            }
            else
            {
                NotificationBubble.Visibility = Visibility.Collapsed;
            }
        }

        private void NotifCloseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentActiveTask != null)
            {
                // Если просто закрыли, задача считается невыполненной (остается в календаре красной/неактивной)
                _currentActiveTask.IsCompleted = false;
                SettingsManager.Save(_settings);
            }
            NotificationBubble.Visibility = Visibility.Collapsed;
            _hideBubbleTimer.Stop();
            _currentActiveTask = null;
            CheckNotificationQueue();
        }

        private bool _isDragging = false;
        private System.Windows.Point _startPoint;

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = false;
                _startPoint = e.GetPosition(this);
                RadialMenuPopup.IsOpen = false;
                this.CaptureMouse();
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (RadialMenuPopup.IsOpen)
                {
                    RadialMenuPopup.IsOpen = false;
                }
                else
                {
                    PositionRadialButtons();
                    RadialMenuPopup.IsOpen = true;
                }
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                System.Windows.Point currentPoint = e.GetPosition(this);
                if (Math.Abs(currentPoint.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPoint.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    this.ReleaseMouseCapture();
                    this.DragMove();
                    _settings.LastX = this.Left;
                    _settings.LastY = this.Top;
                    SettingsManager.Save(_settings);
                }
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.ReleaseMouseCapture();
                if (!_isDragging)
                {
                    // Это был обычный клик - запускаем новую анимацию, если прошло 5 сек
                    if ((DateTime.Now - _lastLeftClickAnimTime).TotalSeconds >= 5)
                    {
                        _lastLeftClickAnimTime = DateTime.Now;
                        LoadSkin("cat1Anim3.gif");

                        _leftClickAnimTimer.Stop();
                        _leftClickAnimTimer.Start();
                    }
                }
                _isDragging = false;
            }
        }

        private void PositionRadialButtons()
        {
            // Обновляем цвета кнопок в зависимости от статуса настроек
            BtnRadNotif.Foreground = _settings.AreNotificationsEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
            BtnRadSound.Foreground = _settings.IsSoundEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);

            // Радиус круга, по которому располагаются кнопки
            double radius = 70;
            // Центр Canvas
            double centerX = 150; // Половина Width (300)
            double centerY = 150; // Половина Height (300)

            var buttons = new[] { BtnRadSettings, BtnRadCalendar, BtnRadStats, BtnRadNotif, BtnRadSound, BtnRadExit };
            double angleStep = 2 * Math.PI / buttons.Length;
            // Сдвигаем начальный угол, чтобы первая кнопка была сверху
            double startAngle = -Math.PI / 2;

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                double angle = startAngle + i * angleStep;

                // Вычисляем координаты X и Y
                double x = centerX + radius * Math.Cos(angle) - (btn.Width / 2);
                double y = centerY + radius * Math.Sin(angle) - (btn.Height / 2);

                System.Windows.Controls.Canvas.SetLeft(btn, x);
                System.Windows.Controls.Canvas.SetTop(btn, y);
            }
        }

        private void RadialSettings_Click(object sender, RoutedEventArgs e)
        {
            RadialMenuPopup.IsOpen = false;
            OpenSettings(1); // 1 = Вкладка настроек/внешнего вида
        }

        private void RadialCalendar_Click(object sender, RoutedEventArgs e)
        {
            RadialMenuPopup.IsOpen = false;
            OpenSettings(0); // 0 = Вкладка задач
        }

        private void RadialStats_Click(object sender, RoutedEventArgs e)
        {
            RadialMenuPopup.IsOpen = false;
            OpenSettings(2); // 2 = Вкладка статистики
        }

        private void RadialNotif_Click(object sender, RoutedEventArgs e)
        {
            _settings.AreNotificationsEnabled = !_settings.AreNotificationsEnabled;
            BtnRadNotif.Foreground = _settings.AreNotificationsEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
            CatSprite.Opacity = _settings.AreNotificationsEnabled ? 1.0 : 0.5;
            SettingsManager.Save(_settings);
        }

        private void RadialSound_Click(object sender, RoutedEventArgs e)
        {
            _settings.IsSoundEnabled = !_settings.IsSoundEnabled;
            BtnRadSound.Foreground = _settings.IsSoundEnabled ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGreen) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LightGray);
            SettingsManager.Save(_settings);
        }
    }
}