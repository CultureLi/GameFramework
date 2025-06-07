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
    internal class UILoading : ViewBase
    {
        public Slider slider;
        public TextMeshProUGUI textProgress;

        private void Awake()
        {
            FW.EventMgr.Subscribe<LoadingProgressEvent>(OnLoadingProgressEvent);
        }

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        void OnLoadingProgressEvent(LoadingProgressEvent e)
        {
            Debug.Log($"OnLoadingProgressEvent {e.progerss}  {e.progressText}");
            slider.value = e.progerss;
            textProgress.text = e.progressText;
        }
    }
}
