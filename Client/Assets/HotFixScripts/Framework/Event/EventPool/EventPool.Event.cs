//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace Framework
{
    public sealed partial class EventPool
    {
        /// <summary>
        /// 事件结点。
        /// </summary>
        private sealed class EventNode : IReference
        {
            private Type _type;
            private EventBase _data;

            public EventNode()
            {
                _data = null;
            }

            public EventBase Data => _data;

            public Type Type => _type;


            public static EventNode Create<T>(T e) where T : EventBase
            {
                EventNode eventNode = ReferencePool.Acquire<EventNode>();
                eventNode._data = e;
                eventNode._type = typeof(T);
                return eventNode;
            }

            public void Clear()
            {
                _type = null;
                _data = null;
            }
        }
    }
}
