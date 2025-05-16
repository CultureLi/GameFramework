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
            private sealed class UIViewInfo : IReference
            {
                public string Name => _name;
                public ViewBase View => _view;

                private string _name;
                private ViewBase _view;
                private ViewData _data;
                private bool _initShow = true;

                public static UIViewInfo Create(string name, ViewData data, GameObject asset, Transform parent)
                {
                    var info = ReferencePool.Acquire<UIViewInfo>();
                    info._name = name;
                    info._data = data;
                    var ui = GameObject.Instantiate(asset, parent);
                    info._view = ui.GetComponent<ViewBase>();
                    return info;
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
