﻿using Framework;
using GameEntry.Stage;
using System.Collections.Generic;
using UnityEngine;

namespace GameEntry
{
    internal class GameEntryStages : MonoBehaviour
    {
        Fsm _stageFsm;
        void Awake()
        {
            _stageFsm = Fsm.Create("GameEntryFsm", new List<FsmState>()
            {
                new EntranceStartStage(this),
                new DownloadHotfixDllStage(this),
                new DownloadCatalogStage(this),
                new DownloadBundleStage(this),
                new DownloadConfigDataStage(this),
                new EntranceEndStage(),
            });

            _stageFsm.Start<EntranceStartStage>();
        }
    }
}
