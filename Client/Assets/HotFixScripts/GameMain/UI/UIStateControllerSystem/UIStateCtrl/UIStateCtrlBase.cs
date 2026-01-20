using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace GameMain.UI
{
    [ExecuteAlways]
    public abstract class UIStateCtrlBase : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        protected int _ctrlUID;
        public int CtrlUID => _ctrlUID;
        public abstract void Capture(int idx);
        public abstract void Apply(int idx);

#if UNITY_EDITOR
        public abstract void OnAddState();
        public abstract void OnRemoveState();
        protected abstract void OnDestroyed();

        protected void OnDestroy()
        {
            OnDestroyed();
        }
#endif
    }

    public class UIStateCtrlBase<TValue> : UIStateCtrlBase
    {
        [SerializeField]
        [ListDrawerSettings(HideAddButton = true,
            DraggableItems = false,
            ShowIndexLabels = true,
            HideRemoveButton = true,
            OnBeginListElementGUI = "BeginItem",
            OnEndListElementGUI = "EndItem")]
        protected List<TValue> _values = new List<TValue>();

        protected virtual TValue TargetValue { get; set; }

        override public void Apply(int idx)
        {
            TargetValue = _values[idx];
        }

        override public void Capture(int idx)
        {
            _values[idx] = TargetValue;
        }

        public T GetOrAddComponent<T>() where T : Component
        {
            var com = gameObject.GetComponent<T>();
            if (!com) com = gameObject.AddComponent<T>();
            return com;
        }

#if UNITY_EDITOR

        UIStateControllerMgr _ctrlMgr;
        public UIStateControllerMgr CtrlMgr
        {
            get
            {
                if (_ctrlMgr == null)
                {
                    _ctrlMgr = GetComponentInParent<UIStateControllerMgr>(true);
                }
                return _ctrlMgr;
            }
        }

        void FixedValuesCnt()
        {
            if (CtrlMgr == null)
                return;
            var controller = CtrlMgr.GetController(_ctrlUID);
            if (controller == null)
                return;

            var stateCnt = controller.StateCount;
            var valueCnt = _values.Count;

            for (var i = valueCnt; i < stateCnt; i++)
            {
                var v = TargetValue;
                _values.Add(v);
            }

            for (var i = stateCnt; i < valueCnt; i++)
            {
                _values.RemoveAt(_values.Count - 1);
            }
        }

        void Reset()
        {
            if (CtrlMgr == null)
            {
                EditorUtility.DisplayDialog("提示", "先给合适的父节点上添加UIStateControllerMgr组件", "确定");
                return;
            }

            if (CtrlMgr.ControllerList.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "需要在UIStateControllerMgr中添加一个控制器吧", "确定");
                return;
            }

            var controller = CtrlMgr.ControllerList[0];
            _ctrlUID = controller.UID;
            FixedValuesCnt();
            controller.OnAddCtrl(this);
        }

        override protected void OnDestroyed()
        {
            if (CtrlMgr == null)
                return;

            var controller = CtrlMgr.GetController(_ctrlUID);
            if (controller != null)
            {
                controller.OnRemoveCtrl(this);
            }
        }


        override public void OnAddState()
        {
            FixedValuesCnt();
        }

        override public void OnRemoveState()
        {
            FixedValuesCnt();
        }

        [OnInspectorGUI]
        [PropertyOrder(-1)]
        protected virtual void Draw()
        {
            if (CtrlMgr == null)
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Label("控制器:", GUILayout.Width(50));
            GUILayout.FlexibleSpace();
            var controller = CtrlMgr.GetController(_ctrlUID);
            var selectedIdx = -1;
            var cnt = CtrlMgr.ControllerList.Count;
            string[] options = new string[cnt];
            for (var i = 0; i < cnt; i++)
            {
                var ctrl = CtrlMgr.ControllerList[i];
                if (controller?.UID == ctrl.UID)
                    selectedIdx = i;

                options[i] = $"{ctrl.Name} (UID:{ctrl.UID})";

            }
            var newSelectedIdx = EditorGUILayout.Popup(selectedIdx, options);

            if (newSelectedIdx != selectedIdx)
            {
                _ctrlUID = CtrlMgr.ControllerList[newSelectedIdx].UID;
                FixedValuesCnt();
                controller?.OnRemoveCtrl(this);
                controller = CtrlMgr.GetController(_ctrlUID);
                controller?.OnAddCtrl(this);
            }
            GUILayout.EndHorizontal();

            if (controller != null)
            {
                SirenixEditorGUI.BeginHorizontalToolbar();

                controller.DrawStateItems();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("记录", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    Capture(controller.SelectedIndex);
                }

                SirenixEditorGUI.EndHorizontalToolbar();
            }
        }

        void BeginItem(int index)
        {
            Rect rect = EditorGUILayout.BeginVertical();
            if (CtrlMgr == null)
                return;
            var controller = CtrlMgr.GetController(_ctrlUID);
            if (controller == null)
                return;
            if (index == controller.SelectedIndex)
            {
                EditorGUI.DrawRect(rect, Color.green.WithAlpha(0.3f));
            }
        }

        void EndItem(int index)
        {
            EditorGUILayout.EndVertical();
        }

#endif
    }
}