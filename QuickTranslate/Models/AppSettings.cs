using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/AppSettings.cs
namespace QuickTranslate.Models
{
    public class AppSettings
    {
        public string ApiUrl { get; set; } = "http://10.0.0.147:8989/translate";
        public string ApiKey { get; set; } = "zhangwei123";
        public string DefaultFromLanguage { get; set; } = "en";
        public string DefaultToLanguage { get; set; } = "zh";
    }
}
