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
            _hideBubbleTimer.Interval = TimeSpan.FromSeconds(5);
            _hideBubbleTimer.Tick += (s, e) => {
                NotificationBubble.Visibility = Visibility.Collapsed;
                _hideBubbleTimer.Stop();
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
                    // Время пришло! Показываем задачу
                    ShowBubble($"Напоминание:\n{task.Title}");

                    // Отмечаем как выполненную, чтобы не спамить
                    task.IsCompleted = true;
                    settingsChanged = true;
                }
            }

            if (settingsChanged)
            {
                SettingsManager.Save(_settings);
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

        private void OpenSettings()
        {
            var sw = new SettingsWindow();
            if (sw.ShowDialog() == true)
            {
                _settings = SettingsManager.Load();
                ApplySettings();
            }
        }

        private void ApplySettings()
        {
            double newSize = _settings.CatSize > 0 ? _settings.CatSize : 120.0;
            this.Width = newSize + 30; // Небольшой запас для кнопок и облачка
            this.Height = newSize + 30;
            CatSprite.Width = newSize;
            CatSprite.Height = newSize;

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
                var desktopWorkingArea = SystemParameters.WorkArea;
                this.Left = desktopWorkingArea.Right - this.Width - 50;
                this.Top = desktopWorkingArea.Bottom - this.Height;
            }

            ShowBubble("Мяу! Я готов.");
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

        private void ShowBubble(string text)
        {
            NotificationText.Text = text;
            NotificationBubble.Visibility = Visibility.Visible;
            _hideBubbleTimer.Stop();
            _hideBubbleTimer.Start();

            SetActiveAnimation();
            _animationTimer.Interval = TimeSpan.FromSeconds(_settings.ActiveAnimationDuration > 0 ? _settings.ActiveAnimationDuration : 5);
            _animationTimer.Stop();
            _animationTimer.Start();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
                _settings.LastX = this.Left;
                _settings.LastY = this.Top;
                SettingsManager.Save(_settings);
            }
            // Правый клик теперь обрабатывается через XAML ContextMenu у Image
        }
    }
}