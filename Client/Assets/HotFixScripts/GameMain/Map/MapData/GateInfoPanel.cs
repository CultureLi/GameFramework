using Google.Protobuf.WellKnownTypes;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 关卡信息/申请/审批面板。原型阶段使用 IMGUI，后续可以替换成项目正式的 UGUI 界面。
    /// </summary>
    public sealed class GateInfoPanel : MonoBehaviour
    {
        private PathfindingMgr _mgr;
        private GateInfoAck _info;
        private string _message;

        public static bool IsOpen { get; private set; }

        public void Bind(PathfindingMgr mgr)
        {
            _mgr = mgr;
        }

        public void Show(GateInfoAck info)
        {
            _info = info;
            _message = info.Message;
            IsOpen = true;
        }

        public void Hide()
        {
            _info = null;
            _message = null;
            IsOpen = false;
        }

        public void ApplyStatus(int status, string message)
        {
            if (_info != null)
                _info.MyStatus = status;

            _message = message;
        }

        public void SetMessage(string message)
        {
            _message = message;
        }

        public void ApplySync(GateAccessSync sync)
        {
            if (_info == null || _info.InstanceId != sync.InstanceId)
                return;

            _info.OwnerPlayerId = sync.OwnerPlayerId;
            _info.IsOwner = sync.MyStatus == GateStatus.Owner;
            _info.MyStatus = sync.MyStatus;
            _info.AllowedPlayers.Clear();
            _info.PendingRequests.Clear();

            foreach (var player in sync.AllowedPlayers)
                _info.AllowedPlayers.Add(player);

            foreach (var player in sync.PendingRequests)
                _info.PendingRequests.Add(player);

            if (!string.IsNullOrEmpty(sync.Message))
                _message = sync.Message;
        }

        private void OnDestroy()
        {
            IsOpen = false;
        }

        private void OnGUI()
        {
            if (!IsOpen || _info == null)
                return;

            const float width = 420f;
            float height = 170f + (_info.AllowedPlayers.Count + _info.PendingRequests.Count) * 30f;
            height = Mathf.Min(height, Screen.height - 120f);

            var rect = new Rect(Screen.width - width - 24f, 70f, width, height);
            GUI.Box(rect, "关卡信息", GUI.skin.window);

            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 28f, rect.width - 24f, rect.height - 40f));
            GUILayout.Label($"关卡: {_info.InstanceId}");
            GUILayout.Label($"归属人: {(_info.OwnerPlayerId == 0 ? "无" : _info.OwnerPlayerId.ToString())}");
            GUILayout.Space(6f);

            if (_info.MyStatus == GateStatus.Owner)
            {
                DrawOwner();
            }
            else
            {
                DrawVisitor();
            }

            if (!string.IsNullOrEmpty(_message))
            {
                GUILayout.Space(6f);
                GUILayout.Label(_message);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("关闭"))
                Hide();

            GUILayout.EndArea();

            if (UnityEngine.Event.current.type == EventType.KeyDown && UnityEngine.Event.current.keyCode == KeyCode.Escape)
            {
                Hide();
                UnityEngine.Event.current.Use();
            }
        }

        private void DrawOwner()
        {
            GUILayout.Label("待审批申请:");
            if (_info.PendingRequests.Count == 0)
            {
                GUILayout.Label("  暂无申请");
            }
            else
            {
                foreach (var player in _info.PendingRequests)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Player {player.PlayerId}");
                    if (GUILayout.Button("同意", GUILayout.Width(70f)))
                        _mgr?.ApproveGate(_info.InstanceId, player.PlayerId);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label("可通行玩家:");
            if (_info.AllowedPlayers.Count == 0)
            {
                GUILayout.Label("  暂无（归属人默认可通行）");
            }
            else
            {
                foreach (var player in _info.AllowedPlayers)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"  Player {player.PlayerId}");
                    if (GUILayout.Button("移除", GUILayout.Width(70f)))
                        _mgr?.RemoveGateAccess(_info.InstanceId, player.PlayerId);
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawVisitor()
        {
            if (_info.MyStatus == GateStatus.Unowned)
            {
                GUILayout.Label("该关卡暂未归属，当前禁止所有人通行。");
                if (GUILayout.Button("认领关卡"))
                    _mgr?.ClaimGate(_info.InstanceId);
                return;
            }

            switch (_info.MyStatus)
            {
                case GateStatus.Pending:
                    GUILayout.Label("已申请，等待归属人处理。");
                    break;

                case GateStatus.Approved:
                    GUILayout.Label("你已获得该关卡的通行权。");
                    break;

                case GateStatus.Rejected:
                    GUILayout.Label("你的申请/通行权已被拒绝或移除。");
                    if (GUILayout.Button("申请通行"))
                        _mgr?.ApplyGate(_info.InstanceId);
                    break;

                default:
                    GUILayout.Label("你还没有该关卡的通行权。");
                    if (GUILayout.Button("申请通行"))
                        _mgr?.ApplyGate(_info.InstanceId);
                    break;
            }
        }
    }
}
