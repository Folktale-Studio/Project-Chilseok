using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Chilseok.Rendering
{
    public sealed class DitherRenderFeature : ScriptableRendererFeature
    {
        [SerializeField]
        [Tooltip("먹·한지 디더에 사용하는 전체 화면 셰이더.")]
        private Shader _shader;

        [SerializeField]
        [Tooltip("게임과 시네마틱에서 함께 사용하는 디더 설정.")]
        private DitherSettings _settings;

        private Material _material;
        private DitherPass _pass;

        public override void Create()
        {
            CoreUtils.Destroy(_material);
            _material = _shader != null ? CoreUtils.CreateEngineMaterial(_shader) : null;
            _pass = _material != null && _settings != null ? new DitherPass(_material, _settings) : null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass != null && renderingData.cameraData.cameraType == CameraType.Game &&
                renderingData.cameraData.resolveFinalTarget)
            {
                renderer.EnqueuePass(_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
            _pass = null;
        }

        private sealed class DitherPass : ScriptableRenderPass
        {
            private static readonly int ControlsId = Shader.PropertyToID("_DitherControls");
            private static readonly int MatrixSizeId = Shader.PropertyToID("_BayerSize");
            private static readonly int InkId = Shader.PropertyToID("_DitherInk");
            private static readonly int PaperId = Shader.PropertyToID("_DitherPaper");
            private static readonly Color32 Ink = new Color32(0x1D, 0x1A, 0x17, 0xFF);
            private static readonly Color32 Paper = new Color32(0xE8, 0xDF, 0xC8, 0xFF);

            private readonly Material _material;
            private readonly DitherSettings _settings;
            private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();

            public DitherPass(Material material, DitherSettings settings)
            {
                _material = material;
                _settings = settings;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                // 현 카메라 색을 읽으려면 화면 출력 대신 중간 컬러 텍스처가 필요하다.
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var source = resources.activeColorTexture;
                if (resources.isActiveTargetBackBuffer || !source.IsValid())
                {
                    return;
                }

                var linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                var outputDescriptor = renderGraph.GetTextureDesc(source);
                outputDescriptor.name = "Chilseok Dither Output";
                outputDescriptor.clearBuffer = false;
                outputDescriptor.format = linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                outputDescriptor.msaaSamples = MSAASamples.None;
                outputDescriptor.bindTextureMS = false;
                outputDescriptor.filterMode = FilterMode.Point;
                var output = renderGraph.CreateTexture(outputDescriptor);

                var size = _settings.InternalResolution;
                var lowDescriptor = outputDescriptor;
                lowDescriptor.name = "Chilseok Dither Grid";
                lowDescriptor.sizeMode = TextureSizeMode.Explicit;
                lowDescriptor.width = size.x;
                lowDescriptor.height = size.y;
                lowDescriptor.useDynamicScale = false;
                lowDescriptor.useDynamicScaleExplicit = false;
                var grid = renderGraph.CreateTexture(lowDescriptor);

                _properties.SetVector(ControlsId, new Vector4(_settings.Brightness, _settings.Dissolve,
                    _settings.Invert ? 1f : 0f, _settings.Boiling));
                _properties.SetFloat(MatrixSizeId, _settings.BayerSize);
                _properties.SetVector(InkId, ToRenderColor(Ink, linear));
                _properties.SetVector(PaperId, ToRenderColor(Paper, linear));

                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, grid, _material, 0, _properties,
                    RenderGraphUtils.FullScreenGeometryType.ProceduralTriangle);
                renderGraph.AddBlitPass(parameters, passName: "Chilseok 1-bit Dither");
                // 점을 보존하기 위해 디더가 끝난 격자는 최근접으로 확대한다.
                renderGraph.AddBlitPass(grid, output, Vector2.one, Vector2.zero,
                    filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest, passName: "Chilseok Point Upscale");
                resources.cameraColor = output;
            }

            private static Vector4 ToRenderColor(Color color, bool linear)
            {
                var value = linear ? color.linear : color;
                return new Vector4(value.r, value.g, value.b, 1f);
            }
        }
    }
}
