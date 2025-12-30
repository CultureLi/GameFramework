using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面管理器接口。
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// UI资源根路径。
        /// </summary>
        string UIAssetRootPath { get;}

        PrefabObjectPool UIPrefabPool { get;}

        /// <summary>
        /// 添加界面组。
        /// </summary>
        /// <param name="groupType"></param>
        /// <param name="groupRoot"></param>
        /// <returns></returns>
        bool AddGroup(EUIGroupType groupType, Transform groupRoot);

        /// <summary>
        /// 界面是否打开状态，不一定在最上层
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool HasUI(string name);

        /// <summary>
        /// 打开主界面
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        void OpenHud(string name, ViewData userData = null);

        /// <summary>
        /// 打开一级界面（活动 / 背包 / 商店 / 角色）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        void OpenView(string name, ViewData userData = null);

        /// <summary>
        /// 打开弹窗界面（二级界面 / 消息框 / MsgBox)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        void OpenPopup(string name, ViewData userData = null);

        /// <summary>
        /// 打开Tips界面（飘字、气泡、跑马灯等）
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userData"></param>
        void OpenTips(string name, ViewData userData = null);

        /// <summary>
        /// 重新激活某个组中最上层UI
        /// </summary>
        /// <param name="groupType"></param>
        /// <returns></returns>
        bool RefocusTopUI(EUIGroupType groupType);

        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="name"></param>
        void CloseUI(string name);

        /// <summary>
        /// //关闭Group下所有UI
        /// </summary>
        void CloseAll(EUIGroupType groupType);

        /// <summary>
        /// 关闭所有UI
        /// </summary>
        void CloseAll();

        /// <summary>
        /// 隐藏Group下所有UI
        /// </summary>
        /// <param name="groupType"></param>
        void HideAll(EUIGroupType groupType);
    }
}
