Shader "BAS_VR/Alpha Screen"
{
    Properties
    {
        [MainTexture] _BaseMap ("Screen Texture", 2D) = "black" {}
        _WindowFill ("Window Fill (RGBA)", Color) = (0.008, 0.025, 0.06, 1)
        [NoScaleOffset] _WindowMask ("Window Fill Mask", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Pass
        {
            Blend One OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_WindowMask);
            SAMPLER(sampler_WindowMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _WindowFill;
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
                half4 ui = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half inside = SAMPLE_TEXTURE2D(_WindowMask, sampler_WindowMask, input.uv).r;
                half fill = (1.0h - ui.a) * _WindowFill.a * inside;
                return half4(ui.rgb + _WindowFill.rgb * fill, ui.a + fill);
            }
            ENDHLSL
        }
    }
}
