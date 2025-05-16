// TranslateResultWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Input; // 确保这个 using 指令是为了 System.Windows.Input.KeyEventArgs
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace QuickTranslate // 确保命名空间与 XAML 中的 x:Class 指令的 CLR 命名空间部分匹配
{
    // 确保类名与 XAML 中的 x:Class 指令的类名部分匹配，并且是 partial
    public partial class TranslateResultWindow : Window
    {
        private bool _isPinned = true; // 窗口初始默认为固定 (Topmost=true 在 XAML 中设置)

        public TranslateResultWindow()
        {
            InitializeComponent(); // 必须是构造函数的第一行或在任何控件引用之前
            UpdatePinButtonAppearance(); // 根据初始 _isPinned 状态设置按钮
        }

        public void UpdateText(string originalText, string translatedText)
        {
            // 确保 XAML 中的 TextBox 名称是 OriginalTextDisplay 和 TranslatedTextDisplay
            if (OriginalTextDisplay != null) // 添加 null 检查以防万一 (尽管 InitializeComponent 后不应为 null)
            {
                OriginalTextDisplay.Text = originalText;
                OriginalTextDisplay.ScrollToHome();
            }

            if (TranslatedTextDisplay != null) // 添加 null 检查以防万一
            {
                TranslatedTextDisplay.Text = translatedText;
                TranslatedTextDisplay.ScrollToHome();
            }
        }

        // 事件处理程序，对应 XAML 中的 <Button x:Name="CloseButton" ... Click="CloseButton_Click" />
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        // 事件处理程序，对应 XAML 中的 <Window ... Loaded="Window_Loaded" />
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 可选逻辑:
            // if (TranslatedTextDisplay != null)
            // {
            //     TranslatedTextDisplay.Focus();
            //     TranslatedTextDisplay.SelectAll();
            // }
        }

        // 事件处理程序，对应 XAML 中的 <Window ... Deactivated="Window_Deactivated" />
        private void Window_Deactivated(object sender, EventArgs e)
        {
            // 只有当窗口未被固定时，失去焦点才自动隐藏
            if (!_isPinned && this.IsVisible)
            {
                this.Hide();
                Debug.WriteLine("[TranslateResultWindow] Window deactivated and not pinned, hiding.");
            }
            else if (_isPinned)
            {
                Debug.WriteLine("[TranslateResultWindow] Window deactivated but pinned, not hiding.");
            }
        }

        // 事件处理程序，对应 XAML 中的 <Window ... PreviewKeyDown="Window_PreviewKeyDown" />
        // 确保 KeyEventArgs 来自 System.Windows.Input
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Hide();
                e.Handled = true; // 表示事件已处理，避免进一步传递
            }
        }

        // 事件处理程序，对应 XAML 中的 <Button x:Name="PinButton" ... Click="PinButton_Click" />
        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _isPinned = !_isPinned; // 切换固定状态
            this.Topmost = _isPinned; // 应用 Topmost 属性
            UpdatePinButtonAppearance(); // 更新按钮外观
            Debug.WriteLine($"[TranslateResultWindow] Pin state changed. IsPinned: {_isPinned}, Topmost: {this.Topmost}");
        }

        private void UpdatePinButtonAppearance()
        {
            // 确保 XAML 中的 Button 名称是 PinButton
            if (PinButton != null) // 检查 PinButton 是否已初始化
            {
                if (_isPinned)
                {
                    PinButton.Content = "解除固定";
                    PinButton.ToolTip = "点击解除固定 (窗口将不再保持最前)";
                }
                else
                {
                    PinButton.Content = "📌 固定";
                    PinButton.ToolTip = "点击以固定窗口 (保持在其他窗口最前)";
                }
            }
        }

        // P/Invoke for removing system menu (optional, for a cleaner look)
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            WindowInteropHelper helper = new WindowInteropHelper(this);
            if (helper.Handle != IntPtr.Zero) // 确保句柄有效
            {
                int windowStyle = GetWindowLong(helper.Handle, GWL_STYLE);
                SetWindowLong(helper.Handle, GWL_STYLE, windowStyle & ~WS_SYSMENU);
            }
        }
    }
}
