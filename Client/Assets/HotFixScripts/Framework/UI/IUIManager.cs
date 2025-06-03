using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面管理器接口。
    /// </summary>
    public interface IUIManager
    {
        string UIAssetRootPath { get;}
        bool AddUIGroup(int groupId, Transform groupRoot);

        /// <summary>
        /// 获取界面组数量。
        /// </summary>
        int UIGroupCount
        {
            get;
        }

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        /// <param name="groupId">界面组Id。</param>
        /// <returns>是否存在界面组。</returns>
        bool HasUIGroup(int groupId);

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="groupId">界面组Id。</param>
        /// <returns>要获取的界面组。</returns>
        IUIGroup GetUIGroup(int groupId);

        /// <summary>
        /// 界面是否打开状态，不一定在最上层
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool IsOpened(string name);

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="name">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        bool IsLoadingUI(string name);

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>界面的序列编号。</returns>
        void OpenUI(string name, int groupId, ViewData userData = null);


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
