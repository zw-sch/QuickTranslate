using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/TranslationProvider.cs
namespace QuickTranslate.Models
{
    /// <summary>
    /// 定义支持的翻译服务提供商
    /// </summary>
    public enum TranslationProvider // 确保是 public
    {
        MTranServer,
        DeepLX
    }
}


