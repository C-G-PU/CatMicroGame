using System;
using System.Linq;
using System.Windows;
using DesktopCat.Services;

namespace DesktopCat.UI
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _currentSettings;

        private bool _isLoaded = false;

        public SettingsWindow(int tabIndex = 0)
        {
            InitializeComponent();
            MainTabControl.SelectedIndex = tabIndex;
            _currentSettings = SettingsManager.Load();

            SizeSlider.Value = _currentSettings.CatSize > 0 ? _currentSettings.CatSize : 150.0;
            AnimationSlider.Value = _currentSettings.ActiveAnimationDuration >= 10 ? _currentSettings.ActiveAnimationDuration : 10;
            BubbleSlider.Value = _currentSettings.BubbleDurationSeconds > 0 ? _currentSettings.BubbleDurationSeconds : 15;

            NotificationsCheck.IsChecked = _currentSettings.AreNotificationsEnabled;
            SoundCheck.IsChecked = _currentSettings.IsSoundEnabled;
            SoundPathText.Text = _currentSettings.NotificationSoundPath;
            SoundVolumeSlider.Value = _currentSettings.NotificationVolume;
            DevModeCheck.IsChecked = _currentSettings.IsDevMode;
            CloudSizeSlider.Value = _currentSettings.CloudSize;
            CloudTextSizeSlider.Value = _currentSettings.CloudTextSize > 0 ? _currentSettings.CloudTextSize : 14.0;
            CloudOffsetXSlider.Value = _currentSettings.CloudOffsetX;
            CloudOffsetYSlider.Value = _currentSettings.CloudOffsetY;

            // Обновление статистики
            LevelText.Text = _currentSettings.Level.ToString();
            ExpBar.Maximum = _currentSettings.Level * 50;
            ExpBar.Value = _currentSettings.Exp;
            ExpText.Text = $"{_currentSettings.Exp} / {_currentSettings.Level * 50}";
            TotalTasksText.Text = _currentSettings.TotalTasksCompleted.ToString();

            // Установка текущего персонажа
            foreach (System.Windows.Controls.ComboBoxItem item in CatSkinCombo.Items)
            {
                if (item.Tag.ToString() == _currentSettings.CatSkin)
                {
                    CatSkinCombo.SelectedItem = item;
                    break;
                }
            }
            if (CatSkinCombo.SelectedItem == null && CatSkinCombo.Items.Count > 0)
                CatSkinCombo.SelectedIndex = 0;

            RefreshTasksList();

            // Подгружаем кастомные скины
            LoadCustomSkins();

            _isLoaded = true;
        }

        private void LoadCustomSkins()
        {
            if (_currentSettings.CustomSkins != null)
            {
                foreach (var skin in _currentSettings.CustomSkins)
                {
                    bool exists = false;
                    foreach (System.Windows.Controls.ComboBoxItem item in CatSkinCombo.Items)
                    {
                        if (item.Tag.ToString() == skin) exists = true;
                    }
                    if (!exists)
                    {
                        CatSkinCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = skin, Tag = skin });
                        CatActiveSkinCombo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = skin, Tag = skin });
                    }
                }
            }

            // Пытаемся заново выбрать нужный элемент, если он загрузился
            foreach (System.Windows.Controls.ComboBoxItem item in CatSkinCombo.Items)
            {
                if (item.Tag.ToString() == _currentSettings.CatSkin)
                {
                    CatSkinCombo.SelectedItem = item;
                    break;
                }
            }

            foreach (System.Windows.Controls.ComboBoxItem item in CatActiveSkinCombo.Items)
            {
                string tag = item.Tag?.ToString() ?? "";
                if (tag == _currentSettings.CatActiveSkin)
                {
                    CatActiveSkinCombo.SelectedItem = item;
                    break;
                }
            }
            if (CatActiveSkinCombo.SelectedItem == null && CatActiveSkinCombo.Items.Count > 0)
                CatActiveSkinCombo.SelectedIndex = 0;

            foreach (System.Windows.Controls.ComboBoxItem item in AppThemeCombo.Items)
            {
                if (item.Tag?.ToString() == _currentSettings.AppTheme)
                {
                    AppThemeCombo.SelectedItem = item;
                    break;
                }
            }
            if (AppThemeCombo.SelectedItem == null && AppThemeCombo.Items.Count > 0)
                AppThemeCombo.SelectedIndex = 0;

            foreach (System.Windows.Controls.ComboBoxItem item in CloudTextColorCombo.Items)
            {
                if (item.Tag?.ToString() == _currentSettings.CloudTextColor)
                {
                    CloudTextColorCombo.SelectedItem = item;
                    break;
                }
            }
            if (CloudTextColorCombo.SelectedItem == null && CloudTextColorCombo.Items.Count > 0)
                CloudTextColorCombo.SelectedIndex = 0;

            ApplyThemeUI();
        }

        private void ApplyThemeUI()
        {
            if (_currentSettings.AppTheme == "Light")
            {
                MainBackgroundBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xBB, 0xFF, 0xFF, 0xFF));
            }
            else
            {
                MainBackgroundBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x99, 0x1A, 0x1A, 0x1A));
            }
        }

        private void AddCustomSkin_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Images (*.png;*.gif;*.jpg;*.jpeg)|*.png;*.gif;*.jpg;*.jpeg";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Копируем файл в рабочую директорию
                    string fileName = System.IO.Path.GetFileName(dialog.FileName);
                    string targetPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets");
                    if (!System.IO.Directory.Exists(targetPath))
                    {
                        System.IO.Directory.CreateDirectory(targetPath);
                    }
                    string destFile = System.IO.Path.Combine(targetPath, fileName);
                    System.IO.File.Copy(dialog.FileName, destFile, true);

                    // Добавляем в настройки
                    if (_currentSettings.CustomSkins == null)
                        _currentSettings.CustomSkins = new System.Collections.Generic.List<string>();

                    if (!_currentSettings.CustomSkins.Contains(fileName))
                    {
                        _currentSettings.CustomSkins.Add(fileName);
                    }

                    LoadCustomSkins();
                    SettingsManager.Save(_currentSettings);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Ошибка при загрузке картинки: " + ex.Message);
                }
            }
        }

        private void RefreshTasksList()
        {
            TasksList.ItemsSource = null;
            TasksList.ItemsSource = _currentSettings.TodoList;
        }

        private void MainCalendar_SelectedDatesChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                TaskDatePicker.SelectedDate = MainCalendar.SelectedDate.Value;
            }
        }

        private void TaskTitleInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TaskTitleInput.Text == "Новая задача...")
            {
                TaskTitleInput.Text = "";
            }
        }

        private string? _editingTaskId = null;

        private void TaskTitle_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is System.Windows.Controls.TextBlock tb && tb.Tag is string taskId)
            {
                var task = _currentSettings.TodoList.Find(t => t.Id == taskId);
                if (task != null)
                {
                    _editingTaskId = taskId;
                    TaskTitleInput.Text = task.Title;
                    TaskDatePicker.SelectedDate = task.ScheduledTime.Date;
                    TaskTimeInput.Text = task.ScheduledTime.ToString("HH:mm");
                    TaskPermanentCheck.IsChecked = task.IsPermanent;
                }
            }
        }

        private void TransferTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string taskId)
            {
                var task = _currentSettings.TodoList.Find(t => t.Id == taskId);
                if (task != null)
                {
                    task.ScheduledTime = task.ScheduledTime.AddDays(1);
                    task.IsCompleted = false;
                    task.IsCanceled = false;
                    RefreshTasksList();
                    SettingsManager.NotifyLiveUpdate(_currentSettings);
                }
            }
        }

        private void DuplicateTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string taskId)
            {
                var task = _currentSettings.TodoList.Find(t => t.Id == taskId);
                if (task != null)
                {
                    var newTask = new TodoTask
                    {
                        Title = task.Title,
                        ScheduledTime = task.ScheduledTime.AddDays(1),
                        IsPermanent = task.IsPermanent,
                        IsCompleted = false,
                        IsCanceled = false
                    };
                    _currentSettings.TodoList.Add(newTask);
                    RefreshTasksList();
                    SettingsManager.NotifyLiveUpdate(_currentSettings);
                }
            }
        }

        private void QuickTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && int.TryParse(btn.Tag?.ToString(), out int minutes))
            {
                var newTask = new TodoTask
                {
                    Title = "Быстрое уведомление",
                    ScheduledTime = DateTime.Now.AddMinutes(minutes),
                    IsPermanent = false,
                    IsCompleted = false,
                    IsCanceled = false
                };
                _currentSettings.TodoList.Add(newTask);
                RefreshTasksList();
                SettingsManager.NotifyLiveUpdate(_currentSettings);
            }
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleInput.Text.Trim();
            if (string.IsNullOrEmpty(title) || title == "Новая задача...") return;

            DateTime selectedDate = TaskDatePicker.SelectedDate ?? DateTime.Today;
            DateTime scheduledTime = selectedDate;

            if (TimeSpan.TryParse(TaskTimeInput.Text, out TimeSpan ts))
            {
                scheduledTime = selectedDate.Add(ts);
            }

            if (_editingTaskId != null)
            {
                var existingTask = _currentSettings.TodoList.Find(t => t.Id == _editingTaskId);
                if (existingTask != null)
                {
                    existingTask.Title = title;
                    existingTask.ScheduledTime = scheduledTime;
                    existingTask.IsPermanent = TaskPermanentCheck.IsChecked ?? false;
                }
                _editingTaskId = null;
            }
            else
            {
                var newTask = new TodoTask
                {
                    Title = title,
                    ScheduledTime = scheduledTime,
                    IsPermanent = TaskPermanentCheck.IsChecked ?? false,
                    IsCompleted = false,
                    IsCanceled = false
                };
                _currentSettings.TodoList.Add(newTask);
            }

            RefreshTasksList();

            TaskTitleInput.Text = "Новая задача...";
            SettingsManager.NotifyLiveUpdate(_currentSettings);
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string taskId)
            {
                _currentSettings.TodoList.RemoveAll(t => t.Id == taskId);
                RefreshTasksList();
                SettingsManager.NotifyLiveUpdate(_currentSettings);
            }
        }

        private void TaskComplete_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoaded && sender is System.Windows.Controls.Primitives.ToggleButton tb && tb.DataContext is TodoTask task)
            {
                if (task.IsCanceled && task.IsCompleted)
                {
                    // Взаимоисключение: если выполнили, то снимаем отмену
                    task.IsCanceled = false;
                }

                if (task.IsCompleted)
                {
                    _currentSettings.Exp += 10;
                    _currentSettings.TotalTasksCompleted++;
                    if (_currentSettings.Exp >= _currentSettings.Level * 50)
                    {
                        _currentSettings.Exp -= _currentSettings.Level * 50;
                        _currentSettings.Level++;
                    }
                }
                else
                {
                    // Снимаем галочку
                    _currentSettings.Exp -= 10;
                    _currentSettings.TotalTasksCompleted--;
                    if (_currentSettings.Exp < 0 && _currentSettings.Level > 1)
                    {
                        _currentSettings.Level--;
                        _currentSettings.Exp += _currentSettings.Level * 50;
                    }
                    if (_currentSettings.Exp < 0) _currentSettings.Exp = 0;
                    if (_currentSettings.TotalTasksCompleted < 0) _currentSettings.TotalTasksCompleted = 0;
                }

                UpdateStatsUI();
                RefreshTasksList(); // Обновит цвета
                SettingsManager.NotifyLiveUpdate(_currentSettings);
            }
        }

        private void TaskCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_isLoaded && sender is System.Windows.Controls.Primitives.ToggleButton tb && tb.DataContext is TodoTask task)
            {
                // Взаимоисключение: если отменили, снимаем "выполнено" и отбираем опыт если он был начислен
                if (task.IsCanceled && task.IsCompleted)
                {
                    task.IsCompleted = false;
                    _currentSettings.Exp -= 10;
                    _currentSettings.TotalTasksCompleted--;
                    if (_currentSettings.Exp < 0 && _currentSettings.Level > 1)
                    {
                        _currentSettings.Level--;
                        _currentSettings.Exp += _currentSettings.Level * 50;
                    }
                    if (_currentSettings.Exp < 0) _currentSettings.Exp = 0;
                    if (_currentSettings.TotalTasksCompleted < 0) _currentSettings.TotalTasksCompleted = 0;
                }

                UpdateStatsUI();
                RefreshTasksList(); // Обновит цвета
                SettingsManager.NotifyLiveUpdate(_currentSettings);
            }
        }

        private void UpdateStatsUI()
        {
            LevelText.Text = _currentSettings.Level.ToString();
            ExpBar.Maximum = _currentSettings.Level * 50;
            ExpBar.Value = _currentSettings.Exp;
            ExpText.Text = $"{_currentSettings.Exp} / {_currentSettings.Level * 50}";
            TotalTasksText.Text = _currentSettings.TotalTasksCompleted.ToString();
        }

        private void SyncApi_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("API интеграция в разработке. Дождитесь следующих обновлений!", "Инфо", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void LiveUpdate_Trigger(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            if (CatSkinCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                _currentSettings.CatSkin = selectedItem.Tag.ToString() ?? "cat.png";
            }

            if (CatActiveSkinCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedActiveItem)
            {
                _currentSettings.CatActiveSkin = selectedActiveItem.Tag?.ToString() ?? "";
            }

            if (AppThemeCombo.SelectedItem is System.Windows.Controls.ComboBoxItem themeItem)
            {
                _currentSettings.AppTheme = themeItem.Tag?.ToString() ?? "Dark";
                ApplyThemeUI();
            }

            if (CloudTextColorCombo.SelectedItem is System.Windows.Controls.ComboBoxItem colorItem)
            {
                _currentSettings.CloudTextColor = colorItem.Tag?.ToString() ?? "White";
            }

            _currentSettings.CatSize = SizeSlider.Value;

            _currentSettings.CloudSize = CloudSizeSlider.Value;
            _currentSettings.CloudTextSize = CloudTextSizeSlider.Value;
            _currentSettings.CloudOffsetX = CloudOffsetXSlider.Value;
            _currentSettings.CloudOffsetY = CloudOffsetYSlider.Value;
            _currentSettings.IsDevMode = DevModeCheck.IsChecked ?? false;
            _currentSettings.IsSoundEnabled = SoundCheck.IsChecked ?? true;
            _currentSettings.NotificationVolume = SoundVolumeSlider.Value;
            _currentSettings.AreNotificationsEnabled = NotificationsCheck.IsChecked ?? true;

            // Уведомляем главное окно о временных изменениях без записи в файл
            SettingsManager.NotifyLiveUpdate(_currentSettings);
        }

        private void ChooseSound_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav";
            if (dialog.ShowDialog() == true)
            {
                _currentSettings.NotificationSoundPath = dialog.FileName;
                SoundPathText.Text = dialog.FileName;
                SettingsManager.NotifyLiveUpdate(_currentSettings);
            }
        }

        private void CenterCatBtn_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Application.Current.MainWindow is DesktopCat.MainWindow mw)
            {
                mw.CenterCatOnScreen();
            }
        }

        private void TestBubble_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            if (System.Windows.Application.Current.MainWindow is DesktopCat.MainWindow mw)
            {
                mw.ToggleTestBubble(TestBubbleCheck.IsChecked ?? false);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _currentSettings.CatSize = SizeSlider.Value;
            _currentSettings.ActiveAnimationDuration = (int)AnimationSlider.Value;
            _currentSettings.BubbleDurationSeconds = (int)BubbleSlider.Value;

            _currentSettings.CloudSize = CloudSizeSlider.Value;
            _currentSettings.CloudTextSize = CloudTextSizeSlider.Value;
            _currentSettings.CloudOffsetX = CloudOffsetXSlider.Value;
            _currentSettings.CloudOffsetY = CloudOffsetYSlider.Value;
            _currentSettings.IsDevMode = DevModeCheck.IsChecked ?? false;
            _currentSettings.IsSoundEnabled = SoundCheck.IsChecked ?? true;
            _currentSettings.NotificationVolume = SoundVolumeSlider.Value;
            _currentSettings.AreNotificationsEnabled = NotificationsCheck.IsChecked ?? true;

            if (CatSkinCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                _currentSettings.CatSkin = selectedItem.Tag.ToString() ?? "cat.png";
            }
            if (CatActiveSkinCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedActiveItem)
            {
                _currentSettings.CatActiveSkin = selectedActiveItem.Tag?.ToString() ?? "";
            }
            if (AppThemeCombo.SelectedItem is System.Windows.Controls.ComboBoxItem themeItem2)
            {
                _currentSettings.AppTheme = themeItem2.Tag?.ToString() ?? "Dark";
            }
            if (CloudTextColorCombo.SelectedItem is System.Windows.Controls.ComboBoxItem colorItem2)
            {
                _currentSettings.CloudTextColor = colorItem2.Tag?.ToString() ?? "White";
            }

            SettingsManager.Save(_currentSettings);

            this.DialogResult = true;
            this.Close();
        }
    }
}