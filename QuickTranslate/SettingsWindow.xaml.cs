// SettingsWindow.xaml.cs
using QuickTranslate.Models;
using QuickTranslate.Services; // 需要引用 AutoStartManager 所在的命名空间
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics; // For Debug.WriteLine

namespace QuickTranslate
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _currentSettings;
        private List<LanguageItem> _sourceLanguages;
        private List<LanguageItem> _targetLanguages;

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeLanguageLists();
            PopulateComboBoxes();

            _currentSettings = App.Settings ?? SettingsManager.LoadSettings();
        }

        private void InitializeLanguageLists()
        {
            _sourceLanguages = new List<LanguageItem>
            {
                new LanguageItem("英语", "en"), new LanguageItem("汉语(普通话)", "zh"),
                new LanguageItem("日语", "ja"), new LanguageItem("韩语", "ko")
            };
            _targetLanguages = new List<LanguageItem>
            {
                new LanguageItem("英语", "en"), new LanguageItem("汉语(普通话)", "zh")
            };
        }

        private void PopulateComboBoxes()
        {
            DefaultFromLangComboBox.ItemsSource = _sourceLanguages;
            DefaultToLangComboBox.ItemsSource = _targetLanguages;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettingsToUI();
        }

        private void LoadSettingsToUI()
        {
            if (_currentSettings != null)
            {
                ApiUrlTextBox.Text = _currentSettings.ApiUrl;
                ApiKeyTextBox.Text = _currentSettings.ApiKey;

                var selectedSourceLangItem = _sourceLanguages.FirstOrDefault(lang => lang.Value == _currentSettings.DefaultFromLanguage);
                DefaultFromLangComboBox.SelectedItem = selectedSourceLangItem ?? _sourceLanguages.FirstOrDefault(lang => lang.Value == "en");

                var selectedTargetLangItem = _targetLanguages.FirstOrDefault(lang => lang.Value == _currentSettings.DefaultToLanguage);
                DefaultToLangComboBox.SelectedItem = selectedTargetLangItem ?? _targetLanguages.FirstOrDefault(lang => lang.Value == "zh");

                // 加载开机自启状态
                try
                {
                    OpenAtLoginCheckBox.IsChecked = AutoStartManager.IsAutoStartEnabled();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"加载开机自启状态时出错: {ex.Message}");
                    OpenAtLoginCheckBox.IsChecked = false; // 出错时默认为未选中
                    OpenAtLoginCheckBox.IsEnabled = false; // 同时禁用复选框，提示用户可能有问题
                    System.Windows.MessageBox.Show("无法读取开机自启设置，此选项暂时不可用。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                // ... _currentSettings 为 null 的处理逻辑 ...
                ApiUrlTextBox.Text = "http://example.com/api";
                ApiKeyTextBox.Text = string.Empty;
                DefaultFromLangComboBox.SelectedItem = _sourceLanguages.FirstOrDefault(lang => lang.Value == "en");
                DefaultToLangComboBox.SelectedItem = _targetLanguages.FirstOrDefault(lang => lang.Value == "zh");
                OpenAtLoginCheckBox.IsChecked = false; // 默认未选中
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSettings == null)
            {
                _currentSettings = new AppSettings();
            }

            _currentSettings.ApiUrl = ApiUrlTextBox.Text.Trim();
            _currentSettings.ApiKey = ApiKeyTextBox.Text.Trim();

            if (DefaultFromLangComboBox.SelectedValue != null)
                _currentSettings.DefaultFromLanguage = DefaultFromLangComboBox.SelectedValue.ToString();
            else if (_sourceLanguages.Any()) _currentSettings.DefaultFromLanguage = _sourceLanguages.First().Value;
            else _currentSettings.DefaultFromLanguage = "en";

            if (DefaultToLangComboBox.SelectedValue != null)
                _currentSettings.DefaultToLanguage = DefaultToLangComboBox.SelectedValue.ToString();
            else if (_targetLanguages.Any()) _currentSettings.DefaultToLanguage = _targetLanguages.First().Value;
            else _currentSettings.DefaultToLanguage = "zh";

            // 保存其他设置到文件
            SettingsManager.SaveSettings(_currentSettings);
            App.TranslationService?.UpdateSettings(_currentSettings);

            // 设置开机自启
            bool enableAutoStart = OpenAtLoginCheckBox.IsChecked ?? false;
            bool autoStartSetSuccessfully = true; // 标记是否成功
            try
            {
                if (OpenAtLoginCheckBox.IsEnabled) // 仅当复选框可用时才尝试设置
                {
                    autoStartSetSuccessfully = AutoStartManager.SetAutoStart(enableAutoStart);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存开机自启设置时出错: {ex.Message}");
                autoStartSetSuccessfully = false;
            }


            if (autoStartSetSuccessfully)
            {
                System.Windows.MessageBox.Show("设置已保存！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("部分设置已保存，但设置开机自启失败。\n请检查程序日志或尝试手动设置。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
