using System;

namespace Framework
{
    public sealed partial class ActionListener<TArg>
    {
        /// <summary>
        /// 内部结点，暂存参数真实类型和数据
        /// </summary>
        private sealed class ArgNode : IReference
        {
            private Type _type;
            private TArg _data;

            public TArg Data => _data;

            public Type Type => _type;


            public static ArgNode Spawn<T>(T e) where T : TArg
            {
                ArgNode eventNode = ReferencePool.Acquire<ArgNode>();
                eventNode._data = e;
                eventNode._type = e.GetType();
                return eventNode;
            }

            public static void UnSpawn(ArgNode node)
            {
                ReferencePool.Release(node);
            }

            public void Clear()
            {
                _type = null;
                _data = default;
            }
        }
    }
}
