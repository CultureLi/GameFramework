using System;
using UnityEngine;
using UnityEngine.AI;

namespace GameFramework.Level
{
    /// <summary>
    /// 关卡门。关闭时启用碰撞体和 NavMeshObstacle（carving），打开时禁用，
    /// 这样 NavMeshSurface 烘焙出的山口在关闭时会被动态挖掉，打开后恢复通行。
    ///
    /// 生成器（Editor/Terrain/MazeGateBuilder）会自动创建并挂上这个组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelGate : MonoBehaviour
    {
        public bool open = true;
        public int gateID;

        [Header("Blocking")]
        public Collider blockingCollider;
        public NavMeshObstacle obstacle;

        [Header("Visual")]
        public Renderer gateRenderer;
        public Color openColor = new Color(0.15f, 0.9f, 0.25f, 1f);
        public Color closedColor = new Color(0.9f, 0.15f, 0.12f, 1f);

        public bool IsOpen => open;

        /// <summary>
        /// 手动请求改变关卡状态时触发（gateId, open）。正式的网络管理器订阅它，
        /// 把请求发给服务端，等服务端广播后再调用 SetOpen 应用。
        /// </summary>
        public static event Action<int, bool> GateStateRequested;

        /// <summary>
        /// 应用权威状态（服务端广播或本地无网络时使用）。不会再次触发请求事件。
        /// </summary>
        public void SetOpen(bool value)
        {
            open = value;
            Apply();
        }

        /// <summary>
        /// 手动请求开关。有网络管理器订阅时走服务端；没有订阅者时退化为本地开关。
        /// </summary>
        public void RequestSetOpen(bool value)
        {
            if (gateID <= 0 || GateStateRequested == null)
            {
                SetOpen(value);
                return;
            }

            GateStateRequested.Invoke(gateID, value);
        }

        public void OpenGate() => RequestSetOpen(true);

        public void CloseGate() => RequestSetOpen(false);

        private void Awake()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            if (blockingCollider != null)
                blockingCollider.enabled = !open;

            if (obstacle != null)
                obstacle.enabled = !open;

            // 材质实例只在运行时改，避免编辑器里产生未保存的 material 实例
            if (gateRenderer != null && Application.isPlaying)
            {
                var material = gateRenderer.material;
                var color = open ? openColor : closedColor;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
            }
        }
    }
}
