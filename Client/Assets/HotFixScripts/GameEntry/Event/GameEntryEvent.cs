using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameEntry
{
    internal class LoadingProgressEvent : EventBase
    {
        public float progerss;
        public string progressText;

        public static LoadingProgressEvent Create(float progress, string progressText)
        {
            var instance = ReferencePool.Acquire<LoadingProgressEvent>();
            instance.progerss = progress;
            instance.progressText = progressText;
            return instance;
        }
    }

    internal class EntryMsgBoxEvent : EventBase
    {
        public GameEntryMsgBoxData data;
    }
}
