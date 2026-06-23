using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Framework
{

    public interface IGameLogicMgr
    {
        public T CreateMgr<T>() where T : GameMgrBase<T>, new();
    }
}
