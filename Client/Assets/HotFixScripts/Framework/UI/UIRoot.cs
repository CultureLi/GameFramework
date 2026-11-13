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
        private Coroutine _blurCoroutine;
        private bool _blurRequested;

        protected void Awake()
        {
            //mask.material = blurMaterial;
            if (template == null)
            {
                Debug.LogError("UIRoot.template 未配置，UI 分组无法创建。");
                return;
            }

            template.SetActive(false);
            _uiMgr = FrameworkMgr.UIMgr;
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
            _blurRequested = show;

            if (!show)
            {
                if (mask != null)
                {
                    mask.enabled = false;
                }

                // 正在抓图的过程中被要求关闭：协程会在抓图结束后自己退出，这里不打断抓图，
                // 避免 Canvas 的 RenderMode 停在 ScreenSpaceCamera 上。
                return;
            }

            if (_blurCoroutine != null)
            {
                return;
            }

            _blurCoroutine = StartCoroutine(BlurMask());
        }

        private IEnumerator BlurMask()
        {
            // 这里先要把UI的Canvas改成ScreenSpaceCamera模式，设置Canvans的camera为UICamera
            for (var groupType = EUIGroupType.HUD; groupType <= EUIGroupType.Popup; groupType++)
            {
                var group = _uiMgr.GetGroup(groupType);
                if (group == null)
                {
                    continue;
                }

                var canvas = group.Canvas;
                if (canvas == null)
                {
                    continue;
                }

                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = uiCamera;
            }

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);

            if (uiCamera != null)
            {
                uiCamera.gameObject.SetActive(true);
            }

            // 用常驻的 RenderTexture，而不是 GetTemporary：
            // temp RT 归还后可能被其他系统拿走复用，而这张贴图会长期挂在 RawImage 上显示。
            if (_rt == null || _rt.width != width || _rt.height != height)
            {
                if (_rt != null)
                {
                    _rt.Release();
                    UnityEngine.Object.Destroy(_rt);
                }

                _rt = new RenderTexture(width, height, 0)
                {
                    filterMode = FilterMode.Bilinear,
                    name = "UIRootBlurRT",
                };
                _rt.Create();
            }

            bool blurApplied = GaussianBlurFeature.Instance != null;
            if (blurApplied)
            {
                GaussianBlurFeature.Instance.Blur(_rt, blurMaterial);
            }
            else
            {
                Debug.LogWarning("GaussianBlurFeature 未挂到 URP Renderer 上，模糊遮罩不会生效。");
            }

            yield return new WaitForEndOfFrame();

            if (uiCamera != null)
            {
                uiCamera.gameObject.SetActive(false);
            }

            for (var groupType = EUIGroupType.HUD; groupType <= EUIGroupType.Popup; groupType++)
            {
                var group = _uiMgr.GetGroup(groupType);
                var canvas = group?.Canvas;
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
            }

            if (blurApplied && _blurRequested && mask != null && _rt != null)
            {
                mask.texture = _rt;
                mask.enabled = true;

                var popupGroup = _uiMgr.GetGroup(EUIGroupType.Popup);
                if (maskCanvas != null && popupGroup != null)
                {
                    maskCanvas.sortingOrder = popupGroup.GetTopUISortingOrder() - 1;
                }
            }

            _blurCoroutine = null;
        }

        private void OnDestroy()
        {
            if (_rt != null)
            {
                _rt.Release();
                UnityEngine.Object.Destroy(_rt);
                _rt = null;
            }
        }
    }
}
