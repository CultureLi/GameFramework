using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面管理器。
    /// </summary>
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        public UIRoot UIRoot
        {
            get; private set;
        }
        public string UIAssetRootPath
        {
            get; set;
        }

        public PrefabObjectPool UIPrefabPool
        {
            get; private set;
        }

        /// <summary>
        /// 界面组，具体事务由对应组完成
        /// </summary>
        private readonly Dictionary<EUIGroupType, UIGroupBase> _groups;

        /// <summary>
        /// 初始化界面管理器的新实例。
        /// </summary>
        public UIManager()
        {
            UIAssetRootPath = "Assets/BundleRes/UI";
            _groups = new Dictionary<EUIGroupType, UIGroupBase>();
            UIPrefabPool = PrefabObjectPool.Create("UIPrefabPool", expireTime:5);
        }

        public void Init(IResourceMgr resMgr)
        {
            var handle = resMgr.InstantiateAsync("Assets/BundleRes/UI/Root/UIRoot.prefab");
            handle.WaitForCompletion();
            UIRoot = handle.Result.GetComponent<UIRoot>();
            GameObject.DontDestroyOnLoad(handle.Result);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="type">界面组id。</param>
        /// <param name="groupRoot">界面组根节点。</param>
        /// <returns>是否增加界面组成功。</returns>
        public bool AddGroup(EUIGroupType groupType, Transform groupRoot)
        {
            if (GetGroup(groupType) != null)
            {
                return false;
            }

            UIGroupBase group = null;
            switch (groupType)
            {
                case EUIGroupType.HUD:
                    group = new UIGroupHud(this, groupType, groupRoot);
                    break;
                case EUIGroupType.Wnd:
                    group = new UIGroupWnd(this, groupType, groupRoot);
                    break;
                case EUIGroupType.Popup:
                    group = new UIGroupPopup(this, groupType, groupRoot);
                    break;
                case EUIGroupType.Tips:
                    group = new UIGroupTips(this, groupType, groupRoot);
                    break;
                default:
                    Debug.LogError("UIManager AddGroup() Error, Not Support GroupType:" + groupType);
                    return false;
            }

            _groups.Add(groupType, group);

            return true;
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="groupId">界面组Id。</param>
        /// <returns>要获取的界面组。</returns>
        public UIGroupBase GetGroup(EUIGroupType groupType)
        {
            return _groups.GetValueOrDefault(groupType);
        }

        /// <summary>
        /// 该界面是否正存在某个组内，不一定是显示状态
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasUI(string name)
        {
            foreach ((var _, var group) in _groups)
            {
                if (group.HasUI(name))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 打开主界面
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        public void OpenHud(string name, ViewData userData = null)
        {
            InternalOpenUI(EUIGroupType.HUD, name, userData);
        }

        /// <summary>
        /// 打开一级界面（活动 / 背包 / 商店 / 角色）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        public void OpenWnd(string name, ViewData userData = null)
        {
            InternalOpenUI(EUIGroupType.Wnd, name, userData);
        }

        /// <summary>
        /// 打开弹窗界面（二级界面 / 消息框 / MsgBox)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        public void OpenPopup(string name, ViewData userData = null)
        {
            InternalOpenUI(EUIGroupType.Popup, name, userData);
        }

        /// <summary>
        /// 打开Tips界面（飘字、气泡、跑马灯等）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        public void OpenTips(string name, ViewData userData = null)
        {
            InternalOpenUI(EUIGroupType.Tips, name, userData);
        }

        /// <summary>
        /// 打开界面
        /// </summary>
        /// <param name="groupType"></param>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        private void InternalOpenUI(EUIGroupType groupType, string name, ViewData userData = null)
        {
            var group = GetGroup(groupType);
            group.OpenUI(name, userData);
        }

        /// <summary>
        /// 激活最顶部ui
        /// </summary>
        /// <param name="groupType"></param>
        /// <returns></returns>
        public bool RefocusTopUI(EUIGroupType groupType)
        {
            var group = GetGroup(groupType);
            return group.RefocusTopUI();
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="name">要关闭的界面。</param>
        public void CloseUI(string name)
        {
            foreach ((var _, var group) in _groups)
            {
                if (group.CloseUI(name))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// //关闭该group下所有UI
        /// </summary>
        /// <param name="groupType"></param>
        public void CloseAll(EUIGroupType groupType)
        {
            var group = GetGroup(groupType);
            group.CloseAll();
        }

        /// <summary>
        /// 关闭所有UI
        /// </summary>
        public void CloseAll()
        {
            foreach ((var _, var group) in _groups)
            {
                group.CloseAll();
            }
        }

        /// <summary>
        /// 隐藏Group下所有UI
        /// </summary>
        /// <param name="groupType"></param>
        public void HideAll(EUIGroupType groupType)
        {
            var group = GetGroup(groupType);
            group.HideAll();
        }

        /// <summary>
        /// 界面管理器轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        float _lastSecondUpdateTime = 0;
        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (Time.realtimeSinceStartup - _lastSecondUpdateTime >= 1)
            {
                _lastSecondUpdateTime = Time.realtimeSinceStartup;
                SecondUpdate();
            }
        }

        void SecondUpdate()
        {
            foreach ((var _, var group) in _groups)
            {
                group.SecondUpdate();
            }
        }

        /// <summary>
        /// 关闭并清理界面管理器。
        /// </summary>
        public void Shutdown()
        {
            CloseAll();
            _groups.Clear();
        }
    }
}
