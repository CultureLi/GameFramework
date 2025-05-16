using System;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Framework
{
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        /// <summary>
        /// 界面组。
        /// </summary>
        private sealed partial class UIGroup : IUIGroup
        {
            Transform _root;
            UIGroupType _groupType;

            private int _sortLayer;
            public UIGroupType GroupType => _groupType;

            private readonly LinkedList<UIViewWrapper> _viewWrappers = new LinkedList<UIViewWrapper>();
            private readonly Dictionary<string, UIViewWrapper> _pool = new Dictionary<string, UIViewWrapper>();

            public UIGroup(UIGroupType type, Transform root)
            {
                _root = root;
                _groupType = type;
                _sortLayer = ((int)type) * 10000;
            }

            private int CalcSortLayer()
            {
                int cnt = _viewWrappers.Count;
                return CalcSortLayer(cnt);
            }

            private int CalcSortLayer(int idx)
            {
                return _sortLayer + idx * 100;
            }

            public ViewBase CurrentView
            {
                get
                {
                    return _viewWrappers.First?.Value.View ?? null;
                }
            }

            public bool HasUI(string name)
            {
                foreach (UIViewWrapper uiFormInfo in _viewWrappers)
                {
                    if (uiFormInfo.Name == name)
                    {
                        return true;
                    }
                }

                return false;
            }

            public ViewBase GetUI(string name)
            {
                foreach (UIViewWrapper wrapper in _viewWrappers)
                {
                    if (wrapper.Name == name)
                    {
                        return wrapper.View;
                    }
                }

                return null;
            }

            private UIViewWrapper GetUIWrapper(string name)
            {
                foreach (UIViewWrapper wrapper in _viewWrappers)
                {
                    if (wrapper.Name == name)
                    {
                        return wrapper;
                    }
                }

                return null;
            }

            public void OpenUI(string name, ViewData data, GameObject asset)
            {
                var wrapper = GetUIWrapper(name);
                if (wrapper == null)
                {
                    InitCreateUI(name, data, asset);
                }
                else
                {
                    RefocusUI(wrapper, data);
                }
            }

            private void InitCreateUI(string name, ViewData data, GameObject asset)
            {
                var wrapper = UIViewWrapper.Create(name, data, asset, _root);
                _viewWrappers.AddFirst(wrapper);
                int layer = CalcSortLayer();
                wrapper.SetLayer(layer);
                wrapper.DoShow();
            }

            private void RefocusUI(UIViewWrapper wrapper, ViewData data)
            {
                wrapper.UpdateViewData(data);
                _viewWrappers.Remove(wrapper);
                _viewWrappers.AddFirst(wrapper);
                ForceUpdateUILayer();
                wrapper.DoShow();
            }

            public void RefocusUI(string name, ViewData data)
            {
                var wrapper = GetUIWrapper(name);
                if (wrapper != null)
                {
                    RefocusUI(wrapper, data);
                }
            }

            private void ForceUpdateUILayer()
            {
                var idx = _viewWrappers.Count - 1;
                foreach(var wrapper in _viewWrappers)
                {
                    var layer = CalcSortLayer(idx);
                    wrapper.SetLayer(layer);
                    idx--;
                }
            }

            public void RefocusUIForm(string name, ViewData data)
            {
                UIViewWrapper uiFormInfo = GetUIFormInfo(name);
                if (uiFormInfo == null)
                {
                    throw new Exception("Can not find UI form info.");
                }

                _viewWrappers.Remove(uiFormInfo);
                _viewWrappers.AddFirst(uiFormInfo);
            }

            public void CloseUI(string name)
            {
                var uiInfo = GetUIFormInfo(name);
                uiInfo.DoClose();
            }

            /// <summary>
            /// 从界面组移除界面。
            /// </summary>
            /// <param name="view">要移除的界面。</param>
            public void RemoveUIForm(string name)
            {
                UIViewWrapper uiFormInfo = GetUIFormInfo(name);
                if (uiFormInfo == null)
                {
                    throw new Exception($"Can not find UI {name}'.");
                }

                if (!_viewWrappers.Remove(uiFormInfo))
                {
                    throw new Exception($"UI group '{_groupType}' not exists specified UI form '{name}'.");
                }

                ReferencePool.Release(uiFormInfo);
            }



            public void Refresh()
            {
               
            }
           
            private UIViewWrapper GetUIFormInfo(string name)
            {
                foreach (UIViewWrapper uiFormInfo in _viewWrappers)
                {
                    if (uiFormInfo.Name == name)
                    {
                        return uiFormInfo;
                    }
                }

                return null;
            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
            }
        }
    }
}
