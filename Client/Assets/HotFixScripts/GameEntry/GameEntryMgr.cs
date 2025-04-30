using UnityEngine.Networking;
using UnityEngine;
using System.Collections;
using System;

namespace GameEntry
{
    internal partial class GameEntryMgr
    {
        public static void Entry()
        {
            Debug.Log(" GameEntryMgr 进来了");
            var go = new GameObject("EntranceStages");
            go.AddComponent<EntranceStages>();
        }
        private static GameEntryMgr instance;
        private static readonly object locker = new();

        public string remoteCatalogHash;
        public string localCatalogHash;

        public static GameEntryMgr I
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            instance = new GameEntryMgr();
                        }
                    }
                }
                return instance;
            }
        }

        public bool IsCatalogHashChanged()
        {
            if (string.IsNullOrEmpty(localCatalogHash) || string.IsNullOrEmpty(remoteCatalogHash))
                return false;

            return localCatalogHash.CompareTo(remoteCatalogHash) != 0;
        }


        public IEnumerator DownloadWithRetry(string url, string savePath, int retryCount = 3, int timeoutSeconds = 10, Action<DownloadHandler> completedCb = null)
        {
            for (int i = 0; i < retryCount; i++)
            {
                using (UnityWebRequest uwr = UnityWebRequest.Get(url))
                {
                    // 设置超时时间（单位：秒）
                    uwr.timeout = timeoutSeconds;

                    // 如果你想写入文件
                    if (!string.IsNullOrEmpty(savePath))
                        uwr.downloadHandler = new DownloadHandlerFile(savePath, append: false);

                    Debug.Log($"🔄 第 {i + 1} 次尝试下载: {url}");

                    yield return uwr.SendWebRequest();

                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"✅ 下载成功: {url}");
                        completedCb?.Invoke(uwr.downloadHandler);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ 下载失败: {url}，错误: {uwr.error}");
                        // 如果是最后一次也失败了
                        if (i == retryCount - 1)
                        {
                            Debug.LogError($"❌ 最终下载失败: {url}");
                            completedCb?.Invoke(null);
                        }
                    }
                }

                yield return new WaitForSeconds(1f); // 可以加一个延迟再重试
            }
        }
    }
}
