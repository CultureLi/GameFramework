using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {   
        IUIManager UIMgr {
            get; set;
        }
        int GroupId { get; }
        void OpenUI(UIViewWrapper wrapper);
        void CloseUI(UIViewWrapper wrapper);

        void RefocusUI(UIViewWrapper wrapper);

        void CloseAll();

    }
}
