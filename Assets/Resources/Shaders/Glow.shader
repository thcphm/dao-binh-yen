// Vat tu phat sang khong chiu anh sang (lua, tan lua, quang sang, khoi): mau dinh sRGB x _BaseColor.
// _SrcBlend/_DstBlend: One/One = cong sang (additive), SrcAlpha/OneMinusSrcAlpha = trong suot thuong.
// _Radial = 1: alpha giam dan tu tam UV ra mep (quang sang duoi dong lua, ban web: CanvasTexture gradient).
Shader "SereneIsle/Glow"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Radial ("Quang tron theo UV", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Radial;
                half _SrcBlend;
                half _DstBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = half4(SRGBToLinear(i.color.rgb), i.color.a);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = i.color * _BaseColor;
                if (_Radial > 0)
                {
                    half r = saturate(1 - length(i.uv * 2 - 1));
                    c.a *= lerp(1, r * r, _Radial);
                }
                // cong sang: nhan alpha vao mau de "trong suot" dung nghia
                c.rgb *= (_DstBlend == 1 ? c.a : 1);
                return c;
            }
            ENDHLSL
        }
    }
}
