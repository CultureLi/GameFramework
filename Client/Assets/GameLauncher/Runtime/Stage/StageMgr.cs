using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.GameLauncher.Runtime.Stage
{
    internal class StageMgr
    {
        private StageBase currentStage;
        private Dictionary<Type, StageBase> stages = new Dictionary<Type, StageBase>();

        public void AddStage(StageBase stage)
        {
            stage.Init(this);
            var type = stage.GetType();
            if (!stages.ContainsKey(type))
            {
                stages.Add(type, stage);
            }
        }

        public void ChangeStage(Type stage)
        {
            if (stages.ContainsKey(stage))
            {
                currentStage?.OnExit();
                currentStage = stages[stage];
                Debug.Log($"ChangeStage: {stage}");
                currentStage.OnEnter();
            }
        }

        public void ChangeStage<T>() where T : StageBase
        {
            ChangeStage(typeof(T));
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            currentStage?.OnUpdate(elapseSeconds, realElapseSeconds);
        }
    }
}
