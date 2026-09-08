using System;
using UnityEngine;

namespace GameMain
{
    /// <summary>
    /// 相机相对地图矩形的约束方式。
    /// </summary>
    public enum CameraBoundType
    {
        /// <summary>
        /// 内切 —— 可见地面始终保持在地图范围内。相机在屏幕上将要出现地图外的空白之前就会被挡住，
        /// 这是开放世界的常规模式。
        /// </summary>
        Inbound,

        /// <summary>
        /// 外切 —— 整张地图始终保持在可见地面范围内。用于需要把地图完整框进屏幕的场合（全局俯瞰），
        /// 此时允许看到地图边界之外，但绝不允许地图滑出视野。
        /// </summary>
        Exbound,
    }

    /// <summary>
    /// 由单一归一化缩放值（0 = 最近 / 拉近，1 = 最远 / 拉远）驱动相机高度，做法与参考工程的
    /// 地图相机一致：缩放只有一个输入量，画面沿着一条美术配好的路径变化。
    ///
    /// <see cref="heightCurve"/> 是相机高度的唯一来源，曲线值直接就是米数，
    /// 所以高度范围完全在曲线里编辑，组件上不再有 minHeight / maxHeight。
    ///
    /// 视场角和俯角刻意<i>不</i>由缩放驱动 —— 本工程要求它们全程保持不变，
    /// 视场角就用 Camera 上配置的值，俯角固定 45°。
    /// </summary>
    [Serializable]
    public class CameraZoomCurve
    {
        /// <summary>曲线缺失或为空时退回的高度，避免相机掉到地面上。</summary>
        const float kFallbackHeight = 50f;

        [Tooltip("缩放值 → 相机相对地面的高度（米）。这是相机高度的唯一来源：想改高度范围就改这条曲线的首尾关键帧。默认 50 → 300。")]
        public AnimationCurve heightCurve = AnimationCurve.Linear(0f, 50f, 1f, 300f);

        /// <summary>取缩放值对应的相机高度（米）。</summary>
        public float EvaluateHeight(float zoom01)
        {
            if (heightCurve == null || heightCurve.length == 0) return kFallbackHeight;
            return Mathf.Max(1f, heightCurve.Evaluate(zoom01));
        }

        /// <summary>
        /// 高度曲线的取值范围，用来把"每格滚轮多少米"换算成归一化缩放值上的步长。
        /// 只扫关键帧而不逐点采样：关键帧之间切线造成的少量过冲对灵敏度换算无关紧要，
        /// 而且用索引器取关键帧不会像 <c>keys</c> 属性那样每次都分配数组。
        /// </summary>
        public void GetHeightRange(out float minHeight, out float maxHeight)
        {
            minHeight = maxHeight = kFallbackHeight;
            if (heightCurve == null || heightCurve.length == 0) return;

            minHeight = maxHeight = heightCurve[0].value;
            for (int i = 1; i < heightCurve.length; i++)
            {
                float v = heightCurve[i].value;
                if (v < minHeight) minHeight = v;
                if (v > maxHeight) maxHeight = v;
            }
        }

        /// <summary>
        /// 反查：给定高度，找出最接近它的缩放值。采样求最近点而不是解析求逆，
        /// 这样任意形状的曲线（包括非单调的）都能用。只在初始化时调用一次，开销可以忽略。
        /// </summary>
        public float FindZoomForHeight(float height)
        {
            if (heightCurve == null || heightCurve.length == 0) return 0f;

            const int kSamples = 64;
            float best = 0f;
            float bestError = float.MaxValue;
            for (int i = 0; i <= kSamples; i++)
            {
                float zoom = i / (float)kSamples;
                float error = Mathf.Abs(heightCurve.Evaluate(zoom) - height);
                if (error < bestError)
                {
                    bestError = error;
                    best = zoom;
                }
            }
            return best;
        }
    }
}
