using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    internal class UIGroupBase : IUIGroup
    {
        public IUIManager UIMgr => _uiMgr;
        private IUIManager _uiMgr; 

        private Transform _root;
        private EUIGroupType _groupType;
        private int _groupLayer;
        public EUIGroupType GroupType => _groupType;

        private readonly List<UIViewWrapper> _viewWrappers = new List<UIViewWrapper>();
        private Dictionary<string, UIViewWrapper> _viewWrapperMap = new Dictionary<string, UIViewWrapper>();

        // 正在接在的ui
        private readonly HashSet<string> _loadingUIs = new HashSet<string>();
        // 准备销毁的ui
        private readonly HashSet<string> _toReleaseOnLoading = new HashSet<string>();

        public UIGroupBase(IUIManager uiMgr, EUIGroupType groupType, Transform root)
        {
            _uiMgr = uiMgr;
            _root = root;
            _groupType = groupType;
            _groupLayer = (int)_groupType * 10000;
        }

        /// <summary>
        /// 计算层级, 如果在预制体中提前设置了SortingOrder，就用预制体中的值，否则根据打开顺序计算
        /// </summary>
        /// <returns></returns>
        private int CalculateOrder(int idx)
        {
            var wrapper = _viewWrappers[idx];
            if (wrapper.CustomSortingOrder != 0)
            {
                return _groupLayer + wrapper.CustomSortingOrder;
            }
            return _groupLayer + idx * 10;
        }

        protected UIViewWrapper GetViewWrapper(string name)
        {
            return _viewWrapperMap.GetValueOrDefault(name);
        }

        /// <summary>
        /// 打开界面，一般不用重写，只需重写DoOpenUI
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        public virtual void OpenUI(string name, ViewData userData)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("UI name is invalid.");
            }

            //已存在，激活
            var wrapper = GetViewWrapper(name);
            if (wrapper != null)
            {
                RefocusUI(wrapper);
                return;
            }

            // 正在加载
            if (_loadingUIs.Contains(name))
            {
                Debug.Log($"UI {name} is loading....");
                return;
            }

            // 添加到加载列表中
            _loadingUIs.Add(name);

            var assetPath = $"{UIMgr.UIAssetRootPath}/{name}.prefab";

            // 异步加载
            if (userData != null && userData.AsyncLoad)
            {
                UIMgr.UIPrefabPool.SpawnAsync(assetPath, (assetGo) =>
                {
                    OnLoadAssetCompleted(name, userData, assetGo);
                });
            }
            else //同步加载
            {
                var assetGo = UIMgr.UIPrefabPool.Spawn(assetPath);
                OnLoadAssetCompleted(name, userData, assetGo);
            }
        }

        void OnLoadAssetCompleted(string name, ViewData userData, GameObject viewGo)
        {
            // 从正在加载列表中移除
            _loadingUIs.Remove(name);

            // 在加载过程中关闭了界面，加载完后不会打开
            if (_toReleaseOnLoading.Contains(name))
            {
                _toReleaseOnLoading.Remove(name);
                return;
            }

            if (viewGo != null)
            {
                var view = viewGo.GetComponent<ViewBase>();
                var wrapper = UIViewWrapper.Create(this, name, userData, view);
                OnBeforeOpenUI(wrapper);
                DoOpenUI(wrapper);
                OnAfterOpenUI(wrapper);
            }
            else
            {
                Debug.Log($"Load UI Failure: {name}");
            }
        }

        public virtual void OnBeforeOpenUI(UIViewWrapper wrapper)
        {

        }

        public virtual void DoOpenUI(UIViewWrapper wrapper)
        {
            wrapper.SetParent(_root);
            _viewWrappers.Add(wrapper);
            _viewWrapperMap[wrapper.Name] = wrapper;
            wrapper.SetSortingOrder(CalculateOrder(_viewWrappers.Count - 1));
            wrapper.DoShow();
        }

        public virtual void OnAfterOpenUI(UIViewWrapper wrapper)
        {
            
        }

        public bool HasUI(string name)
        {
            return _viewWrapperMap.ContainsKey(name);
        }

        /// <summary>
        /// 重新聚焦UI, 会重新计算组内每个UI的 SortingOrder
        /// </summary>
        /// <param name="wrapper"></param>
        /// <param name="data"></param>
        public virtual void RefocusUI(UIViewWrapper wrapper)
        {
            _viewWrappers.Remove(wrapper);
            OnBeforeOpenUI(wrapper);
            DoOpenUI(wrapper);
            RefreshSortingOrder();
            OnAfterOpenUI(wrapper);
        }

        /// <summary>
        /// 重新计算group中所有ui层级
        /// 例如：界面A层级为1，界面B层级为2，重新激活界面A后
        /// A层级需要变为2，B层级变成1
        /// </summary>
        private void RefreshSortingOrder()
        {
            var cnt = _viewWrappers.Count;
            for (var idx = 0; idx < cnt; idx ++)
            {
                _viewWrappers[idx].SetSortingOrder(CalculateOrder(idx));
            }
        }

        /// <summary>
        /// 激活组内最上层UI
        /// </summary>
        /// <returns></returns>
        public bool RefocusTopUI()
        {
            if (_viewWrappers.Count == 0)
                return false;

            RefocusUI(_viewWrappers.Last());
            return true;
        }

        /// <summary>
        /// 关闭UI
        /// </summary>
        /// <param name="name"></param>
        public virtual bool CloseUI(string name)
        {
            var wrapper = GetViewWrapper(name);
            if (wrapper == null)
                return false;

            _viewWrappers.Remove(wrapper);
            _viewWrapperMap.Remove(wrapper.Name);
            OnBeforeCloseUI(wrapper);
            DoCloseUI(wrapper);
            OnAfterCloseUI(wrapper);
            wrapper.Release(UIMgr.UIPrefabPool);
            return true;
        }

        public virtual void OnBeforeCloseUI(UIViewWrapper wrapper)
        {

        }

        public virtual void DoCloseUI(UIViewWrapper wrapper)
        {
            wrapper.DoClose();
        }

        public virtual void OnAfterCloseUI(UIViewWrapper wrapper)
        {

        }

        /// <summary>
        /// 关闭组内所有界面
        /// </summary>
        public virtual void CloseAll()
        {
            foreach (var wrapper in _viewWrappers)
            {
                wrapper.DoClose();
                wrapper.Release(UIMgr.UIPrefabPool);
            }
            _viewWrappers.Clear();
            _viewWrapperMap.Clear();
        }

        public virtual void HideAll()
        {
            foreach (var wrapper in _viewWrappers)
            {
                wrapper.DoHide();
            }
        }

        public virtual void SecondUpdate()
        {
            foreach (var wrapper in _viewWrappers)
            {
                wrapper.SecondUpdate();
            }
        }
    }
}

