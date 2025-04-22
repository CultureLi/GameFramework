
using GameMain.Runtime.Base.RefPool;

namespace GameMain.Runtime.Module.Event
{
    public class EventBase : IReference
    {
        public EventBase() { }
        public static T Acquire<T>() where T : EventBase, IReference, new()
        {
            return ReferencePool.Acquire<T>() as T;
        }

        public void Release()
        {
            ReferencePool.Release(this);
        }

        public void Clear()
        {

        }

    }
}
