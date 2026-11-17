using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 编辑器摆放出来的「可建造区域」标记，记录矩形的本地尺寸（X/Z）。
    /// 不关联任何配置表；导出时与 Transform 的位置、绕 Y 旋转一起写成矩形基础数据。
    ///
    /// 编辑器里由 MapEditor 拖拽绘制后自动挂上，选中/未选中都会画出绿色半透明矩形，
    /// 方便直接看到哪些地方可以手动放置建筑。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildableAreaMarker : MonoBehaviour
    {
        [Tooltip("可建造矩形的尺寸（本地 X/Z，单位米）。")]
        public Vector2 size = new Vector2(10f, 10f);

        [Tooltip("Scene 视图里填充矩形的颜色。")]
        public Color fillColor = new Color(0.2f, 1f, 0.35f, 0.22f);

        [Tooltip("Scene 视图里矩形边框的颜色。")]
        public Color outlineColor = new Color(0.2f, 1f, 0.35f, 1f);

        private void OnDrawGizmos()
        {
            // 运行时的绿色区域由 BuildableAreaMgr 用真实网格绘制，
            // 这里只在编辑模式下画 Gizmos，避免两套表现重叠。
            if (Application.isPlaying)
                return;

            Vector2 areaSize = NormalizedSize();
            var center = new Vector3(0f, 0.02f, 0f);

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.color = fillColor;
            Gizmos.DrawCube(center, new Vector3(areaSize.x, 0.02f, areaSize.y));
            Gizmos.color = outlineColor;
            Gizmos.DrawWireCube(center, new Vector3(areaSize.x, 0.02f, areaSize.y));
            Gizmos.matrix = Matrix4x4.identity;
        }

        /// <summary>至少 0.1 米，避免导出或绘制时出现零尺寸矩形。</summary>
        public Vector2 NormalizedSize()
        {
            return new Vector2(
                Mathf.Max(0.1f, Mathf.Abs(size.x)),
                Mathf.Max(0.1f, Mathf.Abs(size.y)));
        }
    }
}
