using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面管理器接口。
    /// </summary>
    public interface IUIManager
    {
        string UIAssetRootPath { get; set; }
        bool AddUIGroup(UIGroupType type, Transform groupRoot);

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        int UIGroupCount
        {
            get;
        }

        /// <summary>
        /// 设置资源管理器。
        /// </summary>
        /// <param name="resourceManager">资源管理器。</param>
        void SetResourceManager(IResourceMgr resourceManager);

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否存在界面组。</returns>
        bool HasUIGroup(UIGroupType type);

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        IUIGroup GetUIGroup(UIGroupType type);

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        IUIGroup[] GetAllUIGroups();

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <param name="results">所有界面组。</param>
        void GetAllUIGroups(List<IUIGroup> results);


        /// <summary>
        /// 是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否存在界面。</returns>
        bool HasUI(string uiFormAssetName);


        /// <summary>
        /// 获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        ViewBase GetUI(string uiFormAssetName);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        bool IsLoadingUI(string uiFormAssetName);

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>界面的序列编号。</returns>
        void OpenUI(string name, UIGroupType groupType, ViewData userData = null);


        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        void CloseUI(string name);

        /// <summary>
        /// 激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        void RefocusUI(string name, ViewData userData);
    }
}
