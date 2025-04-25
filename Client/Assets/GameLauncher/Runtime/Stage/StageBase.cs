using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher.Runtime.Stage
{
    abstract class StageBase
    {
        private StageMgr owner;

        public StageMgr Owner
        {
            get
            {
                return owner;
            }
        }

        protected internal void Init(StageMgr mgr)
        {
            owner = mgr;
        }

        protected internal void ChangeStage(Type stage)
        {
            owner.ChangeStage(stage);
        }

        protected internal void ChangeStage<T>() where T : StageBase
        {
            owner.ChangeStage(typeof(T));
        }

        protected internal virtual void OnEnter()
        {
        }

        protected internal virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        protected internal virtual void OnExit()
        {
        }
    }
}
