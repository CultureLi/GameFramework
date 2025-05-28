using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        public string UIAssetRootPath { get; set; } = "Assets/BundleRes/UI";

        private readonly Dictionary<UIGroupType, IUIGroup> _groups;
        private readonly HashSet<string> _loadingUIs;
        private readonly HashSet<string> _toReleaseOnLoading;
        private IResourceMgr _resourceMgr;
        private readonly Dictionary<string, UICreateInfo> _createInfos;
        private PrefabObjectPool _viewGoPool;


        /// <summary>
        /// 初始化界面管理器的新实例。
        /// </summary>
        public UIManager()
        {
            _groups = new Dictionary<UIGroupType, IUIGroup>();
            _loadingUIs = new HashSet<string>();
            _toReleaseOnLoading = new HashSet<string>();
            _createInfos = new Dictionary<string, UICreateInfo>();
            _resourceMgr = null;
            _viewGoPool = PrefabObjectPool.Create("UIPrefab");
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
        /// 界面管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach ((var _, var group) in _groups)
            {
                group.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        public void Shutdown()
        {
            _groups.Clear();
            _loadingUIs.Clear();
            _toReleaseOnLoading.Clear();
        }


        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="resourceManager">资源管理器。</param>
        public void SetResourceManager(IResourceMgr resourceManager)
        {
            if (resourceManager == null)
            {
                throw new Exception("Resource manager is invalid.");
            }

            _resourceMgr = resourceManager;
        }

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(UIGroupType type)
        {
            return _groups.ContainsKey(type);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(UIGroupType type)
        {
            return _groups.TryGetValue(type, out var group) ? group : null;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups()
        {
            int index = 0;
            IUIGroup[] results = new IUIGroup[_groups.Count];
            foreach ((var _, var group) in _groups)
            {
                results[index++] = group;
            }

            return results;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        public void GetAllUIGroups(List<IUIGroup> results)
        {
            if (results == null)
            {
                throw new Exception("Results is invalid.");
            }

            foreach ((var _, var group) in _groups)
            {
                results.Add(group);
            }
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="type">界面组名称。</param>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(UIGroupType type, Transform groupRoot)
        {
            if (HasUIGroup(type))
            {
                return false;
            }

            _groups.Add(type, new UIGroup(type, groupRoot));

            return true;
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="type">界面组名称。</param>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="uiGroupHelper">界面组辅助器。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddUIGroup(IUIGroup group, Transform groupRoot)
        {
            if (group == null)
                return false;

            if (HasUIGroup(group.GroupType))
            {
                return false;
            }

            _groups.Add(group.GroupType, group);

            return true;
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUI(string name)
        {
            foreach ((var _, var group) in _groups)
            {
                if (group.HasUI(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="name">界面序列编号。</param>
        /// <returns>要获取的界面。</returns>
        public ViewBase GetUI(string name)
        {
            foreach ((var _, var group) in _groups)
            {
                var view = group.GetUI(name);
                if (view != null)
                    return view;
            }

            return null;
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="name">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUI(string name)
        {
            return _loadingUIs.Contains(name);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面的序列编号。</returns>
        public void OpenUI(string name, UIGroupType groupType, ViewData userData = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new Exception("UI form asset name is invalid.");
            }

            UIGroup uiGroup = (UIGroup)GetUIGroup(groupType);
            if (uiGroup == null)
            {
                throw new Exception($"UI group '{groupType}' is not exist.");
            }

            if (_loadingUIs.Contains(name))
            {
                Debug.Log("正在加载，不要着急");
                return;
            }

            var assetPath = $"{UIAssetRootPath}/{name}.prefab";
            var viewGo = _viewGoPool.Spawn(assetPath);

            if (_toReleaseOnLoading.Contains(name))
            {
                _toReleaseOnLoading.Remove(name);
                return;
            }
            _loadingUIs.Remove(name);

            uiGroup.OpenUI(name, userData, viewGo);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(string name, UIGroupType groupType)
        {
            UIGroup uiGroup = (UIGroup)GetUIGroup(groupType);
            if (uiGroup == null)
            {
                throw new Exception($"UI group '{groupType}' is not exist.");
            }

            var uiView = uiGroup.GetUI(name);
            _viewGoPool.UnSpawn(uiView.gameObject);
            uiGroup.CloseUI(name);
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUI(string name, ViewData userData)
        {
            var uiForm = GetUI(name);
            if (uiForm == null)
            {
                throw new Exception("UI form is invalid.");
            }

            UIGroup uiGroup = (UIGroup)uiForm.Group;
            if (uiGroup == null)
            {
                throw new Exception("UI group is invalid.");
            }

            uiGroup.RefocusUI(name, userData);
        }

       
    }
}
