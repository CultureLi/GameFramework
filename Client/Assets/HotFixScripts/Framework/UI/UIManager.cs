using System;
using System.Collections.Generic;
using UnityEngine;
namespace Framework
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        private readonly Dictionary<UIGroupType, IUIGroup> m_UIGroups;
        private readonly HashSet<string> m_UIFormsBeingLoaded;
        private readonly HashSet<int> m_UIFormsToReleaseOnLoad;
        private IResourceMgr m_ResourceManager;
        private int m_Serial;
        private bool m_IsShutdown;

        private EventHandler<CloseUIFormComplete> m_CloseUIFormCompleteEventHandler;

        /// <summary>
        /// 初始化界面管理器的新实例。
        /// </summary>
        public UIManager()
        {
            m_UIGroups = new Dictionary<UIGroupType, IUIGroup>();
            m_UIFormsBeingLoaded = new HashSet<string>();
            m_UIFormsToReleaseOnLoad = new HashSet<int>();
            m_ResourceManager = null;
            m_Serial = 0;
            m_IsShutdown = false;
            m_CloseUIFormCompleteEventHandler = null;
        }

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        public int UIGroupCount
        {
            get
            {
                return m_UIGroups.Count;
            }
        }


        /// <summary>
        /// 界面管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach ((var _, var group) in m_UIGroups)
            {
                group.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        public void Shutdown()
        {
            m_IsShutdown = true;
            m_UIGroups.Clear();
            m_UIFormsBeingLoaded.Clear();
            m_UIFormsToReleaseOnLoad.Clear();
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

            m_ResourceManager = resourceManager;
        }

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        public bool HasUIGroup(UIGroupType type)
        {
            return m_UIGroups.ContainsKey(type);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(UIGroupType type)
        {
            return m_UIGroups.TryGetValue(type, out var group) ? group : null;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups()
        {
            int index = 0;
            IUIGroup[] results = new IUIGroup[m_UIGroups.Count];
            foreach ((var _, var group) in m_UIGroups)
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

            foreach ((var _, var group) in m_UIGroups)
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

            m_UIGroups.Add(type, new UIGroup(type, groupRoot));

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

            m_UIGroups.Add(group.GroupType, group);

            return true;
        }

        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否存在界面。</returns>
        public bool HasUIForm(string name)
        {
            foreach ((var _, var group) in m_UIGroups)
            {
                if (group.HasUIForm(name))
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
        public ViewBase GetUIForm(string name)
        {
            foreach ((var _, var group) in m_UIGroups)
            {
                var view = group.GetUIForm(name);
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
        public bool IsLoadingUIForm(string name)
        {
            return m_UIFormsBeingLoaded.Contains(name);
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

            if (m_UIFormsBeingLoaded.Contains(name))
            {
                Debug.Log("正在加载，不要着急");
                return;
            }
           /* m_UIFormsBeingLoaded.Add(name);
            var handler = m_ResourceManager.LoadAssetAsync<GameObject>(uiFormAssetName);
            var info = OpenUIFormInfo.Create(serialId, uiGroup, pauseCoveredUIForm, userData);
            handler.Completed += (go) =>
            {
                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    LoadAssetSuccessCallback(uiFormAssetName, go, 0, info);
                }
                else
                {
                    ReferencePool.Release(info);
                }
            };*/
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(string name)
        {
            var uiForm = GetUIForm(name);

            UIGroup uiGroup = (UIGroup)uiForm.Group;
            if (uiGroup == null)
            {
                throw new Exception("UI group is invalid.");
            }

            uiGroup.RemoveUIForm(uiForm);
            uiForm.OnClose();
            uiGroup.Refresh();
        }

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUIForm(string name, ViewData userData)
        {
            var uiForm = GetUIForm(name);
            if (uiForm == null)
            {
                throw new Exception("UI form is invalid.");
            }

            UIGroup uiGroup = (UIGroup)uiForm.Group;
            if (uiGroup == null)
            {
                throw new Exception("UI group is invalid.");
            }

            uiGroup.RefocusUIForm(uiForm, userData);
            uiGroup.Refresh();
            uiForm.OnOpen(userData);
        }

        private void LoadAssetSuccessCallback(string uiFormAssetName, object uiFormAsset, float duration, object userData)
        {
           /* OpenUIFormInfo openUIFormInfo = (OpenUIFormInfo)userData;
            if (openUIFormInfo == null)
            {
                throw new Exception("Open UI form info is invalid.");
            }

            if (m_UIFormsToReleaseOnLoad.Contains(openUIFormInfo.SerialId))
            {
                m_UIFormsToReleaseOnLoad.Remove(openUIFormInfo.SerialId);
                ReferencePool.Release(openUIFormInfo);
                m_UIFormHelper.ReleaseUIForm(uiFormAsset, null);
                return;
            }

            m_UIFormsBeingLoaded.Remove(openUIFormInfo.SerialId);
            UIFormInstanceObject uiFormInstanceObject = UIFormInstanceObject.Create(uiFormAssetName, uiFormAsset, m_UIFormHelper.InstantiateUIForm(uiFormAsset), m_UIFormHelper);
            m_InstancePool.Register(uiFormInstanceObject, true);

            InternalOpenUIForm(openUIFormInfo.SerialId, uiFormAssetName, openUIFormInfo.UIGroup, uiFormInstanceObject.Target, openUIFormInfo.PauseCoveredUIForm, true, duration, openUIFormInfo.UserData);
            ReferencePool.Release(openUIFormInfo);*/
        }
    }
}
