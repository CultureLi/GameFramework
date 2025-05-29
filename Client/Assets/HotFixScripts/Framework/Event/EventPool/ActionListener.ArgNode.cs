//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;

namespace Framework
{
    public sealed partial class ActionListener<TArg>
    {
        /// <summary>
        /// 事件结点。
        /// </summary>
        private sealed class ArgNode : IReference
        {
            private Type _type;
            private TArg _data;

            public ArgNode()
            {
                _data = null;
            }

            public TArg Data => _data;

            public Type Type => _type;


            public static ArgNode Create<T>(T e) where T : TArg
            {
                ArgNode eventNode = ReferencePool.Acquire<ArgNode>();
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
