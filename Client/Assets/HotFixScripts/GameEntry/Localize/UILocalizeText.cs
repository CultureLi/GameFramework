using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace GameEntry
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [Serializable]
    public class UILocalizeText : MonoBehaviour
    {
        [SerializeField]
        private string _key;

        private TextMeshProUGUI _text;
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
                Apply();
            }
        }

        private void OnEnable()
        {
            Apply();
        }

        void Apply()
        {
            if (_text == null)
            {
                _text = gameObject.GetComponent<TextMeshProUGUI>();
            }

            _text.text = FW.LocalizationMgr.Get(_key);
        }
    }
}
