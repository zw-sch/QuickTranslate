// SettingsWindow.xaml.cs
using QuickTranslate.Models;
using QuickTranslate.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Text.Json;

namespace QuickTranslate
{
    public partial class SettingsWindow : Window
    {
        private AppSettings _tempSettings; // 用于在窗口中编辑的设置副本
        private List<LanguageItem> _sourceLanguages = new List<LanguageItem>();
        private List<LanguageItem> _targetLanguages = new List<LanguageItem>();

        public SettingsWindow()
        {
            InitializeComponent();
            InitializeLanguageLists();
            PopulateComboBoxes();

            // 创建 AppSettings 的深拷贝副本进行编辑，避免直接修改 App.Settings 直到用户点击保存
            // JsonSerializer 可以用来简单地实现深拷贝
            string currentSettingsJson = JsonSerializer.Serialize(App.Settings ?? new AppSettings());
            _tempSettings = JsonSerializer.Deserialize<AppSettings>(currentSettingsJson) ?? new AppSettings();

            // 确保嵌套配置对象存在 (如果反序列化结果为 null)
            _tempSettings.MTranServerConfig ??= new ProviderConfig("http://10.0.0.147:8989", "zhangwei123");
            _tempSettings.DeepLXConfig ??= new ProviderConfig("https://api.deeplx.org/YOUR_KEY/translate", string.Empty);
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
            LoadSettingsToUI(); // 加载初始选中的提供商的设置
        }

        private void LoadSettingsToUI()
        {
            if (_tempSettings == null) return;

            // 1. 设置提供商下拉框的选中项
            ProviderComboBox.SelectedItem = _tempSettings.SelectedProvider;

            // 2. 根据选中的提供商加载其特定的 ApiUrl 和 ApiKey
            UpdateUiForSelectedProvider(_tempSettings.SelectedProvider);

            // 3. 加载全局语言设置
            var selectedSourceLangItem = _sourceLanguages.FirstOrDefault(lang => lang.Value == _tempSettings.DefaultFromLanguage);
            DefaultFromLangComboBox.SelectedItem = selectedSourceLangItem ?? _sourceLanguages.FirstOrDefault(lang => lang.Value == "en");

            var selectedTargetLangItem = _targetLanguages.FirstOrDefault(lang => lang.Value == _tempSettings.DefaultToLanguage);
            DefaultToLangComboBox.SelectedItem = selectedTargetLangItem ?? _targetLanguages.FirstOrDefault(lang => lang.Value == "zh");

            // 4. 加载开机自启状态
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

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs? _)
        {
            if (!this.IsLoaded || ProviderComboBox.SelectedItem == null) return;

            if (ProviderComboBox.SelectedItem is TranslationProvider selectedProvider)
            {
                // 当用户切换提供商时，保存当前文本框中的值到 _tempSettings 中对应的旧提供商配置
                // (如果 e.RemovedItems 有内容，说明是用户切换，而不是初始加载)
                if (_ != null && _.RemovedItems.Count > 0 && _.RemovedItems[0] is TranslationProvider oldProvider)
                {
                    SaveUiToProviderConfig(oldProvider);
                }

                // 更新UI以显示新选定提供商的配置
                UpdateUiForSelectedProvider(selectedProvider);
            }
        }

        /// <summary>
        /// 根据选定的提供商更新 API URL 和 API Key 文本框的内容及可见性。
        /// </summary>
        private void UpdateUiForSelectedProvider(TranslationProvider provider)
        {
            if (_tempSettings == null) return;

            ProviderConfig? configToLoad = provider switch
            {
                TranslationProvider.MTranServer => _tempSettings.MTranServerConfig,
                TranslationProvider.DeepLX => _tempSettings.DeepLXConfig,
                _ => null
            };

            if (configToLoad != null)
            {
                ApiUrlTextBox.Text = configToLoad.ApiUrl ?? string.Empty;
                ApiKeyTextBox.Text = configToLoad.ApiKey ?? string.Empty;
            }
            else // 理论上不应发生，因为 _tempSettings 中的配置对象已初始化
            {
                ApiUrlTextBox.Text = string.Empty;
                ApiKeyTextBox.Text = string.Empty;
            }

            // 根据提供商调整 UI 元素的标签和可见性
            switch (provider)
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

        /// <summary>
        /// 将当前 UI 文本框中的 ApiUrl 和 ApiKey 保存到指定提供商的 _tempSettings 配置中。
        /// </summary>
        private void SaveUiToProviderConfig(TranslationProvider providerToSave)
        {
            if (_tempSettings == null) return;

            ProviderConfig? configToUpdate = providerToSave switch
            {
                TranslationProvider.MTranServer => _tempSettings.MTranServerConfig,
                TranslationProvider.DeepLX => _tempSettings.DeepLXConfig,
                _ => null
            };

            if (configToUpdate != null)
            {
                configToUpdate.ApiUrl = ApiUrlTextBox.Text?.Trim() ?? string.Empty;
                if (providerToSave == TranslationProvider.MTranServer) // 只为 MTranServer 保存 ApiKey
                {
                    configToUpdate.ApiKey = ApiKeyTextBox.Text?.Trim() ?? string.Empty;
                }
                else
                {
                    configToUpdate.ApiKey = string.Empty; // DeepLX 的 ApiKey 通常为空
                }
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_tempSettings == null) // 理论上不应为 null
            {
                System.Windows.MessageBox.Show(this, "无法保存设置，内部错误。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 1. 保存当前选定提供商的UI输入到_tempSettings
            if (ProviderComboBox.SelectedItem is TranslationProvider currentSelectedProviderOnSave)
            {
                SaveUiToProviderConfig(currentSelectedProviderOnSave);
                _tempSettings.SelectedProvider = currentSelectedProviderOnSave; // 更新当前选中的提供商
            }
            else
            {
                // 如果没有选中项，则默认或保持不变 (理论上 ComboBox 总有选中项)
                _tempSettings.SelectedProvider = TranslationProvider.MTranServer; // 或者 _tempSettings.SelectedProvider
            }

            // 2. 保存全局语言设置
            string? fromLangValue = DefaultFromLangComboBox.SelectedValue as string;
            _tempSettings.DefaultFromLanguage = fromLangValue ?? _sourceLanguages.FirstOrDefault()?.Value ?? "en";

            string? toLangValue = DefaultToLangComboBox.SelectedValue as string;
            _tempSettings.DefaultToLanguage = toLangValue ?? _targetLanguages.FirstOrDefault()?.Value ?? "zh";

            // 3. 将完整的 _tempSettings 保存到文件，并更新 App.Settings
            SettingsManager.SaveSettings(_tempSettings);

            // 更新 App.Settings 以反映更改 (通过深拷贝或逐个属性复制)
            // 最简单的方式是让 App.Settings 也重新加载或直接赋值一个新的克隆对象
            string savedSettingsJson = JsonSerializer.Serialize(_tempSettings);
            if (App.Settings != null) // App.Settings 应该由 App.xaml.cs 初始化
            {
                // 更新 App.Settings 的内容。因为 App.Settings 是静态的，直接赋值新对象可能不是最佳实践
                // 更好的方式是 App.Settings 也从 SettingsManager.LoadSettings() 重新加载，
                // 或者提供一个 App.UpdateGlobalSettings(AppSettings newSettings) 方法。
                // 为了简单起见，我们这里假设 App.Settings 可以被更新。
                // 实际上，由于 TranslationService 持有 App.Settings 的引用，
                // 我们需要确保 TranslationService 使用的是最新的配置。
                // App.TranslationService.UpdateSettings() 已经是这样做了。
                // 我们还需要确保 App.Settings 本身被更新，以便下次打开设置窗口时加载的是最新值。

                // 方案 A: 重新加载 App.Settings (如果 SettingsManager.LoadSettings 总是返回新实例)
                // App.ReloadSettings(); // 假设 App 类有这样的方法

                // 方案 B: 直接修改 App.Settings 的属性 (如果 App.Settings 是可修改的)
                // 这需要 App.Settings 是可写的，或者提供一个方法来更新它。
                // 目前 App.Settings 是 { get; private set; }，只能在 App 类内部设置。
                // 我们可以在 App 类中提供一个静态方法来更新它。
                // App.UpdateGlobalSettings(_tempSettings); // 假设有此方法

                // 当前最直接的方式是，当 TranslationService.UpdateSettings 被调用时，它已经使用了新的配置。
                // 为了下次 SettingsWindow 打开时能加载到最新的，App.Settings 自身也需要更新。
                // 鉴于 App.Settings 是静态的，并且在 SettingsWindow 的构造函数中被读取，
                // 最好的做法是在 App.xaml.cs 中提供一个方法来更新 App.Settings。
                // 或者，让 App.Settings 直接引用 SettingsManager.LoadSettings() 的结果，并在保存后重新加载。
                // 为了本次修改的集中性，我们先确保 TranslationService 更新了。
                // App.Settings 的更新留作一个潜在的改进点，或依赖于 App 重启。
                // **一个简单的做法是，让 App.Settings 也指向这个 _tempSettings (如果 App.Settings 可写)**
                // **或者，更安全的是，App.Settings 应该在下次需要时从 SettingsManager.LoadSettings() 重新加载。**
                // **目前，我们只确保 TranslationService 使用了新配置。**
                if (App.Settings != null)
                {
                    // 手动将_tempSettings的值复制回App.Settings
                    App.Settings.SelectedProvider = _tempSettings.SelectedProvider;
                    App.Settings.MTranServerConfig = _tempSettings.MTranServerConfig; // 需要确保这些是深拷贝或新实例
                    App.Settings.DeepLXConfig = _tempSettings.DeepLXConfig;
                    App.Settings.DefaultFromLanguage = _tempSettings.DefaultFromLanguage;
                    App.Settings.DefaultToLanguage = _tempSettings.DefaultToLanguage;
                }
            }

            App.TranslationService?.UpdateSettings(_tempSettings); // 确保服务使用最新配置

            // 开机自启设置
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
