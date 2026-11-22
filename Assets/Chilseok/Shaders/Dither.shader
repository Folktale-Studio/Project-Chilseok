Shader "Hidden/Chilseok/Dither"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Chilseok 1-bit Dither"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float4 _DitherControls;
            float4 _DitherInk;
            float4 _DitherPaper;
            float _BayerSize;

            uint Bayer2(uint2 bit)
            {
                return ((bit.x ^ bit.y) << 1) | bit.y;
            }

            float BayerThreshold(uint2 pixel)
            {
                uint rank = 4u * Bayer2(pixel & 1u) + Bayer2((pixel >> 1u) & 1u);
                return _BayerSize > 4.0
                    ? (4u * rank + Bayer2((pixel >> 2u) & 1u) + 0.5) / 64.0
                    : (rank + 0.5) / 16.0;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 source = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord, 0).rgb;
                float brightness = Luminance(max(source, 0.0));
                #if !defined(UNITY_COLORSPACE_GAMMA)
                    brightness = LinearToSRGB(brightness);
                #endif

                // 출력 텍셀 좌표를 사용해야 카메라가 움직여도 Bayer 격자가 화면에 고정된다.
                uint2 pixel = uint2(input.positionCS.xy);
                float bayer = BayerThreshold(pixel);
                float phase = frac(sin(dot(float2(pixel), float2(12.9898, 78.233))) * 43758.5453);
                float boiling = sin((phase + _Time.y) * TWO_PI) * _DitherControls.w * 0.5;
                float threshold = clamp(bayer + boiling, 0.0001, 0.9999);
                float paper = step(threshold, saturate(brightness + _DitherControls.x));
                paper = lerp(paper, 1.0 - paper, _DitherControls.z);
                paper *= step(_DitherControls.y, bayer);
                return lerp(_DitherInk, _DitherPaper, paper);
            }
            ENDHLSL
        }
    }
}
