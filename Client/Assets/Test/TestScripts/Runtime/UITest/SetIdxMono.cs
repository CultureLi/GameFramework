using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Test
{
    public class SetIdxMono : MonoBehaviour
    {
        public Animation ani;
        public Button tsLeft;
        public Button tsRight;
        public Button tsMid;

        public List<Sprite> iconList = new List<Sprite>();
        private int idx;

        private void Awake()
        {
            tsLeft.onClick.AddListener(ClickLeft);
            tsRight.onClick.AddListener(ClickRight);

            idx = 1;
            ResetPos();
        }

        void SetIcon()
        {
            Debug.Log($"SetIcon {idx}");
            tsMid.GetComponent<Image>().sprite = iconList[idx];

            var idxLeft = idx - 1;
            if (idxLeft < 0)
                idxLeft = 2;
            tsLeft.GetComponent<Image>().sprite = iconList[idxLeft];


            var idxRight = idx + 1;
            if (idxRight > 2)
                idxRight = 0;
            tsRight.GetComponent<Image>().sprite = iconList[idxRight];
        }

        void ClickLeft()
        {
            Debug.Log("ClickLeft");
            ani.Rewind("Left");
            ani.Play("Left");

            idx--;
            if (idx < 0)
                idx = 2;
        }

        void ClickRight()
        {
            Debug.Log("ClickRight");
            ani.Rewind("Right");
            ani.Play("Right");


            idx++;
            if (idx > 2)
                idx = 0;

        }

        void ResetPos()
        {
            Debug.Log("ResetPos");
            SetIcon();
            tsMid.transform.SetAsLastSibling();
        }

        void OnSetIdx(int isLeft)
        {
            Debug.Log($"SetAsLastSibling {isLeft}");
            if (isLeft == 1)
                tsLeft.transform.SetAsLastSibling();
            else
                tsRight.transform.SetAsLastSibling();
        }
    }
}
