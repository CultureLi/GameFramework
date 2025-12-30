using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面包装器
    /// </summary>
    public sealed class UIViewWrapper : IReference
    {
        public string Name => _name;
        public IUIGroup UIGroup => _uiGroup;

        private IUIGroup _uiGroup;
        private string _name;
        private ViewBase _view;
        private Canvas _canvas;
        private ViewData _data;
        private bool _initShow = true;

        public static UIViewWrapper Create(IUIGroup group, string name, ViewData data, ViewBase view)
        {
            var info = ReferencePool.Acquire<UIViewWrapper>();
            info._uiGroup = group;
            info._name = name;
            info._data = data;
            info._view = view;
            info._view.Wrapper = info;

            info._canvas = view.GetComponent<Canvas>();
            return info;
        }

        public void Release(PrefabObjectPool pool)
        {
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

        public void SetSortingOrder(int layer)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = layer;
        }

        public void DoShow()
        {
            _view.gameObject.SetActive(true);
            _view.OnShow(_initShow, _data);
            _initShow = false;
        }

        public void ReFocusSelf()
        {
            _uiGroup.RefocusUI(this);
        }

        public void CloseSelf()
        {
            UIGroup.UIMgr.CloseUI(_name);
        }
        public void DoClose()
        {
            DoHide();
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
            _name = null;
            _data = null;
            _view = null;
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

