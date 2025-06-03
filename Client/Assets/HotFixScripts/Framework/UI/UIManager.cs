using System.Collections.Generic;
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
        private readonly Dictionary<string, IUIGroup> _viewNameToGroupMap;
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
            _viewNameToGroupMap = new Dictionary<string, IUIGroup>();
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
        public IUIGroup GetUIGroup(int groupId)
        {
            return _groups.TryGetValue(groupId, out var group) ? group : null;
        }

        /// <summary>
        /// 该UI是否正在打开，不一定在最上层
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool IsOpened(string name)
        {
            return _viewNameToGroupMap.ContainsKey(name);
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
            if (IsOpened(name))
            {
                uiGroup.RefocusUI(name, userData);
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
                _viewNameToGroupMap[name] = uiGroup;
                uiGroup.OpenUI(name, userData, viewGo);
            }
            else
            {
                Debug.Log($"Load UI Failure: {name}");
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUI(string name)
        {
            if (IsLoadingUI(name))
            {
                _toReleaseOnLoading.Add(name);
                _loadingUIs.Remove(name);
                return;
            }

            if (_viewNameToGroupMap.TryGetValue(name, out var uiGroup))
            {
                var viewGo = uiGroup.GetViewGo(name);
                _viewGoPool.UnSpawn(viewGo);
                uiGroup.CloseUI(name);
                _viewNameToGroupMap.Remove(name);
            }
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUI(string name, ViewData userData)
        {
            if (_viewNameToGroupMap.TryGetValue(name, out var uiGroup))
            {
                uiGroup.RefocusUI(name, userData);
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
