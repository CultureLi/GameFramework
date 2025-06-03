using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {   
        int GroupId { get; }

        void OpenUI(string name, ViewData data, GameObject asset);

        void CloseUI(string name);

        void RefocusUI(string name, ViewData data);

        GameObject GetViewGo(string name);

        void CloseAll();

    }
}
