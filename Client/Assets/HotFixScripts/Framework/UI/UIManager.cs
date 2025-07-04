﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        public string UIAssetRootPath => _uiAssetRootPath;
        private string _uiAssetRootPath = "Assets/BundleRes/UI";

        private readonly Dictionary<int, IUIGroup> _groups;
        private readonly Dictionary<string, UIViewWrapper> _viewWrapperMap = new Dictionary<string, UIViewWrapper>();
        private readonly HashSet<string> _loadingUIs;
        private readonly HashSet<string> _toReleaseOnLoading;
        private IResourceMgr _resourceMgr;
        private PrefabObjectPool _viewGoPool;

        /// <summary>
        /// 初始化界面管理器的新实例。
        /// </summary>
        public UIManager()
        {
            _groups = new Dictionary<int, IUIGroup>();
            _viewWrapperMap = new Dictionary<string, UIViewWrapper>();
            _loadingUIs = new HashSet<string>();
            _toReleaseOnLoading = new HashSet<string>();
            _resourceMgr = FrameworkMgr.GetModule<IResourceMgr>();
            _viewGoPool = PrefabObjectPool.Create("UIPrefabPool");
        }

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount
        {
            get
            {
                return _groups.Count;
            }
        }

        /// <summary>
        /// 设置ui资源根目录
        /// </summary>
        /// <param name="uiAssetRootPath"></param>
        public void SetUIAssetRootPath(string uiAssetRootPath)
        {
            _uiAssetRootPath = uiAssetRootPath;
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="type">界面组id。</param>
        /// <param name="groupRoot">界面组根节点。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(int groupId, Transform groupRoot)
        {
            if (HasUIGroup(groupId))
            {
                return false;
            }
            var group = new UIGroup(groupId, groupRoot);
            group.UIMgr = this;
            _groups.Add(groupId, group);

            return true;
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="type">界面组对象。</param>
        /// <param name="groupRoot">界面组根节点。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(IUIGroup group, Transform groupRoot)
        {
            if (group == null)
                return false;

            if (HasUIGroup(group.GroupId))
            {
                return false;
            }
            group.UIMgr = this;
            _groups.Add(group.GroupId, group);

            return true;
        }

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="groupId">界面组Id。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(int groupId)
        {
            return _groups.ContainsKey(groupId);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="groupId">界面组Id。</param>
        /// <returns>要获取的界面组。</returns>
        private IUIGroup GetUIGroup(int groupId)
        {
            return _groups.TryGetValue(groupId, out var group) ? group : null;
        }

        /// <summary>
        /// 该UI是否正在打开，不一定在最上层
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUI(string name)
        {
            return _viewWrapperMap.ContainsKey(name);
        }

        private UIViewWrapper GetViewWrapper(string name)
        {
            return _viewWrapperMap.TryGetValue(name,out var viewWrapper) ? viewWrapper : null;
        }

        private bool AddViewWrapper(string name, UIViewWrapper wrapper)
        {
            return _viewWrapperMap.TryAdd(name, wrapper);
        }

        private bool RemoveViewWrapper(string name)
        {
            return _viewWrapperMap.Remove(name);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="name">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        private bool IsLoadingUI(string name)
        {
            return _loadingUIs.Contains(name);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="name">界面资源名称。</param>
        /// <param name="groupId">界面组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面的序列编号。</returns>
        public void OpenUI(string name, int groupId, ViewData userData = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("UI name is invalid.");
            }

            UIGroup uiGroup = (UIGroup)GetUIGroup(groupId);
            if (uiGroup == null)
            {
                Debug.LogError($"UI group '{groupId}' is not exist.");
            }

            //已经打开，从新聚焦
            var wrapper = GetViewWrapper(name);
            if (wrapper != null)
            {
                uiGroup.RefocusUI(wrapper);
                return;
            }

            if (_loadingUIs.Contains(name))
            {
                Debug.Log($"UI {name} is loading....");
                return;
            }

            var assetPath = $"{UIAssetRootPath}/{name}.prefab";

            _loadingUIs.Add(name);

            if (userData != null && userData.AsyncLoad)
            {
                _viewGoPool.SpawnAsync(assetPath, (assetGo)=>
                {
                    OnLoadAssetCompleted(uiGroup, name, userData, assetGo);
                });
            }
            else
            {
                var assetGo = _viewGoPool.Spawn(assetPath);
                OnLoadAssetCompleted(uiGroup, name, userData, assetGo);
            }
        }

        void OnLoadAssetCompleted(IUIGroup uiGroup, string name, ViewData userData, GameObject viewGo)
        {
            _loadingUIs.Remove(name);

            if (_toReleaseOnLoading.Contains(name))
            {
                _toReleaseOnLoading.Remove(name);
                return;
            }

            if (viewGo != null)
            {
                var wrapper = UIViewWrapper.Create(uiGroup, name, userData, viewGo);
                AddViewWrapper(name, wrapper);
                uiGroup.OpenUI(wrapper);
            }
            else
            {
                Debug.Log($"Load UI Failure: {name}");
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="name">要关闭的界面。</param>
        public void CloseUI(string name)
        {
            if (IsLoadingUI(name))
            {
                _toReleaseOnLoading.Add(name);
                _loadingUIs.Remove(name);
                return;
            }

            var wrapper = GetViewWrapper(name);
            if (wrapper != null)
            {
                wrapper.UIGroup.CloseUI(wrapper);
                wrapper.UnSpawnView(_viewGoPool);
                UIViewWrapper.Release(wrapper);
                RemoveViewWrapper(name);
            }
        }

        /// <summary>
        /// //关闭该group下所有UI
        /// </summary>
        /// <param name="groupId"></param>
        public void CloseAllUI(int groupId)
        {
            var keyList = _viewWrapperMap.Where(kv => kv.Value.UIGroup.GroupId == groupId)
                    .Select(kv => kv.Key)
                    .ToList();
            foreach (var key in keyList)
            {
                CloseUI(key);
            }
        }

        /// <summary>
        /// 关闭所有UI
        /// </summary>
        public void CloseAllUI()
        {
            var keyList = _viewWrapperMap.Keys.ToList();
            foreach (var key in keyList)
            {
                CloseUI(key);
            }
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="name">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUI(string name, ViewData userData)
        {
            var wrapper = GetViewWrapper(name);
            if (wrapper != null)
            {
                wrapper.UpdateViewData(userData);
                wrapper.UIGroup.RefocusUI(wrapper);
            }
        }

        /// <summary>
        /// 界面管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {

        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        public void Shutdown()
        {
            foreach ((var _, var group) in _groups)
            {
                group.CloseAll();
            }

            _groups.Clear();
            _loadingUIs.Clear();
            _toReleaseOnLoading.Clear();
        }
    }
}
