using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Framework
{
    public class GaussianBlurPass : ScriptableRenderPass
    {
        private GaussianBlurFeature _feature;
        private Material _material;
        private RenderTargetIdentifier _source;


        private RTHandle _tempRT1;
        private RTHandle _tempRT2;

        private int _downSample;
        private int _iteration;

        private RenderTexture _targetRT;
        private int _stepID = Shader.PropertyToID("_step");

        public GaussianBlurPass(GaussianBlurFeature feature, int downSample, int iteration)
        {
            this._feature = feature;
            this._downSample = downSample;
            this._iteration = iteration;

            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.width /= _downSample;
            desc.height /= _downSample;

            RenderingUtils.ReAllocateIfNeeded(ref _tempRT1, desc, FilterMode.Bilinear, name: "_GaussianBlurRT1");
            RenderingUtils.ReAllocateIfNeeded(ref _tempRT2, desc, FilterMode.Bilinear, name: "_GaussianBlurRT2");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this._source = source;
        }
        
        public void Blur(RenderTexture targetRT, Material material)
        {
            _targetRT = targetRT;
            _material = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_targetRT != null)
            {
                CommandBuffer cmd = CommandBufferPool.Get("GaussianBlurPass");

                // DownSample
                _material.SetFloat(_stepID, 2);
                cmd.Blit(_source, _tempRT1, _material);

                for (int i = 1; i <= _iteration; i++)
                {
                    _material.SetFloat(_stepID, i * 1.5f);
                    cmd.Blit(_tempRT1, _tempRT2, _material);
                    cmd.Blit(_tempRT2, _tempRT1, _material);
                }

                // 回写到屏幕（或你自己的 RT）
                cmd.Blit(_tempRT1, _targetRT);

                context.ExecuteCommandBuffer(cmd);

                _targetRT = null;
                CommandBufferPool.Release(cmd);
                _feature.SetActive(false);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            _tempRT1?.Release();
            _tempRT2?.Release();
        }
    }
}
