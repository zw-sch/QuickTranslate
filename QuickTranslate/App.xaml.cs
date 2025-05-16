// App.xaml.cs
using QuickTranslate.Models;
using QuickTranslate.Services;
using System;
using System.Diagnostics;
using System.Windows;
using Gma.System.MouseKeyHook;
using System.Threading.Tasks;
using Forms = System.Windows.Forms; // System.Windows.Forms.NotifyIcon 在这里
using System.Drawing; // System.Drawing.Icon 在这里

namespace QuickTranslate
{
    public partial class App : System.Windows.Application
    {
        private IKeyboardMouseEvents? _globalHook;
        // AppSettings 现在包含 SelectedProvider，由 SettingsWindow 管理和保存
        public static AppSettings? Settings { get; private set; }
        public static TranslationService? TranslationService { get; private set; }
        private TranslateResultWindow? _translateResultWindow;
        private Forms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindowInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // SettingsManager 负责加载 AppSettings，其中包含 SelectedProvider
            Settings = SettingsManager.LoadSettings();

            if (Settings == null) // 理论上 SettingsManager.LoadSettings() 会返回一个实例
            {
                // 如果真的为null，创建一个默认实例以避免后续的 NullReferenceException
                Settings = new AppSettings();
                Debug.WriteLine("[App] 警告: SettingsManager.LoadSettings() 返回 null，已创建默认 AppSettings。");
                // 也可以选择在这里提示用户或退出，但创建默认实例更稳健
                // System.Windows.MessageBox.Show("无法加载设置。应用程序即将退出。", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // System.Windows.Application.Current.Shutdown(-1);
                // return;
            }

            // TranslationService 的构造函数会使用 Settings 中的 SelectedProvider
            TranslationService = new TranslationService(Settings);
            InitializeNotifyIcon();

            _globalHook = Hook.GlobalEvents();
            _globalHook.MouseClick += GlobalHook_MouseClick;

            Debug.WriteLine("[App] 应用程序已启动。");
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
                _settingsWindowInstance.Closed += (s, args) => _settingsWindowInstance = null;
                _settingsWindowInstance.ShowDialog(); // 以模态方式打开
            }
            else
            {
                _settingsWindowInstance.Activate();
            }
            Debug.WriteLine("[App] 设置窗口已打开或已激活。");
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
                Debug.WriteLine("[App] 鼠标中键已点击！");
                HandleTranslationTrigger(e.Location);
            }
        }

        private async void HandleTranslationTrigger(System.Drawing.Point mousePosition)
        {
            if (TranslationService == null || Settings == null) // 确保 Settings 也已加载
            {
                Debug.WriteLine("[App] 翻译服务或设置尚未初始化。");
                System.Windows.MessageBox.Show("服务尚未就绪。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedText = GetSelectedText();
            Debug.WriteLine($"[App] 捕获到的文本: '{selectedText}'");

            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                // TranslationService 内部会使用 Settings 中的语言和提供商信息
                string translatedText = await TranslationService.TranslateAsync(selectedText);
                Debug.WriteLine($"[App] 翻译结果: '{translatedText}' (提供商: {Settings.SelectedProvider})");
                ShowTranslationResult(selectedText, translatedText, mousePosition);
            }
            else
            {
                Debug.WriteLine("[App] 没有选中文本或未捕获到文本。");
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
            catch (Exception ex) { Debug.WriteLine($"[App] 访问剪贴板错误 (复制前): {ex.Message}"); }

            try { System.Windows.Clipboard.SetText(string.Empty); }
            catch (Exception ex) { Debug.WriteLine($"[App] 清空剪贴板错误: {ex.Message}"); }

            try { Forms.SendKeys.SendWait("^c"); }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] SendKeys 错误: {ex.Message}");
                RestoreClipboard(originalClipboardText, clipboardContainsTextInitially);
                return string.Empty;
            }

            Task.Delay(150).Wait();

            string selectedText = string.Empty;
            try
            {
                if (System.Windows.Clipboard.ContainsText()) { selectedText = System.Windows.Clipboard.GetText(); }
            }
            catch (Exception ex) { Debug.WriteLine($"[App] 从剪贴板获取选中文本错误: {ex.Message}"); }

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
            catch (Exception ex) { Debug.WriteLine($"[App] 恢复原始剪贴板内容错误: {ex.Message}"); }
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
            if (_globalHook != null)
            {
                _globalHook.MouseClick -= GlobalHook_MouseClick;
                _globalHook.Dispose();
                _globalHook = null;
            }
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _settingsWindowInstance?.Close();
            _translateResultWindow?.Close();
            Debug.WriteLine("[App] 应用程序正在退出。");
            base.OnExit(e);
        }
    }
}
