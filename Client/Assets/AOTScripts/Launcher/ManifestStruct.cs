using System;
using System.Collections.Generic;

namespace Launcher
{
    [System.Serializable]
    public class DllInfo
    {
        public string name;
        public string hash;
        public List<string> dependencies;
    }
    [System.Serializable]
    public class HotFixDllManifest
    {
        public List<DllInfo> Item = new List<DllInfo>();
    }

    [System.Serializable]
    public class MetaDataInfo
    {
        public List<string> item = new List<string>();
    }

}
