Shader "BAS_VR/Sci-Fi Screen Cutout"
{
    Properties
    {
        [MainTexture] _BaseMap ("Screen Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.75
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Match the outer edge of the frame on the 768 x 384 Canvas.
                float2 pixel = (input.uv - 0.5) * float2(768.0, 384.0);
                float x = abs(pixel.x);
                float top = min(173.0 + clamp(220.0 - x, 0.0, 11.0), 516.0 - x);
                float bottom = max(-174.0, x - 516.0);
                clip(min(365.0 - x, min(top - pixel.y, pixel.y - bottom)) + 1.5);
                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                return half4(color, _Opacity);
            }
            ENDHLSL
        }
    }
}
