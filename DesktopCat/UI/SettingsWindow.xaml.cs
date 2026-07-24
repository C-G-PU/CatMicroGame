using System;
using System.Linq;
using System.Windows;
using DesktopCat.Services;

namespace DesktopCat.UI
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _currentSettings;

        public SettingsWindow()
        {
            InitializeComponent();
            _currentSettings = SettingsManager.Load();
            AppsTextBox.Text = string.Join(", ", _currentSettings.AllowedApps);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var apps = AppsTextBox.Text
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            _currentSettings.AllowedApps = apps;
            SettingsManager.Save(_currentSettings);

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}