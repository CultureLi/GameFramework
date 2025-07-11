using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public static class LanguageUtility
    {
        readonly static Dictionary<SystemLanguage, string> _sysLanguageToISOCodeMap = new Dictionary<SystemLanguage, string>
        {
            // https://zh.wikipedia.org/zh-my/ISO_639-2
            { SystemLanguage.Afrikaans,           "af" },
            { SystemLanguage.Arabic,              "ar" },
            { SystemLanguage.Basque,              "eu" },
            { SystemLanguage.Belarusian,          "by" },
            { SystemLanguage.Bulgarian,           "bg" },
            { SystemLanguage.Catalan,             "ca" },
            { SystemLanguage.Chinese,             "cn" },
            { SystemLanguage.ChineseSimplified,   "cn" },
            { SystemLanguage.ChineseTraditional,  "zh" },
            { SystemLanguage.Czech,               "cs" },
            { SystemLanguage.Danish,              "da" },
            { SystemLanguage.Dutch,               "nl" },
            { SystemLanguage.English,             "en" },
            { SystemLanguage.Estonian,            "et" },
            { SystemLanguage.Faroese,             "fo" },
            { SystemLanguage.Finnish,             "fi" },
            { SystemLanguage.French,              "fr" },
            { SystemLanguage.German,              "de" },
            { SystemLanguage.Greek,               "el" },
            { SystemLanguage.Hebrew,              "iw" },
            { SystemLanguage.Hungarian,           "hu" },
            { SystemLanguage.Icelandic,           "is" },
            { SystemLanguage.Indonesian,          "id" },
            { SystemLanguage.Italian,             "it" },
            { SystemLanguage.Japanese,            "ja" },
            { SystemLanguage.Korean,              "ko" },
            { SystemLanguage.Latvian,             "lv" },
            { SystemLanguage.Lithuanian,          "lt" },
            { SystemLanguage.Norwegian,           "no" },
            { SystemLanguage.Polish,              "pl" },
            { SystemLanguage.Portuguese,          "pt" },
            { SystemLanguage.Romanian,            "ro" },
            { SystemLanguage.Russian,             "ru" },
            { SystemLanguage.SerboCroatian,       "sh" },
            { SystemLanguage.Slovak,              "sk" },
            { SystemLanguage.Slovenian,           "sl" },
            { SystemLanguage.Spanish,             "es" },
            { SystemLanguage.Swedish,             "sv" },
            { SystemLanguage.Thai,                "th" },
            { SystemLanguage.Turkish,             "tr" },
            { SystemLanguage.Ukrainian,           "uk" },
            { SystemLanguage.Unknown,             "en" },
            { SystemLanguage.Vietnamese,          "vi" },
            { SystemLanguage.Hindi,               "hi" },
        };

        public static string GetLanguageISOCode(SystemLanguage language)
        {
            return _sysLanguageToISOCodeMap.TryGetValue(language, out var code) ? code : _sysLanguageToISOCodeMap[SystemLanguage.Unknown];
        }

        public static SystemLanguage GetSystemLanguageByISOCode(string isoCode)
        {
            foreach ((var language, var code) in _sysLanguageToISOCodeMap)
            {
                if (code.Equals(isoCode))
                    return language;
            }
            return SystemLanguage.Unknown;
        }
    }
}
