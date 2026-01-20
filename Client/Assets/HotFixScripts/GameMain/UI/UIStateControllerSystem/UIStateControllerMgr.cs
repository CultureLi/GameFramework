using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;



#if UNITY_EDITOR
using UnityEditor;
#endif


namespace GameMain.UI
{
    [ExecuteAlways]
    public class UIStateControllerMgr : MonoBehaviour
    {
        [SerializeField, LabelText("控制器列表:")]
        [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
        List<UIStateController> _controllerList = new List<UIStateController>();
        public List<UIStateController> ControllerList => _controllerList;

        public UIStateController GetController(int uid)
        {
            return _controllerList.Find(e => e.UID == uid);
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += OnPrefabOpend;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            OnPrefabClosing();
            EditorApplication.delayCall -= OnPrefabOpend;
#endif
        }


        //----------------------------- Editor ---------------------------------
#if UNITY_EDITOR
        void OnPrefabOpend()
        {
            if (this == null)
                return;

            //打开prefab的时候设置状态
            if (IsInPrefabEditMode())
            {
                foreach (var ctrl in _controllerList)
                {
                    ctrl.SelectedIndex = 0;
                }
            }
        }
        void OnPrefabClosing()
        {
            // 关闭prefab的时候刷新列表
            if (IsInPrefabEditMode())
            {
                foreach (var ctrl in _controllerList)
                {
                    ctrl.RefreshCtrlList();
                }
            }
        }

        private bool IsInPrefabEditMode()
        {
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            return prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject);
        }

        int GenUID()
        {
            int id = 0;
            foreach (var controller in _controllerList)
            {
                if (controller.UID > id)
                {
                    id = controller.UID;
                }
            }
            return id + 1;
        }

        [OnInspectorGUI]
        [HideLabel]
        [PropertyOrder(0)]
        private void Draw()
        {
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 20;
            if (GUILayout.Button("添加控制器", btnStyle, GUILayout.Height(50)))
            {
                AddController();
            }
        }

        void AddController()
        {
            var controller = new UIStateController(this, GenUID());
            _controllerList.Add(controller);
            EditorUtility.SetDirty(this);
        }
        public void RemoveController(int uid)
        {
            var controller = _controllerList.Find(c => c.UID == uid);
            _controllerList.Remove(controller);
            EditorUtility.SetDirty(this);
        }
#endif
    }

}

