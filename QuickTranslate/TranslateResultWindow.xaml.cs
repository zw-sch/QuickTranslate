// TranslateResultWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Input; // 明确使用 System.Windows.Input.KeyEventArgs
using System.Windows.Interop; // For WindowInteropHelper
using System.Runtime.InteropServices; // For DllImport

namespace QuickTranslate
{
    /// <summary>
    /// Interaction logic for TranslateResultWindow.xaml
    /// </summary>
    public partial class TranslateResultWindow : Window
    {
        public TranslateResultWindow()
        {
            InitializeComponent();
        }

        public void UpdateText(string originalText, string translatedText)
        {
            OriginalTextTextBlock.Text = originalText;
            TranslatedTextTextBlock.Text = translatedText;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide(); // 或者 this.Close(); 如果希望每次都创建一个新的实例
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 可选: 尝试将焦点设置到关闭按钮，以便Esc键能更容易被捕获
            // CloseButton.Focus();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // 当窗口失去焦点时自动关闭 (隐藏)
            // 为避免因快速操作导致窗口意外关闭，可以添加更复杂的逻辑，但目前保持简单
            if (this.IsVisible)
            {
                this.Hide();
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) // 明确指定 KeyEventArgs
        {
            // 按 Esc 键关闭窗口
            if (e.Key == Key.Escape)
            {
                this.Hide();
                e.Handled = true; // 表示事件已处理
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