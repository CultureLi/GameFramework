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
    internal class GameEntryMsgBoxData : ViewData
    {
        public string title;
        public string content;

        public string btnOkText = "OK";
        public string btnCancelText = "Cancel";
        //点击回调，btnOk:true, btnCancel:false;
        public Action<bool> callback;
    }
    /// <summary>
    /// 专门给GameEntry使用的提示框，这里设置为internal
    /// </summary>
    internal class UIGameEntryMsgBox : ViewBase
    {
        public TextMeshProUGUI textTitle;
        public TextMeshProUGUI textContent;

        public Button btnCancel;
        public TextMeshProUGUI textBtnCancel;
        public Button btnOk;
        public TextMeshProUGUI textBtnOk;

        private GameEntryMsgBoxData _uiData;

        public override void OnShow(bool isInitShow, ViewData data)
        {
            _uiData = data as GameEntryMsgBoxData;

            textTitle.text = _uiData.title;
            textContent.text = _uiData.content;

            if (string.IsNullOrEmpty(_uiData.btnOkText))
            {
                btnOk.gameObject.SetActive(false);
            }
            else
            {
                btnOk.gameObject.SetActive(true);
                textBtnOk.text = _uiData.btnOkText;
            }

            if (string.IsNullOrEmpty(_uiData.btnCancelText))
            {
                btnCancel.gameObject.SetActive(false);
            }
            else
            {
                btnCancel.gameObject.SetActive(true);
                textBtnCancel.text = _uiData.btnCancelText;
            }

            btnOk.onClick.AddListener(OnBtnOkClick);
            btnCancel.onClick.AddListener(OnBtnCancelClick);
        }

        void OnBtnOkClick()
        {
            _uiData?.callback?.Invoke(true);
            Close();
        }

        void OnBtnCancelClick()
        {
            _uiData?.callback?.Invoke(false);
            Close();
        }

        public override void OnClose()
        {
            
        }
    }
}
