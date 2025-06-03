using System;
using System.Collections.Generic;
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
            int _groupId;

            private int _sortLayer;
            public int GroupId => _groupId;

            private readonly LinkedList<UIViewWrapper> _viewWrappers = new LinkedList<UIViewWrapper>();
            private readonly Dictionary<string, UIViewWrapper> _wrapperMap = new Dictionary<string, UIViewWrapper>();

            public UIGroup(int groupId, Transform root)
            {
                _root = root;
                _groupId = groupId;
                _sortLayer = groupId * 10000;
            }

            /// <summary>
            /// 计算层级
            /// </summary>
            /// <returns></returns>
            private int CalcSortLayer()
            {
                int cnt = _viewWrappers.Count;
                return CalcSortLayer(cnt);
            }

            private int CalcSortLayer(int idx)
            {
                return _sortLayer + idx * 100;
            }

            public GameObject GetViewGo(string name)
            {
                return GetUIWrapper(name)?.View.gameObject;
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

            /// <summary>
            /// 打开UI
            /// </summary>
            /// <param name="name"></param>
            /// <param name="data"></param>
            /// <param name="asset"></param>
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

            private void InitCreateUI(string name, ViewData data, GameObject viewGo)
            {
                viewGo.transform.SetParent(_root);
                var wrapper = UIViewWrapper.Spawn(this, name, data, viewGo);
                _viewWrappers.AddFirst(wrapper);
                int layer = CalcSortLayer();
                wrapper.SetLayer(layer);
                wrapper.DoShow();
            }

            /// <summary>
            /// 重新聚焦UI
            /// </summary>
            /// <param name="wrapper"></param>
            /// <param name="data"></param>
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

            /// <summary>
            /// 强制更新UI层级
            /// </summary>
            private void ForceUpdateUILayer()
            {
                var idx = _viewWrappers.Count;
                foreach(var wrapper in _viewWrappers)
                {
                    var layer = CalcSortLayer(idx);
                    wrapper.SetLayer(layer);
                    idx--;
                }
            }

            /// <summary>
            /// 关闭UI
            /// </summary>
            /// <param name="name"></param>
            public void CloseUI(string name)
            {
                var wrapper = GetUIWrapper(name);
                wrapper.DoClose();
                RemoveUIWrapper(wrapper);
            }

            /// <summary>
            /// 从界面组移除界面。
            /// </summary>
            /// <param name="view">要移除的界面。</param>
            private void RemoveUIWrapper(UIViewWrapper wrapper)
            {
                if (!_viewWrappers.Remove(wrapper))
                {
                    throw new Exception($"UI group '{_groupId}' not exists specified UI form '{wrapper.Name}'.");
                }
                UIViewWrapper.UnSpawn(wrapper);
            }

            /// <summary>
            /// 关闭改组所有界面
            /// </summary>
            public void CloseAll()
            {
                foreach (var wrapper in _viewWrappers)
                {
                    wrapper.DoClose();
                }
                _viewWrappers.Clear();
                _wrapperMap.Clear();
            }

        }
    }
}
