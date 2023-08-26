using GameEngine.Runtime.Base.ReferencePool;

namespace GameEngine.Runtime.Module.Event
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
