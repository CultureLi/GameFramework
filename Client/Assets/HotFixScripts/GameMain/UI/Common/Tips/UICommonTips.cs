using Framework;
using GameEntry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameMain.Event.Event;

namespace GameMain.UI
{
    public class UICommonTipsData : ViewData, IReference
    {
        public string content;

        public void Clear()
        {
            content = string.Empty;
        }
    }

    public class UICommonTips : ViewBase
    {
        public override bool CanBeReleased
        {
            get;
            set;
        } = false;

        public GameObject textTipTemplate;

        private Stack<UICommonTipsItem> _pool = new Stack<UICommonTipsItem>();
        private Stack<UICommonTipsData> _dataQueue = new Stack<UICommonTipsData>();

        private void Awake()
        {
            var items = UITools.CloneGameObject(textTipTemplate, 5);
            foreach (var item in items)
            {
                var comp = item.GetComponent<UICommonTipsItem>();
                comp.Init(OnTipDisplayComplete);
                item.SetActive(false);
                _pool.Push(comp);
            }
        }

        public override void OnShow(bool isInitShow, ViewData data)
        {
            UIDelegate.I.CommonTipsCtrl = this;
        }

        public void ShowTips(UICommonTipsData data)
        {
            _dataQueue.Push(data);
        }

        void Display()
        {
            if (_dataQueue.Count == 0)
            {
                _displayInterval = 0;
                return;
            }
            if (_pool.Count == 0)
                return;

            var item = _pool.Pop();
            var data = _dataQueue.Pop();

            item.SetContent(data.content);
        }

        void OnTipDisplayComplete(UICommonTipsItem item)
        {
            item.gameObject.SetActive(false);
            _pool.Push(item);
        }

        float _displayInterval;
        void Update()
        {
            _displayInterval -= Time.deltaTime;
            if (_displayInterval <= 0)
            {
                _displayInterval = 0.4f;
                Display();
            }
        }

        public override void OnClose()
        {
            UIDelegate.I.CommonTipsCtrl = null;
        }
    }
}
