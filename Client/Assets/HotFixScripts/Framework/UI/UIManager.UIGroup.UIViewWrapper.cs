using UnityEngine;

namespace Framework
{
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        private sealed partial class UIGroup : IUIGroup
        {
            /// <summary>
            /// 界面包装器
            /// </summary>
            private sealed class UIViewWrapper : IReference
            {
                public string Name => _name;
                public ViewBase View => _view;

                private string _name;
                private ViewBase _view;
                private Canvas _canvas;
                private ViewData _data;
                private bool _initShow = true;

                public static UIViewWrapper Spawn(IUIGroup group, string name, ViewData data, GameObject viewGo)
                {
                    var info = ReferencePool.Acquire<UIViewWrapper>();
                    info._name = name;
                    info._data = data;
                    info._view = viewGo.GetComponent<ViewBase>();
                    info._canvas = viewGo.GetComponent<Canvas>();
                    return info;
                }

                public static void UnSpawn(UIViewWrapper info)
                {
                    ReferencePool.Release(info);
                }

                public void UpdateViewData(ViewData data)
                {
                    _data = data;
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
    }
}
