using GameLauncher.Runtime.Event;
using GameEngine.Runtime.Base;
using GameLauncher.Runtime.Event;
using YooAsset;

namespace GameLauncher.Runtime
{
    internal class LauncherMgr:Singleton<LauncherMgr>
    {
        public ResourceDownloaderOperation Downloader { set; get; }


        public void Init() 
        { 

        }

        private void RegisterEvent()
        {
            //LauncherEventMgr.Instance.AddListener<CommonMessageEvent>()
        }



        public void OnDownloadErrorCallback(string fileName, string error)
        {

        }

        public void OnDownloadProgressCallback(int totalDownloadCount, int currentDownloadCount, long totalDownloadSizeBytes, long currentDownloadSizeBytes)
        {

        }
    }
}
