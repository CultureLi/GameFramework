using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 界面组接口。
    /// </summary>
    public interface IUIGroup
    {   
        UIGroupType GroupType { get; }
        
        PrefabObjectPool ViewPool {
            get; set;
        }

        void OpenUI(string name, ViewData data, GameObject asset);

        void CloseUI(string name);

        void RefocusUI(string name, ViewData data);

        bool HasUI(string name);

        void CloseAll();

    }
}
