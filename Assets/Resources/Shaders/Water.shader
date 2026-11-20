// Mat bien kieu ban web: MeshPhongMaterial flatShading, mau #3ab6cc, opacity .74, shininess 90.
// Song: y = sin(x*.35 + t*1.1)*.05 + cos(z*.42 + t*.9)*.05 (toa do object, giong waterGeo).
Shader "SereneIsle/Water"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (0.2275, 0.7137, 0.8, 0.74)
        _Shininess ("Shininess", Float) = 90
        _SpecStrength ("Specular Strength", Float) = 1
        _WaveAmp ("Wave Amplitude", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WebLighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Shininess;
                float _SpecStrength;
                float _WaveAmp;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 p = input.positionOS.xyz;
                float t = _Time.y;
                p.y += (sin(p.x * 0.35 + t * 1.1) + cos(p.z * 0.42 + t * 0.9)) * _WaveAmp;
                o.positionWS = TransformObjectToWorld(p);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // flat shading: phap tuyen tu dao ham man hinh, luon huong len
                float3 n = normalize(cross(ddx(i.positionWS), ddy(i.positionWS)));
                if (n.y < 0) n = -n;

                Light sun = GetMainLight();
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 h = normalize(sun.direction + v);
                half ndl = saturate(dot(n, sun.direction));
                // Blinn-Phong khong chuan hoa: he so (s/2+1)*0.25 = 11.5 cua three.js lam ca vung rong
                // bi trang xoa khi camera nhin tu tren xuong mat phang lon -> chi giu dom lap lanh nho.
                half spec = pow(saturate(dot(n, h)), _Shininess) * ndl * _SpecStrength;

                half3 albedo = SI_ToSRGB(_BaseColor.rgb);
                half3 c = albedo * (SI_Hemi(n) + sun.color * ndl) + sun.color * spec * 0.6;
                c = SI_Fog(c, i.positionWS);
                return half4(SI_Output(c), _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
