using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameMain.UI
{
    public class GameEntryMsgBoxData : ViewData, IReference
    {
        public string title;
        public string content;

        public string btnOkText = "OK";
        public string btnCancelText = "Cancel";
        public Action<bool> callback;

        public void Clear()
        {
        }
    }

    public class UICommonMsgBox : ViewBase
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

            btnOk.AddSafeListener(OnBtnOkClick);
            btnCancel.AddSafeListener(OnBtnCancelClick);
        }

        void OnBtnOkClick()
        {
            Close();
            _uiData?.callback?.Invoke(true);
        }

        void OnBtnCancelClick()
        {
            Close();
            _uiData?.callback?.Invoke(false);
        }

        public override void OnClose()
        {
            
        }
    }
}
