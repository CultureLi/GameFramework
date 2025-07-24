using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameEntry
{
    [Serializable]
    public class AppConfigData
    {
        public Dictionary<string, string> url;
        public List<Dictionary<string, string>> loginServer;
    }

    public static class AppConfig
    {
        static AppConfigData _data;

        private static readonly string kResUpdateUrlKey = "resUpdateUrl";

        public static void Initialize()
        {
            var asset = Resources.Load<TextAsset>("appConfig");
            _data = JsonConvert.DeserializeObject<AppConfigData>(asset.text);

            DumpAppCfg();
        }

        public static string GetUrl(string key)
        {
            if (_data == null)
                return string.Empty;

            return _data.url.TryGetValue(key, out var url) ? url : string.Empty;
        }

        public static string GetResUpdateUrl()
        {
            return GetUrl(kResUpdateUrlKey);
        }

        static void DumpAppCfg()
        {
            foreach ((var key, var value) in _data.url)
            {
                Debug.Log($"{key} {value}");
            }

            foreach (var item in _data.loginServer)
            {
                foreach ((var key, var value) in item)
                {
                    Debug.Log($"{key} {value}");
                }
            }
        }
    }
}
