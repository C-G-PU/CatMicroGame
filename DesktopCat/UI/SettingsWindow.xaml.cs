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

        public SettingsWindow()
        {
            InitializeComponent();
            _currentSettings = SettingsManager.Load();

            SizeSlider.Value = _currentSettings.CatSize > 0 ? _currentSettings.CatSize : 120.0;
            AnimationSlider.Value = _currentSettings.ActiveAnimationDuration > 0 ? _currentSettings.ActiveAnimationDuration : 5;

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

        private void TaskTitleInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TaskTitleInput.Text == "Новая задача...")
            {
                TaskTitleInput.Text = "";
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

            var newTask = new TodoTask
            {
                Title = title,
                ScheduledTime = scheduledTime,
                IsPermanent = TaskPermanentCheck.IsChecked ?? false,
                IsCompleted = false
            };

            _currentSettings.TodoList.Add(newTask);
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

        private void TaskItem_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
            {
                SettingsManager.NotifyLiveUpdate(_currentSettings);
            }
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

            _currentSettings.CatSize = SizeSlider.Value;

            // Уведомляем главное окно о временных изменениях без записи в файл
            SettingsManager.NotifyLiveUpdate(_currentSettings);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _currentSettings.CatSize = SizeSlider.Value;
            _currentSettings.ActiveAnimationDuration = (int)AnimationSlider.Value;
            if (CatSkinCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                _currentSettings.CatSkin = selectedItem.Tag.ToString() ?? "cat.png";
            }
            if (CatActiveSkinCombo.SelectedItem is System.Windows.Controls.ComboBoxItem selectedActiveItem)
            {
                _currentSettings.CatActiveSkin = selectedActiveItem.Tag?.ToString() ?? "";
            }

            SettingsManager.Save(_currentSettings);

            this.DialogResult = true;
            this.Close();
        }
    }
}