// SettingsWindow.xaml.cs
using QuickTranslate.Models;
using QuickTranslate.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace QuickTranslate
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _currentSettings;
        private List<LanguageItem> _sourceLanguages = new List<LanguageItem>();
        private List<LanguageItem> _targetLanguages = new List<LanguageItem>();

        public SettingsWindow()
        {
            InitializeComponent();

            InitializeLanguageLists();
            PopulateComboBoxes();

            if (App.Settings == null)
            {
                Debug.WriteLine("[SettingsWindow] 严重错误: App.Settings 为 null! 正在尝试从 SettingsManager 加载。");
                _currentSettings = SettingsManager.LoadSettings();
                if (_currentSettings == null)
                {
                    _currentSettings = new AppSettings(); // 最后的后备
                    Debug.WriteLine("[SettingsWindow] 警告: SettingsManager.LoadSettings() 也返回 null，已创建新的 AppSettings 实例。");
                }
            }
            else
            {
                _currentSettings = App.Settings;
            }
        }

        private void InitializeLanguageLists()
        {
            _sourceLanguages.Clear();
            _sourceLanguages.AddRange(new List<LanguageItem>
            {
                new LanguageItem("英语", "en"), new LanguageItem("汉语(普通话)", "zh"),
                new LanguageItem("日语", "ja"), new LanguageItem("韩语", "ko")
            });

            _targetLanguages.Clear();
            _targetLanguages.AddRange(new List<LanguageItem>
            {
                new LanguageItem("英语", "en"), new LanguageItem("汉语(普通话)", "zh")
            });
        }

        private void PopulateComboBoxes()
        {
            DefaultFromLangComboBox.ItemsSource = _sourceLanguages;
            DefaultToLangComboBox.ItemsSource = _targetLanguages;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettingsToUI();
            if (ProviderComboBox != null && (ProviderComboBox.SelectedItem != null || ProviderComboBox.Items.Count > 0))
            {
                if (ProviderComboBox.SelectedItem == null && ProviderComboBox.Items.Count > 0)
                {
                    ProviderComboBox.SelectedIndex = 0;
                }
                ProviderComboBox_SelectionChanged(ProviderComboBox, null); // 传递 null 因为 SelectionChangedEventArgs 未被使用
            }
        }

        private void LoadSettingsToUI()
        {
            if (_currentSettings != null)
            {
                ProviderComboBox.SelectedItem = _currentSettings.SelectedProvider;
                ApiUrlTextBox.Text = _currentSettings.ApiUrl ?? string.Empty; // 确保不为 null
                ApiKeyTextBox.Text = _currentSettings.ApiKey ?? string.Empty; // 确保不为 null

                var selectedSourceLangItem = _sourceLanguages.FirstOrDefault(lang => lang.Value == _currentSettings.DefaultFromLanguage);
                DefaultFromLangComboBox.SelectedItem = selectedSourceLangItem ?? _sourceLanguages.FirstOrDefault(lang => lang.Value == "en");

                var selectedTargetLangItem = _targetLanguages.FirstOrDefault(lang => lang.Value == _currentSettings.DefaultToLanguage);
                DefaultToLangComboBox.SelectedItem = selectedTargetLangItem ?? _targetLanguages.FirstOrDefault(lang => lang.Value == "zh");

                try
                {
                    OpenAtLoginCheckBox.IsChecked = AutoStartManager.IsAutoStartEnabled();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SettingsWindow] 加载开机自启状态时出错: {ex.Message}");
                    OpenAtLoginCheckBox.IsChecked = false;
                    OpenAtLoginCheckBox.IsEnabled = false;
                    System.Windows.MessageBox.Show(this, "无法读取开机自启设置，此选项暂时不可用。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                Debug.WriteLine("[SettingsWindow] 严重警告: _currentSettings 为 null，UI可能未正确初始化。");
                if (ProviderComboBox != null && ProviderComboBox.Items.Count > 0) ProviderComboBox.SelectedIndex = 0;
                if (ApiUrlTextBox != null) ApiUrlTextBox.Text = "http://localhost:8080";
                if (ApiKeyTextBox != null) ApiKeyTextBox.Text = "";
            }
        }

        // 将参数 e 重命名为 _ (弃元) 或 _e 来表示它未被使用
        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs? _) // 使用弃元 _
        {
            if (!this.IsLoaded || ApiUrlLabel == null || ApiKeyLabel == null || ApiKeyTextBox == null || ProviderComboBox.SelectedItem == null)
            {
                return;
            }

            if (ProviderComboBox.SelectedItem is TranslationProvider selectedProvider)
            {
                switch (selectedProvider)
                {
                    case TranslationProvider.MTranServer:
                        ApiUrlLabel.Content = "API 地址 (基础 URL):";
                        ApiUrlTextBox.ToolTip = "例如: http://10.0.0.147:8989 (程序会自动拼接 /translate)";
                        ApiKeyLabel.Visibility = Visibility.Visible;
                        ApiKeyTextBox.Visibility = Visibility.Visible;
                        ApiKeyLabel.Content = "API 密钥 (Token):";
                        break;
                    case TranslationProvider.DeepLX:
                        ApiUrlLabel.Content = "DeepLX API 端点:";
                        ApiUrlTextBox.ToolTip = "完整的 DeepLX 端点 URL (包含路径中的密钥和 /translate)";
                        ApiKeyLabel.Visibility = Visibility.Collapsed;
                        ApiKeyTextBox.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSettings == null) { _currentSettings = new AppSettings(); }

            if (ProviderComboBox.SelectedItem is TranslationProvider selectedProviderValue)
            {
                _currentSettings.SelectedProvider = selectedProviderValue;
            }
            else
            {
                _currentSettings.SelectedProvider = TranslationProvider.MTranServer;
            }

            _currentSettings.ApiUrl = ApiUrlTextBox.Text?.Trim() ?? string.Empty; // 处理可能的 null

            if (_currentSettings.SelectedProvider == TranslationProvider.MTranServer)
            {
                _currentSettings.ApiKey = ApiKeyTextBox.Text?.Trim() ?? string.Empty; // 处理可能的 null
            }
            else
            {
                _currentSettings.ApiKey = string.Empty;
            }

            string? fromLangValue = DefaultFromLangComboBox.SelectedValue as string;
            _currentSettings.DefaultFromLanguage = fromLangValue ?? _sourceLanguages.FirstOrDefault()?.Value ?? "en"; // 增加对 FirstOrDefault()?.Value 的 null 检查

            string? toLangValue = DefaultToLangComboBox.SelectedValue as string;
            _currentSettings.DefaultToLanguage = toLangValue ?? _targetLanguages.FirstOrDefault()?.Value ?? "zh"; // 增加对 FirstOrDefault()?.Value 的 null 检查

            SettingsManager.SaveSettings(_currentSettings);
            App.TranslationService?.UpdateSettings(_currentSettings);

            bool enableAutoStart = OpenAtLoginCheckBox.IsChecked ?? false;
            bool autoStartSetSuccessfully = true;
            try
            {
                if (OpenAtLoginCheckBox.IsEnabled)
                { autoStartSetSuccessfully = AutoStartManager.SetAutoStart(enableAutoStart); }
            }
            catch (Exception ex)
            { Debug.WriteLine($"[SettingsWindow] 保存开机自启设置时出错: {ex.Message}"); autoStartSetSuccessfully = false; }

            if (autoStartSetSuccessfully)
            { System.Windows.MessageBox.Show(this, "设置已保存！", "成功", MessageBoxButton.OK, MessageBoxImage.Information); }
            else
            { System.Windows.MessageBox.Show(this, "部分设置已保存，但设置开机自启失败。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning); }

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
