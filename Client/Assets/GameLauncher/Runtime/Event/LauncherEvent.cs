

namespace GameLauncher.Runtime.Event
{
    /// <summary>
    /// 通用消息事件
    /// </summary>
    internal class CommonMessageEvent:LauncherEventBase
    {
        public string content;
    }

    /// <summary>
    /// YooAsset初始化失败
    /// </summary>
    internal class InitYooAssetFailed:LauncherEventBase
    { 
    }

    /// <summary>
    /// 更新版本失败
    /// </summary>
    internal class UpdateVersionFailed : LauncherEventBase 
    { }

    /// <summary>
    /// 更新Manifest失败
    /// </summary>
    internal class UpdateManifestFailed : LauncherEventBase
    { }

    internal class UpdateFilesInfo : LauncherEventBase
    {
        public int num;
        public long size;
    }

    /// <summary>
    /// 下载失败
    /// </summary>
    internal class DownloadingFileFailed : LauncherEventBase 
    { }
}
