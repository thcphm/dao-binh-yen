// Lambert kieu ban web: mau dinh (vertex color sRGB) x texture x _BaseColor, den ban cau + mat troi + bong do URP.
// Dung cho dia hinh (_VertexColorWeight = 1), cay/da/nha (mau dinh), nhan vat va cay Quaternius (texture).
// Them (ban web makeObjMat, dong 408-428):
//   _SWAY          : cay co lay theo gio (dinh cay dung dua, goc dung yen) - ho tro GPU instancing.
//   _OcclusionFade : vat nam giua camera va nhan vat thi thua dan bang luoi diem Bayer 4x4.
//   _ALPHATEST_ON  : la cay dang tam phang co texture cat alpha (Quaternius); di kem _Cull = Off (ve 2 mat).
Shader "SereneIsle/WebLambert"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _VertexColorWeight ("Vertex Color Weight", Range(0, 1)) = 1
        [Toggle(_SWAY)] _Sway ("Lay theo gio", Float) = 0
        _OcclusionFade ("Thua khi che nhan vat", Range(0, 1)) = 0
        _FogScale ("Muc anh huong suong mu", Range(0, 1)) = 1
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Cat alpha (la cay)", Float) = 0
        _Cutoff ("Nguong cat alpha", Range(0, 1)) = .5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        _Desaturate ("Phai mau (vung it suc song)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _VertexColorWeight;
            half _Sway;
            half _OcclusionFade;
            half _FogScale;
            half _AlphaClip;
            half _Cutoff;
            half _Cull;
            half _Desaturate;
        CBUFFER_END

        // gio: dich chuyen trong khong gian vat the, phase theo vi tri goc cua tung cay.
        // _SI_Wind (thoi tiet, toan cuc, mac dinh 0): bao thi lay nhanh va manh gap ~3.
        float _SI_Wind;
        float3 SI_ApplySway(float3 posOS)
        {
        #if defined(_SWAY)
            float3 ip = TransformObjectToWorld(float3(0, 0, 0));
            float hg = max(0, posOS.y - .5);
            float wd = saturate(_SI_Wind);
            float w = 1 + wd * 2.2;
            float ph = _Time.y * 1.5 + ip.x * .37 + ip.z * .29;
            // gio giat (nhanh) chi co khi co gio; doi gio khong lam nhay pha
            float gust = sin(_Time.y * 4.3 + ip.x * .5 + ip.z * .3) * wd;
            posOS.x += (sin(ph) + sin(ph * 2.3) * .3 + gust + wd * .8) * .03 * w * hg * hg;
            posOS.z += (cos(ph * .8) + gust * .5) * .025 * w * hg * hg;
        #endif
            return posOS;
        }

        void SI_AlphaClip(float2 uv)
        {
        #if defined(_ALPHATEST_ON)
            clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a - _Cutoff);
        #endif
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ _SWAY
            #pragma multi_compile_local _ _ALPHATEST_ON
            // den diem: lua trai, duoc (ban web: lightSrc + PointLight ban dem)
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WebLighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
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
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 texc = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
            #if defined(_ALPHATEST_ON)
                clip(texc.a - _Cutoff);
            #endif
                SI_OcclusionClip(i.positionWS, i.positionCS.xy, _OcclusionFade);

                // mat sau cua la (ve 2 mat): lat phap tuyen de khong bi den
                float3 n = normalize(i.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                // texture sRGB duoc GPU doi sang linear khi lay mau -> doi nguoc ve sRGB de tinh nhu ban web
                half3 albedo = i.color * SI_ToSRGB(texc.rgb) * SI_ToSRGB(_BaseColor.rgb);
                // vung it suc song (dao vua qua bao): mau nhat, nga nau xam
                half lum = dot(albedo, half3(.3, .59, .11));
                albedo = lerp(albedo, lum * half3(1.06, .97, .82), _Desaturate);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light sun = GetMainLight(shadowCoord);
                half ndl = saturate(dot(n, sun.direction));
                half3 light = SI_Hemi(n) + sun.color * (ndl * sun.shadowAttenuation);

            #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightCount)
                    Light pl = GetAdditionalLight(lightIndex, i.positionWS);
                    // nua Lambert: mat quay ngang van sang nhe, nhu anh lua hat len
                    half w = saturate(dot(n, pl.direction) * .75 + .25);
                    light += pl.color * (pl.distanceAttenuation * w);
                LIGHT_LOOP_END
            #endif

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
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_local _ _SWAY
            #pragma multi_compile_local _ _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
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
                o.positionCS = positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { SI_AlphaClip(i.uv); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ _SWAY
            #pragma multi_compile_local _ _ALPHATEST_ON

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(SI_ApplySway(input.positionOS.xyz));
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { SI_AlphaClip(i.uv); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ _SWAY
            #pragma multi_compile_local _ _ALPHATEST_ON

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                o.positionCS = TransformObjectToHClip(SI_ApplySway(input.positionOS.xyz));
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { SI_AlphaClip(i.uv); return half4(normalize(i.normalWS), 0); }
            ENDHLSL
        }
    }

    FallBack Off
}
