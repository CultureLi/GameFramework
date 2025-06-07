using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面组。
    /// </summary>
    internal sealed partial class UIGroup : IUIGroup
    {
        public IUIManager UIMgr
        {
            get;set;
        }

        private Transform _root;
        private int _groupId;
        private int _groupLayer;
        public int GroupId => _groupId;

        private readonly LinkedList<UIViewWrapper> _viewWrappers = new LinkedList<UIViewWrapper>();


        public UIGroup(int groupId, Transform root)
        {
            _root = root;
            _groupId = groupId;
            _groupLayer = groupId * 10000;
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
            return _groupLayer + idx * 100;
        }

        public void OpenUI(UIViewWrapper wrapper)
        {
            wrapper.SetParent(_root);
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
        public void RefocusUI(UIViewWrapper wrapper)
        {
            _viewWrappers.Remove(wrapper);
            _viewWrappers.AddFirst(wrapper);
            ForceUpdateUILayer();
            wrapper.DoShow();
        }

        /// <summary>
        /// 强制更新UI层级
        /// </summary>
        private void ForceUpdateUILayer()
        {
            var idx = _viewWrappers.Count;
            foreach (var wrapper in _viewWrappers)
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
        public void CloseUI(UIViewWrapper wrapper)
        {
            if (wrapper == null)
                return;

            _viewWrappers.Remove(wrapper);
            wrapper.DoClose();
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
        }

    }
}

