// Services/SettingsManager.cs
using QuickTranslate.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics; // For Debug.WriteLine

namespace QuickTranslate.Services
{
    public static class SettingsManager // 保持为静态类
    {
        private static readonly string AppName = "QuickTranslate";
        private static readonly string SettingsFileName = "settings.json";

        private static string GetSettingsFilePath()
        {
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
                    AppSettings? loadedSettings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loadedSettings != null)
                    {
                        // 确保嵌套的配置对象不是 null (如果旧的 settings.json 可能没有这些)
                        loadedSettings.MTranServerConfig ??= new ProviderConfig("http://10.0.0.147:8989", "zhangwei123");
                        loadedSettings.DeepLXConfig ??= new ProviderConfig("https://api.deeplx.org/YOUR_KEY/translate", string.Empty);
                        return loadedSettings;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsManager] 加载设置时出错: {ex.Message}。将返回新的默认设置。");
                    // 发生错误时，返回一个新的默认 AppSettings 实例
                }
            }
            // 如果文件不存在或加载失败，创建一个包含默认设置的新 AppSettings 实例
            var defaultSettings = new AppSettings();
            SaveSettings(defaultSettings); // 可以选择在首次创建时保存一次默认设置
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
                Debug.WriteLine($"[SettingsManager] 设置已保存到: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SettingsManager] 保存设置时出错: {ex.Message}");
                // 可以考虑通知用户保存失败
            }
        }
    }
}
