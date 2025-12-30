using Framework;
using GameMain.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.Event
{
    internal partial class Event
    {
        internal class CommonTipsEvent : EventBase
        {
            public string content;

            public static CommonTipsEvent Create(string content)
            {
                var instance = ReferencePool.Acquire<CommonTipsEvent>();
                instance.content = content;
                return instance;
            }
        }
    }
}
