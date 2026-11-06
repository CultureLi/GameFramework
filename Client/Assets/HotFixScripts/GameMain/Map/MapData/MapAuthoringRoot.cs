using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 编辑器作者场景的根节点标记（挂在 DataEditor 根上）。
    /// 如果作者场景和运行场景是同一个，运行时会把带这个标记的根节点隐藏，
    /// 避免和按 MapData 流式创建的实体重复。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapAuthoringRoot : MonoBehaviour
    {
    }
}
