// Services/SettingsManager.cs
using QuickTranslate.Models;
using System;
using System.IO;
using System.Text.Json;

namespace QuickTranslate.Services
{
    public class SettingsManager
    {
        private static string AppName = "QuickTranslate"; // 用于创建配置文件夹
        private static string SettingsFileName = "settings.json";

        private static string GetSettingsFilePath()
        {
            // %APPDATA%\QuickTranslate\settings.json
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolderPath = Path.Combine(appDataPath, AppName);
            Directory.CreateDirectory(appFolderPath); // 确保目录存在
            return Path.Combine(appFolderPath, SettingsFileName);
        }

        public static AppSettings LoadSettings()
        {
            string filePath = GetSettingsFilePath();
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch (Exception ex)
                {
                    // 处理加载错误，例如记录日志或返回默认值
                    Console.WriteLine($"Error loading settings: {ex.Message}");
                    return new AppSettings(); // 返回默认设置
                }
            }
            // 如果文件不存在，创建一个包含默认设置的新文件
            var defaultSettings = new AppSettings();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string filePath = GetSettingsFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true }; // 格式化JSON，易于阅读
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // 处理保存错误
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}