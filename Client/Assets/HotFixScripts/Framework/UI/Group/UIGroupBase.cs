using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    public enum EUIGroupType
    {
        HUD = 1, // 主界面
        Wnd, // 一级界面（活动 / 背包 / 商店 / 角色）
        Popup, // 弹窗界面（二级界面 / 消息框 / MsgBox)
        Tips, // 提示信息（飘字、气泡、跑马灯等）
    }

    public class UIGroupBase
    {
        public IUIManager UIMgr
        {
            get; private set;
        }

        public Transform Root
        {
            get; private set;
        }

        public Canvas Canvas => Root.GetComponent<Canvas>();

        private int _groupLayer;
        public EUIGroupType GroupType
        {
            get; private set;
        }

        private readonly List<UIViewWrapper> _viewWrappers = new List<UIViewWrapper>();
        private Dictionary<string, UIViewWrapper> _viewWrapperMap = new Dictionary<string, UIViewWrapper>();

        // 正在加载的ui
        private readonly HashSet<string> _loadingUIs = new HashSet<string>();
        // 准备销毁的ui
        private readonly HashSet<string> _toReleaseOnLoading = new HashSet<string>();

        public UIGroupBase(IUIManager uiMgr, EUIGroupType groupType, Transform root)
        {
            UIMgr = uiMgr;
            Root = root;
            GroupType = groupType;
            _groupLayer = (int)GroupType * 10000;
        }

        /// <summary>
        /// 计算层级,
        /// 如果在预制体中提前设置了SortingOrder，就用_groupLayer + SortingOrder
        /// 否则根据打开顺序计算
        /// </summary>
        /// <returns></returns>
        private int CalculateOrder(int idx)
        {
            var wrapper = _viewWrappers[idx];
            if (wrapper.CustomSortingOrder != 0)
            {
                return _groupLayer + wrapper.CustomSortingOrder;
            }
            return _groupLayer + idx * 100;
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
                return;
            }

            //已存在，激活（重新打开时要把新的 userData 带进去，否则显示的还是上一次的数据）
            var wrapper = GetViewWrapper(name);
            if (wrapper != null)
            {
                wrapper.UpdateViewData(userData);
                RefocusUI(wrapper);
                return;
            }

            // 正在加载
            if (_loadingUIs.Contains(name))
            {
                // "加载中先关掉、又立刻打开"：撤销关闭意图，等加载完成后正常打开。
                _toReleaseOnLoading.Remove(name);
                Debug.Log($"UI {name} is loading....");
                return;
            }

            // 添加到加载列表中
            _loadingUIs.Add(name);

            var assetPath = $"{UIMgr.UIAssetRootPath}/{name}.prefab";

            // userData 为 null 时走 ViewData.DefaultAsyncLoad（默认异步），
            // 避免每次首次打开界面都在主线程 WaitForCompletion。
            bool asyncLoad = userData != null ? userData.AsyncLoad : ViewData.DefaultAsyncLoad;

            if (asyncLoad)
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

            // 在加载过程中关闭了界面：对象已经实例化出来了，必须还回池子，
            // 否则它会一直挂在池子里处于"已获取"状态，永远不会被回收。
            if (_toReleaseOnLoading.Remove(name))
            {
                if (viewGo != null)
                {
                    UIMgr.UIPrefabPool.UnSpawn(viewGo);
                }
                return;
            }

            if (viewGo == null)
            {
                Debug.LogError($"Load UI Failure: {name}");
                return;
            }

            var view = viewGo.GetComponent<ViewBase>();
            if (view == null)
            {
                Debug.LogError($"Load UI Failure: {name} 根节点上没有 ViewBase 组件。");
                UIMgr.UIPrefabPool.UnSpawn(viewGo);
                return;
            }

            var wrapper = UIViewWrapper.Create(this, name, userData, view);
            OnBeforeOpenUI(wrapper);
            DoOpenUI(wrapper);
            OnAfterOpenUI(wrapper);
        }

        public virtual void OnBeforeOpenUI(UIViewWrapper wrapper)
        {

        }

        public virtual void DoOpenUI(UIViewWrapper wrapper)
        {
            _viewWrappers.Add(wrapper);
            _viewWrapperMap[wrapper.assetPath] = wrapper;
            //wrapper.SetSortingOrder(CalculateOrder(_viewWrappers.Count - 1));
            wrapper.DoShow(CalculateOrder(_viewWrappers.Count - 1));
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
            RefreshSortingOrder();
            OnBeforeOpenUI(wrapper);
            DoOpenUI(wrapper);
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
            {
                // 还在异步加载中：记下关闭意图，加载完成时直接把实例还回池子。
                if (_loadingUIs.Contains(name))
                {
                    _toReleaseOnLoading.Add(name);
                    return true;
                }

                return false;
            }

            _viewWrappers.Remove(wrapper);
            _viewWrapperMap.Remove(wrapper.assetPath);
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
            // 先摘记录再逐个关闭：DoClose 里的业务回调（OnClose）可能又去开/关其他界面，
            // 边遍历边改 _viewWrappers 会抛集合被修改异常，甚至重复释放。
            var wrappers = _viewWrappers.ToArray();
            _viewWrappers.Clear();
            _viewWrapperMap.Clear();

            foreach (var wrapper in wrappers)
            {
                wrapper.DoClose();
                wrapper.Release(UIMgr.UIPrefabPool);
            }

            // 正在加载中的界面也一并标记为"加载完就丢弃"，否则关全部之后它们还会弹出来。
            foreach (var loadingName in _loadingUIs)
            {
                _toReleaseOnLoading.Add(loadingName);
            }
        }

        public virtual void HideAll()
        {
            for (int i = 0; i < _viewWrappers.Count; i++)
            {
                _viewWrappers[i].DoHide();
            }
        }

        public int GetTopUISortingOrder()
        {
            if (_viewWrappers.Count == 0)
                return _groupLayer;
            return _viewWrappers.Last().Canvas.sortingOrder;
        }

        public virtual void SecondUpdate()
        {
            // 快照长度 + 边界判断：界面的 SecondUpdate 里关掉自己是常见写法。
            for (int i = 0; i < _viewWrappers.Count; i++)
            {
                _viewWrappers[i].SecondUpdate();
            }
        }
    }
}

