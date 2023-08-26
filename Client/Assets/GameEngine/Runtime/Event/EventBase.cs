using GameEngine.Runtime.Pool.ReferencePool;

namespace GameEngine.Runtime.Event
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
