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

namespace QuickTranslate
{
    public partial class App : System.Windows.Application
    {
        private IKeyboardMouseEvents? _globalHook;
        public static SettingsManager? SettingsManager { get; private set; }
        public static AppSettings? Settings { get; private set; } // 这个会被 SettingsWindow 修改
        public static TranslationService? TranslationService { get; private set; }
        private TranslateResultWindow? _translateResultWindow;
        private Forms.NotifyIcon? _notifyIcon;
        private SettingsWindow? _settingsWindowInstance; // 持有设置窗口的单个实例

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            SettingsManager = new SettingsManager();
            Settings = SettingsManager.LoadSettings(); // Settings 在此被初始化

            if (Settings == null)
            {
                System.Windows.MessageBox.Show("无法加载设置。应用程序即将退出。", "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Windows.Application.Current.Shutdown(-1);
                return;
            }

            TranslationService = new TranslationService(Settings); // TranslationService 使用初始化的 Settings
            InitializeNotifyIcon();

            _globalHook = Hook.GlobalEvents();
            _globalHook.MouseClick += GlobalHook_MouseClick;

            Debug.WriteLine("应用程序已启动，全局钩子和托盘图标已激活。");
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
                    Debug.WriteLine($"图标文件未找到: {iconPath}.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载托盘图标时出错: {ex.Message}");
            }

            var contextMenu = new Forms.ContextMenuStrip();
            var settingsMenuItem = new Forms.ToolStripMenuItem("设置...");
            settingsMenuItem.Click += SettingsMenuItem_Click; // 事件处理已存在
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            var exitMenuItem = new Forms.ToolStripMenuItem("退出");
            exitMenuItem.Click += ExitMenuItem_Click; // 事件处理已存在
            contextMenu.Items.Add(exitMenuItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick; // 事件处理已存在
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
            // 确保只有一个设置窗口实例打开
            if (_settingsWindowInstance == null || !_settingsWindowInstance.IsLoaded)
            {
                _settingsWindowInstance = new SettingsWindow();
                // 设置Owner，使其在主应用退出时关闭，并且居中于主窗口（如果主窗口可见）
                // 由于我们没有可见的主窗口，可以不设置Owner，或者设置为Application.Current.MainWindow (如果将来有)
                // _settingsWindowInstance.Owner = System.Windows.Application.Current.MainWindow;
                _settingsWindowInstance.Closed += (s, args) => _settingsWindowInstance = null; // 关闭后置空实例，以便下次重新创建
                _settingsWindowInstance.ShowDialog(); // 使用 ShowDialog() 以模态方式打开
            }
            else
            {
                _settingsWindowInstance.Activate(); // 如果已打开，则激活它
            }
            Debug.WriteLine("设置窗口已打开或已激活。");
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

        // ... (GlobalHook_MouseClick, HandleTranslationTrigger, GetSelectedText, RestoreClipboard, ShowTranslationResult 保持不变)
        private void GlobalHook_MouseClick(object? sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Middle)
            {
                Debug.WriteLine("鼠标中键已点击！");
                HandleTranslationTrigger(e.Location);
            }
        }

        private async void HandleTranslationTrigger(System.Drawing.Point mousePosition)
        {
            if (TranslationService == null || Settings == null)
            {
                Debug.WriteLine("翻译服务或设置尚未初始化。");
                System.Windows.MessageBox.Show("服务尚未就绪。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedText = GetSelectedText();
            Debug.WriteLine($"捕获到的文本: '{selectedText}'");

            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                string fromLang = Settings.DefaultFromLanguage ?? "auto";
                string toLang = Settings.DefaultToLanguage ?? "zh";

                Debug.WriteLine($"正在翻译 '{selectedText}' 从 {fromLang} 到 {toLang} 使用 API {Settings.ApiUrl}");
                string translatedText = await TranslationService.TranslateAsync(selectedText, fromLang, toLang);
                Debug.WriteLine($"翻译结果: '{translatedText}'");

                ShowTranslationResult(selectedText, translatedText, mousePosition);
            }
            else
            {
                Debug.WriteLine("没有选中文本或未捕获到文本。");
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
            catch (Exception ex) { Debug.WriteLine($"访问剪贴板错误 (复制前): {ex.Message}"); }

            try { System.Windows.Clipboard.SetText(string.Empty); }
            catch (Exception ex) { Debug.WriteLine($"清空剪贴板错误: {ex.Message}"); }

            try { Forms.SendKeys.SendWait("^c"); }
            catch (Exception ex)
            {
                Debug.WriteLine($"SendKeys 错误: {ex.Message}");
                RestoreClipboard(originalClipboardText, clipboardContainsTextInitially);
                return string.Empty;
            }

            Task.Delay(150).Wait();

            string selectedText = string.Empty;
            try
            {
                if (System.Windows.Clipboard.ContainsText()) { selectedText = System.Windows.Clipboard.GetText(); }
            }
            catch (Exception ex) { Debug.WriteLine($"从剪贴板获取选中文本错误: {ex.Message}"); }

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
            catch (Exception ex) { Debug.WriteLine($"恢复原始剪贴板内容错误: {ex.Message}"); }
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
                Debug.WriteLine("全局钩子已释放。");
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
                Debug.WriteLine("托盘图标已释放。");
            }

            _settingsWindowInstance?.Close(); // 关闭设置窗口（如果存在）
            _translateResultWindow?.Close();
            Debug.WriteLine("应用程序正在退出。");
            base.OnExit(e);
        }
    }
}
