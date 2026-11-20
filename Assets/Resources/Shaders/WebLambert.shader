// Lambert kieu ban web: mau dinh (vertex color sRGB) x _BaseColor, den ban cau + mat troi + bong do URP.
// Dung cho dia hinh (_VertexColorWeight = 1), cay/da/nha (mau dinh) va nhan vat (mau don, _VertexColorWeight = 0).
// Them (ban web makeObjMat, dong 408-428):
//   _SWAY          : cay co lay theo gio (dinh cay dung dua, goc dung yen) - ho tro GPU instancing.
//   _OcclusionFade : vat nam giua camera va nhan vat thi thua dan bang luoi diem Bayer 4x4.
Shader "SereneIsle/WebLambert"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _VertexColorWeight ("Vertex Color Weight", Range(0, 1)) = 1
        [Toggle(_SWAY)] _Sway ("Lay theo gio", Float) = 0
        _OcclusionFade ("Thua khi che nhan vat", Range(0, 1)) = 0
        _FogScale ("Muc anh huong suong mu", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _VertexColorWeight;
            half _Sway;
            half _OcclusionFade;
            half _FogScale;
        CBUFFER_END

        // gio: dich chuyen trong khong gian vat the, phase theo vi tri goc cua tung cay
        float3 SI_ApplySway(float3 posOS)
        {
        #if defined(_SWAY)
            float3 ip = TransformObjectToWorld(float3(0, 0, 0));
            float hg = max(0, posOS.y - .5);
            float ph = _Time.y * 1.5 + ip.x * .37 + ip.z * .29;
            posOS.x += (sin(ph) + sin(ph * 2.3) * .3) * .03 * hg * hg;
            posOS.z += cos(ph * .8) * .025 * hg * hg;
        #endif
            return posOS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ _SWAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WebLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half3  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs p = GetVertexPositionInputs(SI_ApplySway(input.positionOS.xyz));
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.color = lerp(half3(1, 1, 1), input.color.rgb, _VertexColorWeight);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                SI_OcclusionClip(i.positionWS, i.positionCS.xy, _OcclusionFade);

                float3 n = normalize(i.normalWS);
                half3 albedo = i.color * SI_ToSRGB(_BaseColor.rgb);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light sun = GetMainLight(shadowCoord);
                half ndl = saturate(dot(n, sun.direction));
                half3 light = SI_Hemi(n) + sun.color * (ndl * sun.shadowAttenuation);

                half3 lit = albedo * light;
                half3 c = lerp(lit, SI_Fog(lit, i.positionWS), _FogScale);
                return half4(SI_Output(c), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_local _ _SWAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(SI_ApplySway(input.positionOS.xyz));
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ _SWAY

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(SI_ApplySway(input.positionOS.xyz));
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ _SWAY

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(SI_ApplySway(input.positionOS.xyz));
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0); }
            ENDHLSL
        }
    }

    FallBack Off
}
