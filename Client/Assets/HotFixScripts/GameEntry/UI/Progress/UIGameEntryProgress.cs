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
            GameEntry.EventMgr.Subscribe<LoadingProgressEvent>(OnLoadingProgressEvent);
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
            GameEntry.EventMgr.Unsubscribe<LoadingProgressEvent>(OnLoadingProgressEvent);
        }

        void OnLoadingProgressEvent(LoadingProgressEvent e)
        {
            slider.value = e.progerss;
            textProgress.text = e.progressText;
        }
    }
}
