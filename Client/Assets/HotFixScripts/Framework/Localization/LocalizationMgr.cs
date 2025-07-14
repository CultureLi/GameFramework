using cfg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    internal class LocalizationMgr : ILocalizationMgr, IFramework
    {
        IConfigMgr _cfgMgr;
        private readonly string kLanguageKey = "Language";
        private SystemLanguage _language;
        private string _tbName;
        
        public SystemLanguage Language
        {
            get => _language;
            set
            {
                _language = value;
                _tbName = $"TbI18nCfg_{LanguageUtility.GetLanguageISOCode(value)}";
            }
        }

        public LocalizationMgr()
        {
            _cfgMgr = FrameworkMgr.GetModule<IConfigMgr>();
            Initialize();
        }

        /// <summary>
        /// 获取多语言
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            var table = _cfgMgr.GetTable<TbI18nCfg>(_tbName);
            return table.Get(key)?.Value ?? key;
        }

        public string Format(string key, object arg0)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, arg0);
            }
            catch (Exception e)
            {
                Debug.LogError($"Localization Format error: {key} {arg0} {format}");
                return format;
            }
        }

        public string Format(string key, object arg0, object arg1)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, arg0, arg1);
            }
            catch (Exception e)
            {
                Debug.LogError($"Localization Format error: {key} {arg0} {arg1} {format}");
                return format;
            }
        }

        public string Format(string key, object arg0, object arg1, object arg2)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, arg0, arg1, arg2);
            }
            catch (Exception e)
            {
                Debug.LogError($"Localization Format error: {key} {arg0} {arg1} {arg2} {format}");
                return format;
            }
        }

        public string Format(string key, params object[] args)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch (Exception e)
            {
                Debug.LogError($"Localization Format error: {key} {args} {format}");
                return format;
            }
        }

        /// <summary>
        /// 初始化设置本地语言
        /// </summary>
        public void Initialize()
        {
            var language = SystemLanguage.Unknown;
            if (PlayerPrefs.HasKey(kLanguageKey))
            {
                language = (SystemLanguage)PlayerPrefs.GetInt(kLanguageKey);
            }
            else
            {
                language = GetSystemLanguage();
            }

            if (!IsValid(language))
            {
                language = SystemLanguage.Unknown;
            }

            PlayerPrefs.SetInt(kLanguageKey, (int)language);
            Language = language;
        }

        /// <summary>
        /// 设置本地语言
        /// </summary>
        /// <param name="isoCode"></param>
        /// <returns></returns>
        public bool SetLanguage(string isoCode)
        {
            if (IsValid(isoCode))
            {
                var language = LanguageUtility.GetSystemLanguageByISOCode(isoCode);
                PlayerPrefs.SetInt(kLanguageKey, (int)language);
                return true;
            }
            return false;
        }

        bool IsValid(SystemLanguage language)
        {
            var isoCode = LanguageUtility.GetLanguageISOCode(language);
            return IsValid(isoCode);
        }

        bool IsValid(string isoCode)
        {
            var cfgs = _cfgMgr.GetTable<TbLanguageCfg>().DataList;
            return cfgs.Exists(e => e.Code == isoCode);
        }

        SystemLanguage GetSystemLanguage()
        {
            var language = Application.systemLanguage;

            //华为手机安卓9以上获取系统多语言出错, 手机切换为繁体时，Application.systemLanguage返回的依然是简体
#if UNITY_ANDROID && !UNITY_EDITOR
            if (language == SystemLanguage.ChineseSimplified ||
                language == SystemLanguage.ChineseTraditional ||
                language == SystemLanguage.Chinese)
            {
                using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (var curActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        AndroidJavaObject locale = curActivity.Call<AndroidJavaObject>("getResources")
                                .Call<AndroidJavaObject>("getConfiguration").Get<AndroidJavaObject>("locale");

                        if (AndroidVersion.SDK_INT >= 24)
                        {
                            locale = curActivity.Call<AndroidJavaObject>("getResources")
                                .Call<AndroidJavaObject>("getConfiguration").Call<AndroidJavaObject>("getLocales")
                                .Call<AndroidJavaObject>("get", 0);
                        }
                        var lang = locale.Call<string>("getLanguage");
                        var tag = locale.Call<string>("toLanguageTag").ToLower();
                        var country = locale.Call<string>("getCountry");

                        Debug.Log($"lang:{lang} tag:{tag} country:{country}");

                        if (lang.Equals("zh"))
                        {
                            if (tag.Contains("zh-hans"))
                            {
                                language = SystemLanguage.ChineseSimplified;
                            }
                            else if (country.ToLower().Contains("zh-hant"))
                            {
                                language = SystemLanguage.ChineseTraditional;
                            }
                            else
                            {
                                language = country.Equals("CN")
                                    ? SystemLanguage.ChineseSimplified
                                    : SystemLanguage.ChineseTraditional;
                            }
                        }

                        locale.Dispose();
                    }
                }
            }
#endif
            return language;
        }


        public void Shutdown()
        {
            
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            
        }
    }
}
