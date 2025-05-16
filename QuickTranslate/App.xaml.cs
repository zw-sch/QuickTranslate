// App.xaml.cs
using QuickTranslate.Models;
using QuickTranslate.Services;
using System;
using System.Diagnostics;
using System.Windows;
using Gma.System.MouseKeyHook;
using System.Threading.Tasks;
using Forms = System.Windows.Forms;
using System.Drawing;
using System.Text.Json;

namespace QuickTranslate
{
    public partial class App : System.Windows.Application
    {
        private IKeyboardMouseEvents? _globalHook;

        // 为 Settings 属性在声明时提供一个默认的非 null 实例，以解决 CS8618 警告。
        // 这个实例会在 OnStartup 中被实际加载的设置覆盖。
        public static AppSettings Settings { get; private set; } = new AppSettings();

        public static TranslationService? TranslationService { get; private set; }
        private TranslateResultWindow? _translateResultWindow;
        private Forms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindowInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AppSettings? loadedSettings = SettingsManager.LoadSettings();
            // SettingsManager.LoadSettings() 应该总是返回一个有效的 AppSettings 实例（即使是默认的）
            // 所以 loadedSettings 理论上不应为 null。
            // 但为了绝对安全，我们仍然使用之前声明时初始化的 Settings 作为最终后备。
            if (loadedSettings != null)
            {
                Settings = loadedSettings;
            }
            // 如果 loadedSettings 为 null（极不可能），Settings 将保持为 new AppSettings()。

            TranslationService = new TranslationService(Settings); // Settings 此处保证非 null
            InitializeNotifyIcon();

            _globalHook = Hook.GlobalEvents();
            _globalHook.MouseClick += GlobalHook_MouseClick;

            Debug.WriteLine("[App] 应用程序已启动。");
        }

        /// <summary>
        /// 允许外部（如 SettingsWindow）请求更新全局静态 Settings 对象。
        /// </summary>
        public static void UpdateGlobalSettings(AppSettings newSettings)
        {
            // 实现深拷贝以更新 Settings，避免直接引用外部对象
            // 同时确保嵌套的 ProviderConfig 对象也被正确处理
            string newSettingsJson = JsonSerializer.Serialize(newSettings);
            AppSettings? deserializedSettings = JsonSerializer.Deserialize<AppSettings>(newSettingsJson);

            if (deserializedSettings != null)
            {
                // 确保嵌套配置在反序列化后不为null (如果 newSettings 可能有null的嵌套对象)
                deserializedSettings.MTranServerConfig ??= new ProviderConfig();
                deserializedSettings.DeepLXConfig ??= new ProviderConfig();
                Settings = deserializedSettings;
            }
            else
            {
                // 反序列化失败，这是一个严重问题，记录错误，但不改变现有 Settings
                Debug.WriteLine("[App] 严重错误: 更新全局设置时反序列化失败。保持原有设置。");
                // 或者可以选择抛出异常或使用 new AppSettings() 作为回退
                // Settings = new AppSettings(); // 极端情况下的回退
            }

            TranslationService?.UpdateSettings(Settings); // 确保 TranslationService 使用最新的 Settings
            Debug.WriteLine("[App] 全局设置已更新。");
        }


        private void InitializeNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "QuickTranslate - 快速翻译";
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "translate_icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    Debug.WriteLine($"[App] 图标文件未找到: {iconPath}.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] 加载托盘图标时出错: {ex.Message}");
            }

            var contextMenu = new Forms.ContextMenuStrip();
            var settingsMenuItem = new Forms.ToolStripMenuItem("设置...");
            settingsMenuItem.Click += SettingsMenuItem_Click;
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            var exitMenuItem = new Forms.ToolStripMenuItem("退出");
            exitMenuItem.Click += ExitMenuItem_Click;
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowSettingsWindow();
        }

        private void SettingsMenuItem_Click(object? sender, EventArgs e)
        {
            ShowSettingsWindow();
        }

        private void ShowSettingsWindow()
        {
            if (_settingsWindowInstance == null || !_settingsWindowInstance.IsLoaded)
            {
                _settingsWindowInstance = new SettingsWindow();
                _settingsWindowInstance.Closed += (s, args) =>
                {
                    _settingsWindowInstance = null;
                };
                _settingsWindowInstance.ShowDialog();
            }
            else
            {
                _settingsWindowInstance.Activate();
            }
        }

        private void ExitMenuItem_Click(object? sender, EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            System.Windows.Application.Current.Shutdown();
        }

        private void GlobalHook_MouseClick(object? sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Middle)
            {
                HandleTranslationTrigger(e.Location);
            }
        }

        private async void HandleTranslationTrigger(System.Drawing.Point mousePosition)
        {
            if (TranslationService == null)
            {
                System.Windows.MessageBox.Show("服务尚未就绪。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedText = GetSelectedText();
            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                string translatedText = await TranslationService.TranslateAsync(selectedText);
                Debug.WriteLine($"[App] 翻译结果: '{translatedText}' (当前提供商: {Settings.SelectedProvider})");
                ShowTranslationResult(selectedText, translatedText, mousePosition);
            }
        }

        private string GetSelectedText()
        {
            string? originalClipboardText = null;
            bool clipboardContainsTextInitially = false;
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    originalClipboardText = System.Windows.Clipboard.GetText();
                    clipboardContainsTextInitially = true;
                }
            }
            catch { /*忽略*/ }

            try { System.Windows.Clipboard.SetText(string.Empty); }
            catch { /*忽略*/ }

            try { Forms.SendKeys.SendWait("^c"); }
            catch
            {
                RestoreClipboard(originalClipboardText, clipboardContainsTextInitially);
                return string.Empty;
            }

            Task.Delay(150).Wait();

            string selectedText = string.Empty;
            try
            {
                if (System.Windows.Clipboard.ContainsText()) { selectedText = System.Windows.Clipboard.GetText(); }
            }
            catch { /*忽略*/ }

            RestoreClipboard(originalClipboardText, clipboardContainsTextInitially);
            return selectedText.Trim();
        }

        private void RestoreClipboard(string? originalText, bool hadTextInitially)
        {
            try
            {
                if (hadTextInitially && originalText != null) { System.Windows.Clipboard.SetText(originalText); }
                else if (!hadTextInitially) { System.Windows.Clipboard.Clear(); }
            }
            catch { /*忽略*/ }
        }

        private void ShowTranslationResult(string originalText, string translatedText, System.Drawing.Point mousePosition)
        {
            Current.Dispatcher.Invoke(() =>
            {
                if (_translateResultWindow == null || !_translateResultWindow.IsLoaded)
                {
                    _translateResultWindow = new TranslateResultWindow();
                    _translateResultWindow.Closed += (s, args) => _translateResultWindow = null;
                }

                _translateResultWindow.UpdateText(originalText, translatedText);
                Rect workArea = SystemParameters.WorkArea;
                _translateResultWindow.Left = Math.Max(workArea.Left + 5, Math.Min(mousePosition.X + 15, workArea.Right - _translateResultWindow.Width - 5));
                _translateResultWindow.Top = Math.Max(workArea.Top + 5, Math.Min(mousePosition.Y + 15, workArea.Bottom - _translateResultWindow.Height - 5));

                if (!_translateResultWindow.IsVisible) { _translateResultWindow.Show(); }
                _translateResultWindow.Activate();
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _globalHook?.Dispose();
            _notifyIcon?.Dispose();
            _settingsWindowInstance?.Close();
            _translateResultWindow?.Close();
            base.OnExit(e);
        }
    }
}
