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

        /// <summary>
        /// 获取登录服务器地址，默认 127.0.0.1:19000。
        /// </summary>
        public static void GetLoginServer(out string host, out int port)
        {
            host = "127.0.0.1";
            port = 19000;

            if (_data?.loginServer == null || _data.loginServer.Count == 0)
                return;

            var server = _data.loginServer[0];
            if (server.TryGetValue("host", out var hostValue) && !string.IsNullOrEmpty(hostValue))
                host = hostValue;

            if (server.TryGetValue("port", out var portValue) && int.TryParse(portValue, out var portNumber))
                port = portNumber;
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
