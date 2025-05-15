using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    internal sealed partial class UIManager : IFramework, IUIManager
    {
        /// <summary>
        /// 界面组。
        /// </summary>
        private sealed partial class UIGroup : IUIGroup
        {

            Transform _root;
            UIGroupType _groupType;
            public UIGroupType GroupType => _groupType;

            private readonly LinkedList<UIFormInfo> m_UIFormInfos = new LinkedList<UIFormInfo>();
            private readonly Dictionary<string, UIFormInfo> _pool = new Dictionary<string, UIFormInfo>();

            public UIGroup(UIGroupType type, Transform root)
            {
                _root = root;
                _groupType = type;
            }

            /// <summary>
            /// 获取界面组中界面数量。
            /// </summary>
            public int UIFormCount
            {
                get
                {
                    return m_UIFormInfos.Count;
                }
            }

            /// <summary>
            /// 获取当前界面。
            /// </summary>
            public ViewBase CurrentUIForm
            {
                get
                {
                    return m_UIFormInfos.First != null ? m_UIFormInfos.First.Value.View : null;
                }
            }

            /// <summary>
            /// 界面组中是否存在界面。
            /// </summary>
            /// <param name="serialId">界面序列编号。</param>
            /// <returns>界面组中是否存在界面。</returns>
            public bool HasUIForm(string name)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    if (uiFormInfo.View.UIName == name)
                    {
                        return true;
                    }
                }

                return false;
            }


            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            /// <param name="serialId">界面序列编号。</param>
            /// <returns>要获取的界面。</returns>
            public ViewBase GetUIForm(string name)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    if (uiFormInfo.View.UIName == name)
                    {
                        return uiFormInfo.View;
                    }
                }

                return null;
            }


            /// <summary>
            /// 往界面组增加界面。
            /// </summary>
            /// <param name="uiForm">要增加的界面。</param>
            public void AddUIForm(string name, ViewData data)
            {
                m_UIFormInfos.AddFirst(UIFormInfo.Create(name, data));
            }

            /// <summary>
            /// 从界面组移除界面。
            /// </summary>
            /// <param name="view">要移除的界面。</param>
            public void RemoveUIForm(ViewBase view)
            {
                UIFormInfo uiFormInfo = GetUIFormInfo(view);
                if (uiFormInfo == null)
                {
                    throw new Exception($"Can not find UI {view.UIName}'.");
                }

                if (!m_UIFormInfos.Remove(uiFormInfo))
                {
                    throw new Exception($"UI group '{_groupType}' not exists specified UI form '{view.UIName}'.");
                }

                ReferencePool.Release(uiFormInfo);
            }

            /// <summary>
            /// 激活界面。
            /// </summary>
            /// <param name="uiForm">要激活的界面。</param>
            /// <param name="data">用户自定义数据。</param>
            public void RefocusUIForm(ViewBase view, ViewData data)
            {
                UIFormInfo uiFormInfo = GetUIFormInfo(view);
                if (uiFormInfo == null)
                {
                    throw new Exception("Can not find UI form info.");
                }

                m_UIFormInfos.Remove(uiFormInfo);
                m_UIFormInfos.AddFirst(uiFormInfo);
            }

            /// <summary>
            /// 刷新界面组。
            /// </summary>
            public void Refresh()
            {
               
            }

           
            private UIFormInfo GetUIFormInfo(ViewBase view)
            {
                if (view == null)
                {
                    throw new Exception("UI form is invalid.");
                }

                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    if (uiFormInfo.View == view)
                    {
                        return uiFormInfo;
                    }
                }

                return null;
            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
            }
        }
    }
}
