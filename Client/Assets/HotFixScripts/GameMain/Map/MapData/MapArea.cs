using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// Main 场景中的一个地图区域节点（内城 Midgard / 世界地图 OpenWorld）。
    ///
    /// 区域节点下面挂自己的 <see cref="WorldChunkStreaming"/>，由 <see cref="MapDataMgr"/>
    /// 按需激活并加载对应 &lt;AreaName&gt; 目录下的地图数据。切换时只是显示/隐藏这些节点，
    /// 不做真正的场景切换。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapArea : MonoBehaviour
    {
        public const string MidgardName = "Midgard";
        public const string OpenWorldName = "OpenWorld";

        [Tooltip("区域名，必须与地图数据目录 Assets/AssetBundles/MapData/<区域名> 一致。")]
        public string areaName = MidgardName;

        [Tooltip("该区域的地块流式加载组件；留空时自动从子节点查找。")]
        public WorldChunkStreaming chunkStreaming;

        [Tooltip("切换到该区域时相机注视的地面位置（XZ）。")]
        public Vector2 cameraFocus = new Vector2(500f, 500f);

        [Tooltip("该区域的地图矩形最小角（XZ），切换时同步给相机做边界约束。")]
        public Vector2 mapMinXZ = new Vector2(0f, 0f);

        [Tooltip("该区域的地图矩形最大角（XZ），切换时同步给相机做边界约束。")]
        public Vector2 mapMaxXZ = new Vector2(1000f, 1000f);

        public string AreaName
        {
            get
            {
                if (!string.IsNullOrEmpty(areaName))
                    return areaName;

                var streaming = ChunkStreaming;
                return streaming != null && !string.IsNullOrEmpty(streaming.sceneName)
                    ? streaming.sceneName
                    : name;
            }
        }

        public WorldChunkStreaming ChunkStreaming
        {
            get
            {
                if (chunkStreaming == null)
                    chunkStreaming = GetComponentInChildren<WorldChunkStreaming>(true);

                return chunkStreaming;
            }
        }

        private void Reset()
        {
            chunkStreaming = GetComponentInChildren<WorldChunkStreaming>(true);
            areaName = name;
        }
    }
}
