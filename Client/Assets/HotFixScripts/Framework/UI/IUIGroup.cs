//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {
        
        UIGroupType GroupType { get; }

        /// <summary>
        /// 获取当前界面。
        /// </summary>
        ViewBase CurrentUIForm
        {
            get;
        }

        void OpenUI(string name, ViewData data, GameObject asset);

        void CloseUI(string name);

        void Update(float elapseSeconds, float realElapseSeconds);

        /// <summary>
        /// 界面组中是否存在界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>界面组中是否存在界面。</returns>
        bool HasUIView(string name);

        /// <summary>
        /// 从界面组中获取界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>要获取的界面。</returns>
        ViewBase GetUIView(string name);

    }
}
