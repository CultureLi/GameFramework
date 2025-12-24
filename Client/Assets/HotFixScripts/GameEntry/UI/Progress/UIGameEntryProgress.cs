using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameEntry
{
    internal class UIGameEntryProgress : ViewBase
    {
        public Slider slider;
        public TextMeshProUGUI textProgress;

        private void Awake()
        {
            FW.EventMgr.Subscribe<LoadingProgressEvent>(OnLoadingProgressEvent);
            slider.value = 0;
            textProgress.text = string.Empty;
        }

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
        }

        public override void OnClose()
        {
            base.OnClose();
            FW.EventMgr.Unsubscribe<LoadingProgressEvent>(OnLoadingProgressEvent);
        }

        void OnLoadingProgressEvent(LoadingProgressEvent e)
        {
            slider.value = e.progress;
            textProgress.text = e.progressText;
        }
    }
}
