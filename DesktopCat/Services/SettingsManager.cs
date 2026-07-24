using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesktopCat.Services
{
    public class AppSettings
    {
        public List<string> AllowedApps { get; set; } = new List<string>();
        public string CatSkin { get; set; } = "Default";
        public double CatSize { get; set; } = 120.0;
        public double LastX { get; set; } = -1;
        public double LastY { get; set; } = -1;
    }

    public static class SettingsManager
    {
        private static readonly string ConfigPath = "settings.json";

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
            }
            catch
            {
                // Игнорируем ошибки записи
            }
        }
    }
}