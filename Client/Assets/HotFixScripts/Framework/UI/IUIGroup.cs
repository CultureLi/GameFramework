using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {   
        UIGroupType GroupType { get; }
        ViewBase CurrentView
        {
            get;
        }

        void OpenUI(string name, ViewData data, GameObject asset);

        void CloseUI(string name);

        void Update(float elapseSeconds, float realElapseSeconds);

        bool HasUI(string name);

        ViewBase GetUI(string name);

    }
}
