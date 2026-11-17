using cfg;
using Framework;
using GameEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static GameMain.Event.Event;

namespace GameMain.UI
{
    internal class UIMain : ViewBase
    {
        /// <summary>主界面「建造」下拉框使用的预制体。</summary>
        private const string BuildDropdownAddress =
            "Assets/AssetBundles/UI/Common/Dropdown/Dropdown.prefab";

        private TMP_Dropdown _buildDropdown;
        private AsyncOperationHandle<GameObject> _buildDropdownHandle;
        private bool _buildDropdownLoading;
        private readonly List<MidgardBuildingCfg> _buildConfigs = new List<MidgardBuildingCfg>();

        public void OnBtnGotoLoginClick()
        {
            FW.I.ReStart();
        }

        public override void OnShow(bool isInitShow, ViewData data)
        {
            base.OnShow(isInitShow, data);
            EnsureBuildDropdown();
        }

        public override void OnClose()
        {
            ReleaseBuildDropdown();
        }


        public void OpenHud2()
        {
            FW.UIMgr.OpenHud("Main/UIHUD2");
        }

        public void OpenRole()
        {
            FW.UIMgr.OpenPopup("Setting/UISetting");
        }

        public void OpenPopup1()
        {
            FW.UIMgr.OpenPopup("Popup/UIPopup1");
        }

        public void ShowTips()
        {
            UIDelegate.I.ShowCommonTips(Time.deltaTime.ToString());
        }

        /// <summary>
        /// 切换内城、世界地图：不真正切场景，只显示/隐藏 Main 场景里的 Midgard / OpenWorld 节点，
        /// 并加载对应区域的地图数据。
        /// </summary>
        public void SwitchScene()
        {
            var mapData = MapDataMgr.I;
            if (mapData == null)
            {
                UIDelegate.I.ShowCommonTips("地图管理器未初始化");
                return;
            }

            string area = mapData.SwitchNextArea();
            if (area == MapArea.MidgardName)
                UIDelegate.I.ShowCommonTips("已切换到内城");
            else if (area == MapArea.OpenWorldName)
                UIDelegate.I.ShowCommonTips("已切换到世界地图");
            else
                UIDelegate.I.ShowCommonTips("场景里没有可切换的地图区域");

            // 切换区域后旧的建造控制器已销毁，下拉框回到「建造」状态。
            ResetBuildDropdown();
        }

        // ---------------------------------------------------------------- 建造下拉框

        /// <summary>
        /// 运行时实例化公共 Dropdown 预制体，并用 MidgardBuildingCfg 的建筑名填充。
        /// 选项 0 是「建造」（未选择建筑 / 取消建造），后面依次是建筑名字。
        /// </summary>
        private void EnsureBuildDropdown()
        {
            if (_buildDropdown != null || _buildDropdownLoading)
                return;

            _buildDropdownLoading = true;
            var handle = FW.ResMgr.InstantiateAsync(BuildDropdownAddress);
            handle.Completed += operation =>
            {
                _buildDropdownLoading = false;

                if (operation.Status != AsyncOperationStatus.Succeeded || operation.Result == null)
                {
                    Debug.LogError("[UIMain] 建造下拉框加载失败: " + BuildDropdownAddress);
                    FW.ResMgr.Release(operation);
                    return;
                }

                _buildDropdownHandle = operation;
                var go = operation.Result;
                go.name = "BuildDropdown";
                go.transform.SetParent(transform, false);

                var rect = go.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(-150f, -370f);
                    rect.sizeDelta = new Vector2(280f, 50f);
                }

                _buildDropdown = go.GetComponent<TMP_Dropdown>();
                if (_buildDropdown == null)
                {
                    Debug.LogError("[UIMain] Dropdown 预制体上没有 TMP_Dropdown 组件");
                    return;
                }

                CreateBuildPlaceholder(_buildDropdown);
                BuildDropdownOptions();
                _buildDropdown.onValueChanged.AddListener(OnBuildDropdownChanged);
            };
        }

        /// <summary>
        /// 没选建筑时下拉框显示「建造」。TMP_Dropdown 的占位文字只在 value == -1 时显示，
        /// 这样下拉列表里可以只放 MidgardBuildingCfg 的建筑名字。
        /// </summary>
        private static void CreateBuildPlaceholder(TMP_Dropdown dropdown)
        {
            var caption = dropdown.captionText;
            if (caption == null)
                return;

            var placeholder = UnityEngine.Object.Instantiate(caption, caption.transform.parent);
            placeholder.name = "Placeholder";
            placeholder.text = "建造";
            placeholder.raycastTarget = false;

            var captionRect = caption.rectTransform;
            var rect = placeholder.rectTransform;
            rect.anchorMin = captionRect.anchorMin;
            rect.anchorMax = captionRect.anchorMax;
            rect.pivot = captionRect.pivot;
            rect.anchoredPosition = captionRect.anchoredPosition;
            rect.sizeDelta = captionRect.sizeDelta;

            dropdown.placeholder = placeholder;
        }

        private void BuildDropdownOptions()
        {
            _buildConfigs.Clear();
            var options = new List<string>();

            var table = FW.CfgMgr?.GetTable<TbMidgardBuildingCfg>();
            if (table != null)
            {
                for (int i = 0; i < table.DataList.Count; i++)
                {
                    var config = table.DataList[i];
                    string name = string.IsNullOrEmpty(config.Name)
                        ? "建筑 " + config.Id
                        : config.Name;

                    options.Add(name);
                    _buildConfigs.Add(config);
                }
            }
            else
            {
                Debug.LogWarning("[UIMain] 找不到 MidgardBuildingCfg 配置表");
            }

            _buildDropdown.ClearOptions();
            _buildDropdown.AddOptions(options);
            _buildDropdown.SetValueWithoutNotify(-1);
            _buildDropdown.RefreshShownValue();
        }

        private void OnBuildDropdownChanged(int index)
        {
            if (index < 0 || index >= _buildConfigs.Count)
            {
                MapDataMgr.I?.ActiveBuildCtrl?.ClearSelection();
                return;
            }

            var config = _buildConfigs[index];
            var buildCtrl = MapDataMgr.I?.ActiveBuildCtrl;
            if (buildCtrl == null)
            {
                UIDelegate.I.ShowCommonTips("当前地图不能建造");
                ResetBuildDropdown();
                return;
            }

            buildCtrl.SelectBuilding(config);
        }

        /// <summary>建造被取消（Esc / 右键 / 切换地图）时把下拉框恢复成「建造」。</summary>
        private void Update()
        {
            if (_buildDropdown == null)
                return;

            var buildCtrl = MapDataMgr.I?.ActiveBuildCtrl;
            bool building = buildCtrl != null && buildCtrl.HasSelection;
            if (!building && _buildDropdown.value != -1)
            {
                _buildDropdown.SetValueWithoutNotify(-1);
                _buildDropdown.RefreshShownValue();
            }
        }

        private void ResetBuildDropdown()
        {
            if (_buildDropdown == null)
                return;

            _buildDropdown.SetValueWithoutNotify(-1);
            _buildDropdown.RefreshShownValue();
        }

        private void ReleaseBuildDropdown()
        {
            if (_buildDropdown == null)
                return;

            _buildDropdown.onValueChanged.RemoveListener(OnBuildDropdownChanged);
            _buildDropdown = null;

            if (_buildDropdownHandle.IsValid() && FW.ResMgr != null)
                FW.ResMgr.ReleaseInstance(_buildDropdownHandle);

            _buildDropdownHandle = default;
        }
    }
}
