using DG.Tweening;
using Framework;
using System;
using TMPro;
using UnityEngine;

namespace GameMain.UI
{
    public class UICommonTipsItem : MonoBehaviour
    {
        public TextMeshProUGUI textContent;
        public Animation ani;
        private Action<UICommonTipsItem> _onComplete;

        public void Init(Action<UICommonTipsItem> onComplete)
        {
            _onComplete = onComplete;
        }

        public void SetContent(string content)
        {
            gameObject.SetActive(true);
            textContent.text = content;
            ani.Play();
        }

        public void OnAniEnd()
        {
            _onComplete?.InvokeSafely(this);
        }
    }
}
