using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesktopCat.Services
{
    public class AppSettings
    {
        public List<string> AllowedApps { get; set; } = new List<string>();
        public string CatSkin { get; set; } = "cat.png";
        public double CatSize { get; set; } = 120.0;
        public double LastX { get; set; } = -1;
        public double LastY { get; set; } = -1;
        public int ActiveAnimationDuration { get; set; } = 5;
    }

    public static class SettingsManager
    {
        private static readonly string ConfigPath = "settings.json";

        // Событие, которое срабатывает, когда настройки изменены и сохранены (или изменены "на лету")
        public static event Action<AppSettings>? OnSettingsChanged;

        public static AppSettings Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
                catch
                {
                    // В случае ошибки чтения возвращаем дефолтные
                }
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
                OnSettingsChanged?.Invoke(settings);
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }

        // Вспомогательный метод для Live-update
        public static void NotifyLiveUpdate(AppSettings settings)
        {
            OnSettingsChanged?.Invoke(settings);
        }
    }
}