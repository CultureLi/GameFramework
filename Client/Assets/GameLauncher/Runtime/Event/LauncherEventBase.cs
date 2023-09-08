using GameEngine.Runtime.Base.RefPool;

namespace GameLauncher.Runtime.Event
{
    internal class LauncherEventBase : IReference
    {
        public LauncherEventBase() { }
        public static T Acquire<T>() where T : LauncherEventBase, IReference, new()
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
