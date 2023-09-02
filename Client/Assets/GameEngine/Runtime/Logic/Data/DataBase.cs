using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Runtime.Logic.Data
{
    public abstract class DataBase
    {
        public virtual void OnInit()
        {
            RegisterEvent();
        }

        public abstract void OnRelease();

        public abstract void RegisterEvent();

    }
}

