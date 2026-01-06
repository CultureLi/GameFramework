using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Framework
{
    public class UIRoot : MonoBehaviour
    {
        public GameObject template;
        public Canvas maskCanvas;
        public RawImage mask;
        public Camera uiCamera;
        public Material blurMaterial;

        private IUIManager _uiMgr;
        private RenderTexture _rt;
        
        protected void Awake()
        {
            //mask.material = blurMaterial;
            template.SetActive(false);
            _uiMgr = FrameworkMgr.GetModule<IUIManager>();
            for (var type = EUIGroupType.HUD; type <= EUIGroupType.Tips; type++)
            {
                var groupRoot = GameObject.Instantiate(template, transform);
                groupRoot.SetActive(true);
                groupRoot.name = Enum.GetName(typeof(EUIGroupType), type);
                _uiMgr.AddGroup(type, groupRoot.transform);
            }

        }

        public void ShowBlurMask(bool show)
        {
            if (show)
            {
                StartCoroutine(BlurMask());
            }
            else
            {
                mask.enabled = false;
            }
        }

        private IEnumerator BlurMask()
        {
            // 这里先要把UI的Canvas改成ScreenSpaceCamera模式，设置Canvans的camera为UICamera
            for (var groupType = EUIGroupType.HUD; groupType <= EUIGroupType.Popup; groupType++)
            {
                var canvas = _uiMgr.GetGroup(groupType).Canvas;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _uiMgr.UIRoot.uiCamera;
            }

            int width = Screen.width;
            int height = Screen.height;

            uiCamera.gameObject.SetActive(true);

            if (_rt != null)
            {
                RenderTexture.ReleaseTemporary(_rt);
            }
            _rt = RenderTexture.GetTemporary(width, height, 0);
            _rt.filterMode = FilterMode.Bilinear;
            GaussianBlurFeature.Instance.Blur(_rt, blurMaterial);

            yield return new WaitForEndOfFrame();

            uiCamera.gameObject.SetActive(false);
            mask.enabled = true;
            mask.texture = _rt;

            for (var groupType = EUIGroupType.HUD; groupType <= EUIGroupType.Popup; groupType++)
            {
                var canvas = _uiMgr.GetGroup(groupType).Canvas;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            _uiMgr.UIRoot.maskCanvas.sortingOrder = _uiMgr.GetGroup(EUIGroupType.Popup).GetTopUISortingOrder() - 1;
        }
    }
}
