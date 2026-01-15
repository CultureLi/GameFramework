using Framework;
using GameEntry.Stage;
using GameMain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.HotFixScripts.GameMain
{
    internal class GameMainEntryStages : MonoBehaviour
    {
        Fsm _stageFsm;
        void Awake()
        {
            _stageFsm = Fsm.Create("GameMainEntryFsm", new List<FsmState>()
            {
                new MainEntryStartStage(),
            });

            _stageFsm.Start<MainEntryStartStage>();
        }
    }
}
