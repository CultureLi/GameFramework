using System;
using UnityEngine;

namespace Framework
{
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        private sealed partial class UIGroup : IUIGroup
        {
            /// <summary>
            /// 界面组界面信息。
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

                public static UIViewWrapper Create(string name, ViewData data, GameObject asset, Transform parent)
                {
                    var info = ReferencePool.Acquire<UIViewWrapper>();
                    info._name = name;
                    info._data = data;
                    var ui = GameObject.Instantiate(asset, parent);
                    info._view = ui.GetComponent<ViewBase>();
                    info._canvas = ui.GetComponent<Canvas>();
                    return info;
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
