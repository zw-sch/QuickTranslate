// Services/AutoStartManager.cs
using Microsoft.Win32; // 用于注册表操作
using System;
using System.Diagnostics;
using System.Reflection; // 用于获取程序路径
                         // using System.Windows.Forms; // Application.ExecutablePath 也可以，但Assembly更通用

namespace QuickTranslate.Services
{
    public static class AutoStartManager
    {
        // 应用程序在注册表中的名称，可以自定义
        private static readonly string AppName = "QuickTranslate";
        private static readonly string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private static string GetApplicationPath()
        {
            // 获取当前运行的 .exe 文件的完整路径
            // return Forms.Application.ExecutablePath; // 如果 <UseWindowsForms>true</UseWindowsForms>
            string? path = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(path))
            {
                // 回退方案，或者抛出异常
                path = Process.GetCurrentProcess().MainModule?.FileName;
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("无法获取应用程序路径。");
            }
            return path;
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(AppName);
                        // 检查值是否存在并且与当前应用程序路径匹配
                        // 有些程序可能会在路径改变后，旧的启动项仍然存在，所以严格匹配路径更好
                        return value != null && value.ToString().Equals(GetApplicationPath(), StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查开机自启状态时出错: {ex.Message}");
                // 发生错误时，可以认为未启用或返回false
            }
            return false;
        }

        public static bool SetAutoStart(bool enable)
        {
            try
            {
                string appPath = GetApplicationPath();
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true)) // true 表示可写
                {
                    if (key == null)
                    {
                        Debug.WriteLine($"无法打开注册表项: {RegistryKeyPath}");
                        return false; // 无法打开注册表项
                    }

                    if (enable)
                    {
                        key.SetValue(AppName, $"\"{appPath}\""); // 为路径加上引号，以防路径中包含空格
                        Debug.WriteLine($"已设置开机自启: {appPath}");
                    }
                    else
                    {
                        if (key.GetValue(AppName) != null) // 仅当值存在时才尝试删除
                        {
                            key.DeleteValue(AppName, false); // false 表示如果值不存在不抛出异常
                            Debug.WriteLine("已取消开机自启。");
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置开机自启时出错: {ex.Message}");
                // 通常可能是权限问题，但 HKCU\Software\... 通常对当前用户可写
                return false;
            }
        }
    }
}
