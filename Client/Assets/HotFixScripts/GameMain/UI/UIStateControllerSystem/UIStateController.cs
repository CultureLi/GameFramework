using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using GameEntry;
using System.Linq;
using Framework;



#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;
#endif


namespace GameMain.UI
{
    [Serializable]
    public class UIStateItem
    {
        [ShowInInspector]
        [SerializeField]
        [ReadOnly]
        int _idx;
        public int Idx => _idx;

        [SerializeField]
        public string name;

        public UIStateItem(int idx)
        {
            _idx = idx;
        }

        public string GetShowName()
        {
            if (string.IsNullOrEmpty(name))
                return $"State:{_idx}";

            return $"{name}:{_idx}";
        }
    }

    [Serializable]
    public class UIStateController
    {
        [SerializeField, ReadOnly]
        int _uid;
        public int UID => _uid;
        [SerializeField]
        [OnValueChanged("OnNameValueChanged")]
        string _name;
        public string Name => _name;
        [SerializeReference, HideInInspector]
        UIStateControllerMgr _owner;

        [SerializeField, HideInInspector]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false, DefaultExpandedState = false)]
        [PropertyOrder(1)]
        List<UIStateItem> _states = new List<UIStateItem>()
        {
            new UIStateItem(0), new UIStateItem(1)
        };

        public int StateCount => _states.Count;


        private int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (0 <= value && value < _states.Count)
                {
                    _selectedIndex = value;
                    Apply();
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private List<UIStateCtrlBase> _stateCtrlList = new List<UIStateCtrlBase>();
        private List<UIStateCtrlBase> StateCtrlList => _stateCtrlList;


        public UIStateController(UIStateControllerMgr owner, int uid)
        {
            _owner = owner;
            _uid = uid;
            _name = $"Controller_{uid}";
        }

        void Apply()
        {
            foreach (var ctrl in _stateCtrlList)
            {
                ctrl.Apply(_selectedIndex);
            }
        }

        //----------------------------- Editor ---------------------------------

#if UNITY_EDITOR

        private bool _statesFoldout = false;
        private bool _ctrlListFoldout = false;

        void OnNameValueChanged()
        {
            Debug.Log(_name);
        }
        [OnInspectorGUI]
        [HideLabel]
        [PropertyOrder(0)]
        private void Draw()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();

            DrawStateItems();

            GUILayout.FlexibleSpace();

            // Delete last
            GUI.enabled = _states.Count > 2;
            if (GUILayout.Button("Del", GUILayout.Height(30), GUILayout.Width(40)))
            {
                bool ok = EditorUtility.DisplayDialog("提示", "此操作会删除【最后一个】状态", "确定", "取消");
                if (ok)
                {
                    RemoveState();
                }
            }
            GUI.enabled = true;

            // Add
            if (GUILayout.Button("Add", GUILayout.Height(30), GUILayout.Width(40)))
            {
                AddState();
            }

            SirenixEditorGUI.EndHorizontalToolbar();

            _statesFoldout = EditorGUILayout.Foldout(_statesFoldout, $"编辑", true);
            if (_statesFoldout)
            {
                EditorGUI.indentLevel++;

                foreach (var item in _states)
                {
                    item.name = EditorGUILayout.TextField($"State:{item.Idx} Name:", item.name);
                }

                EditorGUI.indentLevel--;
            }
            GUILayout.BeginHorizontal();

            _ctrlListFoldout = EditorGUILayout.Foldout(_ctrlListFoldout, $"控制列表:", true);
            if (GUILayout.Button("刷新", GUILayout.Width(45)))
            {
                RefreshCtrlList();
            }
            GUILayout.EndHorizontal();
            if (_ctrlListFoldout)
            {
                EditorGUI.indentLevel++;
                foreach (var ctrl in _stateCtrlList)
                {
                    GUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    EditorGUILayout.TextField($"{ctrl.name} ({ctrl.GetType().Name})");
                    GUI.enabled = true;
                    if (GUILayout.Button("选中", GUILayout.Width(45)))
                    {
                        EditorGUIUtility.PingObject(ctrl.gameObject);
                    }

                    GUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            if (GUILayout.Button("删除控制器", GUILayout.Height(25)))
            {
                bool ok = EditorUtility.DisplayDialog("提示", "确定要删除该控制器？", "确定", "取消");
                if (ok)
                {
                    _owner?.RemoveController(UID);
                }
            }

            GUILayout.Space(5);

        }

        public void RefreshCtrlList()
        {
            _stateCtrlList.Clear();
            var list = _owner.GetComponentsInChildren<UIStateCtrlBase>(true);
            foreach (var ctrl in list)
            {
                if (ctrl.CtrlUID == _uid && ctrl.GetComponentInParent<UIStateControllerMgr>(true) == _owner)
                {
                    _stateCtrlList.Add(ctrl);
                }
            }
            EditorUtility.SetDirty(_owner);
        }

        public void DrawStateItems()
        {
            SirenixEditorGUI.BeginHorizontalToolbar();

            // Tabs
            foreach (var item in _states)
            {
                bool isSelected = item.Idx == _selectedIndex;

                Color old = GUI.color;
                if (isSelected)
                    GUI.color = Color.green.WithAlpha(0.8f);

                if (GUILayout.Button(
                    item.GetShowName(), GUILayout.Height(30)))
                {
                    if (_selectedIndex == item.Idx)
                        return;
                    SelectedIndex = item.Idx;
                    EditorUtility.SetDirty(_owner);
                }

                GUI.color = old;
            }

            SirenixEditorGUI.EndHorizontalToolbar();
        }


        private void AddState()
        {
            _states.Add(new UIStateItem(_states.Count));

            foreach (var ctrl in _stateCtrlList)
            {
                ctrl.OnAddState();
            }

            _selectedIndex = _states.Last().Idx;
            EditorUtility.SetDirty(_owner);
        }

        private void RemoveState()
        {
            if (_states.Count <= 2)
                return;

            _states.RemoveLast();

            foreach (var ctrl in _stateCtrlList)
            {
                ctrl.OnRemoveState();
            }
            _selectedIndex = _states.Last().Idx;
            EditorUtility.SetDirty(_owner);
        }

        public void OnAddCtrl(UIStateCtrlBase ctrl)
        {
            if (!_stateCtrlList.Contains(ctrl))
            {
                _stateCtrlList.Add(ctrl);
                EditorUtility.SetDirty(_owner);
            }
        }

        public void OnRemoveCtrl(UIStateCtrlBase ctrl)
        {
            _stateCtrlList.Remove(ctrl);
            EditorUtility.SetDirty(_owner);
        }
#endif
    }

}

