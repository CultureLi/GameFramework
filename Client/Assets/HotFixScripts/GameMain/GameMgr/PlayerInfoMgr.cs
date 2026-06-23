using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameMain
{
    public class PlayerInfoMgr : GameMgrBase<PlayerInfoMgr>, ISecondUpdate
    {

        public override void Initialize()
        {
            Debug.Log("PlayerInfoMgr Initialize");
        }

        override public void Dispose()
        {
            Debug.Log("PlayerInfoMgr Dispose");
        }

        public void SecondUpdate()
        {
            Debug.Log("PlayerInfoMgr OnSecondUpdate");
        }

        public void Test()
        {
            Debug.Log("PlayerInfoMgr Test");
        }
    }
}
