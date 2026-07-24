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

            _isLoaded = true;
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

            SettingsManager.Save(_currentSettings);

            this.DialogResult = true;
            this.Close();
        }
    }
}