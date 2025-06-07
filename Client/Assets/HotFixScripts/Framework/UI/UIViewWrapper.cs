using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面包装器
    /// </summary>
    public sealed class UIViewWrapper : IReference
    {
        public string Name => _name;
        public ViewBase View => _view;

        public IUIGroup UIGroup => _uiGroup;

        private IUIGroup _uiGroup;
        private string _name;
        private ViewBase _view;
        private Canvas _canvas;
        private ViewData _data;
        private bool _initShow = true;

        public static UIViewWrapper Create(IUIGroup group, string name, ViewData data, GameObject viewGo)
        {
            var info = ReferencePool.Acquire<UIViewWrapper>();
            info._uiGroup = group;
            info._name = name;
            info._data = data;
            info._view = viewGo.GetComponent<ViewBase>();
            info._view.Wrapper = info;

            info._canvas = viewGo.GetComponent<Canvas>();
            return info;
        }

        public static void Release(UIViewWrapper info)
        {
            ReferencePool.Release(info);
        }

        public void UnSpawnView(PrefabObjectPool pool)
        {
            pool.UnSpawn(View.gameObject);
        }

        public void UpdateViewData(ViewData data)
        {
            _data = data;
        }

        public void SetParent(Transform parent)
        {
            View.transform.parent = parent;
        }

        public void SetLayer(int layer)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = layer;
        }

        public void DoShow()
        {
            _view.OnShow(_initShow, _data);
            _initShow = false;
        }

        public void CloseSelf()
        {
            UIGroup.UIMgr.CloseUI(_name);
        }
        public void DoClose()
        {
            _view.OnClose();
        }

        public void Clear()
        {
            _initShow = true;
            _name = null;
            _data = null;
            _view = null;
        }
    }
}

