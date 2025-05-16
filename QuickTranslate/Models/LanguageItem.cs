// Models/LanguageItem.cs
namespace QuickTranslate.Models
{
    public class LanguageItem
    {
        public string DisplayName { get; set; } // 用于在下拉列表中显示
        public string Value { get; set; }       // 对应的参数值，例如 "en", "zh"

        public LanguageItem(string displayName, string value)
        {
            DisplayName = displayName;
            Value = value;
        }

        // 重写 ToString() 以便 ComboBox 默认显示 DisplayName，但最好还是设置 DisplayMemberPath
        public override string ToString()
        {
            return DisplayName;
        }
    }
}