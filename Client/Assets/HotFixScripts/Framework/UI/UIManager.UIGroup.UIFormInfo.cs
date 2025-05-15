
using System;

namespace Framework
{
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        private sealed partial class UIGroup : IUIGroup
        {
            /// <summary>
            /// 界面组界面信息。
            /// </summary>
            private sealed class UIFormInfo : IReference
            {
                private string _name;
                private ViewBase _view;
                private ViewData _data;

                public static UIFormInfo Create(string name, ViewData data)
                {
                    var info = ReferencePool.Acquire<UIFormInfo>();
                    info._name = name;
                    info._data = data;
                    return info;
                }

                public ViewBase View
                {
                    get
                    {
                        return _view;
                    }
                }

                public void Clear()
                {
                    _name = null;
                    _data = null;
                    _view = null;
                }
            }
        }
    }
}
