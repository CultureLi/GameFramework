using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面包装器
    /// </summary>
    public sealed class UIViewWrapper : IReference
    {
        public string assetPath
        {
            get; private set;
        }
        public UIGroupBase UIGroup
        {
            get; private set;
        }

        private ViewBase _view;
        public Canvas Canvas => _canvas;
        private Canvas _canvas;
        private ViewData _data;
        private bool _initShow = true;
        // 在prefab中自定义的层级，在Group层级基础+自定义 = 该ui最终层级
        public int CustomSortingOrder
        {
            get; private set;
        }
        public static UIViewWrapper Create(UIGroupBase group, string assetPath, ViewData data, ViewBase view)
        {
            var info = ReferencePool.Acquire<UIViewWrapper>();
            info.UIGroup = group;
            info.assetPath = assetPath;
            info._data = data;
            info._view = view;
            info._view.Wrapper = info;
            info._canvas = view.GetComponent<Canvas>();
            info.CustomSortingOrder = info._canvas.sortingOrder;
            info._view.name = view.name.Replace("(Clone)", "");

            view.transform.SetParent(info.UIGroup.Root);
            RectTransform uiTransform = info.Canvas.GetComponent<RectTransform>();
            uiTransform.anchoredPosition = Vector2.zero;
            uiTransform.anchorMin = Vector2.zero;
            uiTransform.anchorMax = Vector2.one;
            uiTransform.pivot = Vector2.one * .5f;
            uiTransform.sizeDelta = Vector2.zero;
            uiTransform.localScale = Vector3.one;

            info._view.gameObject.SetActive(false);
            return info;
        }

        public void Release(PrefabObjectPool pool)
        {
            // 重新设置回去，避免复用的时候层级不对
            _canvas.sortingOrder = CustomSortingOrder;
            pool.UnSpawn(_view.gameObject);
            ReferencePool.Release(this);
        }

        public void UpdateViewData(ViewData data)
        {
            _data = data;
        }

        public void SetParent(Transform parent)
        {
            _view.transform.parent = parent;
        }

        public void SetSortingOrder(int sortingOrder)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = sortingOrder;
        }

        public void DoShow(int sortingOrder)
        {
            _view.gameObject.SetActive(true);
            SetSortingOrder(sortingOrder);
            _view.OnShow(_initShow, _data);
            _initShow = false;
        }

        public void ReFocusSelf()
        {
            UIGroup.RefocusUI(this);
        }

        public void CloseSelf()
        {
            UIGroup.UIMgr.CloseUI(assetPath);
        }
        public void DoClose()
        {
            _view.gameObject.SetActive(false);
            _view.OnClose();
        }

        public void DoHide()
        {
            _view.gameObject.SetActive(false);
            _view.OnHide();
        }

        public void Clear()
        {
            _initShow = true;
            assetPath = null;
            _data = null;
            _view = null;
            _canvas = null;
            CustomSortingOrder = 0;
        }

        public void SecondUpdate()
        {
            if (_view)
            {
                _view.SecondUpdate();
            }
        }
    }
}

