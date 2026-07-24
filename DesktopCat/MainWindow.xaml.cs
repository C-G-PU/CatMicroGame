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
        private NotificationService _notificationService;
        private DispatcherTimer _hideBubbleTimer;
        private DispatcherTimer _animationTimer;
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

            _notificationService = new NotificationService();
            _notificationService.OnNotificationReceived += OnNotificationReceived;

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

            SetupTrayIcon();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void StartAnimation(double fromAngle, double toAngle, double durationSeconds)
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

        private void SetIdleAnimation()
        {
            StartAnimation(-3, 3, 2.0); // Медленное, легкое покачивание
        }

        private void SetActiveAnimation()
        {
            StartAnimation(-15, 15, 0.4); // Быстрое, активное покачивание
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application; // Временно дефолтная иконка
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

            try
            {
                CatSprite.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/Assets/{_settings.CatSkin}"));
            }
            catch
            {
                // Fallback to default if skin not found
            }
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplySettings();
            SetIdleAnimation();

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

            bool success = await _notificationService.InitializeAsync();
            if (!success)
            {
                // Если нет доступа, показываем сообщение один раз
                ShowBubble("Нет доступа к уведомлениям!");
            }
            else
            {
                ShowBubble("Мяу! Я готов.");
            }
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

        private void OnNotificationReceived(string appName, string title, string message)
        {
            // Фильтрация: если список не пуст, и приложения нет в списке, игнорируем
            if (_settings.AllowedApps.Count > 0 &&
                !_settings.AllowedApps.Any(a => a.Equals(appName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                string displayText = $"{appName}: {title}";
                if (!string.IsNullOrEmpty(message))
                    displayText += $"\n{message}";

                ShowBubble(displayText);
            });
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
            else if (e.ChangedButton == MouseButton.Right)
            {
                ContextMenu menu = new ContextMenu();

                MenuItem settingsItem = new MenuItem { Header = "Настройки" };
                settingsItem.Click += (s, args) => {
                    var sw = new SettingsWindow();
                    if (sw.ShowDialog() == true)
                    {
                        _settings = SettingsManager.Load();
                    }
                };

                MenuItem exitItem = new MenuItem { Header = "Выход" };
                exitItem.Click += (s, args) => System.Windows.Application.Current.Shutdown();

                menu.Items.Add(settingsItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(exitItem);

                this.ContextMenu = menu;
            }
        }
    }
}