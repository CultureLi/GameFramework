using Framework;
using System;


namespace Assets.Scripts.ObjectPoolTest
{
    public class CustomPool : ObjectPoolBase
    {
        public override Type ObjectType => throw new NotImplementedException();

        public override int Count => throw new NotImplementedException();

        public override int CanReleaseCount => throw new NotImplementedException();

        public override bool AllowMultiSpawn => throw new NotImplementedException();

        public override float AutoReleaseInterval
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public override int Capacity
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public override float ExpireTime
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public override int Priority
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override ObjectInfo[] GetAllObjectInfos()
        {
            throw new NotImplementedException();
        }

        public override void Release()
        {
            throw new NotImplementedException();
        }

        public override void Release(int toReleaseCount)
        {
            throw new NotImplementedException();
        }

        public override void ReleaseAllUnused()
        {
            throw new NotImplementedException();
        }

        public override void Shutdown()
        {
            
        }


        public override void Update(float elapseSeconds, float realElapseSeconds)
        {
            
        }
    }
}
