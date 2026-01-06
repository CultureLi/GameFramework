using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Framework
{
    public class GaussianBlurFeature : ScriptableRendererFeature
    {
        [Range(1, 4)] public int downSample = 1;
        [Range(1, 4)] public int iteration = 2;

        private Material blurMaterial;
        private GaussianBlurPass blurPass;

        public static GaussianBlurFeature Instance
        {
            get; private set;
        }
        public override void Create()
        {
            blurPass = new GaussianBlurPass(this, downSample, iteration);

            Instance = this;
        }

        // ❌ 不要在这里拿 camera target
        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.camera.gameObject.CompareTag("UICamera"))
                return;
            renderer.EnqueuePass(blurPass);
        }

        // ✅ 正确位置：URP 会保证 RT 已创建
        public override void SetupRenderPasses(
            ScriptableRenderer renderer,
            in RenderingData renderingData)
        {
            blurPass.Setup(renderer.cameraColorTargetHandle.nameID);
        }

        public void Blur(RenderTexture rt, Material mt)
        {
            SetActive(true);
            blurPass.Blur(rt, mt);
        }
    }

}
