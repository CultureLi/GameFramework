using UnityEngine;

namespace Framework
{
    public enum EUIGroupType
    {
        HUD = 1, // 主界面
        View, // 一级界面（活动 / 背包 / 商店 / 角色）
        Popup, // 弹窗界面（二级界面 / 消息框 / MsgBox)
        Tips, // 提示信息（飘字、气泡、跑马灯等）
    }
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {   
        IUIManager UIMgr
        {
            get;
        }
        EUIGroupType GroupType { get; }
        void OpenUI(string name, ViewData userData);
        bool CloseUI(string name);
        bool HasUI(string name);
        void RefocusUI(UIViewWrapper wrapper);
        void CloseAll();
        void HideAll();
        bool RefocusTopUI();

        void SecondUpdate();
    }
}
