using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using DesktopCat.Services;
using DesktopCat.UI;

namespace DesktopCat
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private NotificationService _notificationService;
        private DispatcherTimer _hideBubbleTimer;

        public MainWindow()
        {
            InitializeComponent();

            _settings = SettingsManager.Load();
            _notificationService = new NotificationService();
            _notificationService.OnNotificationReceived += OnNotificationReceived;

            _hideBubbleTimer = new DispatcherTimer();
            _hideBubbleTimer.Interval = TimeSpan.FromSeconds(5);
            _hideBubbleTimer.Tick += (s, e) => {
                NotificationBubble.Visibility = Visibility.Collapsed;
                _hideBubbleTimer.Stop();
            };

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                exitItem.Click += (s, args) => Application.Current.Shutdown();

                menu.Items.Add(settingsItem);
                menu.Items.Add(new Separator());
                menu.Items.Add(exitItem);

                this.ContextMenu = menu;
            }
        }
    }
}