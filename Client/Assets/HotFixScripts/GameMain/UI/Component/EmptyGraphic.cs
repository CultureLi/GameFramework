using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;

namespace GameMain.UI
{
    /// <summary>
    /// 挂载空Graphic组件，响应点击事件
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class EmptyGraphic : MaskableGraphic
    {
        protected EmptyGraphic()
        {
            useLegacyMeshGeneration = false;
        }

        protected sealed override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
