// Vom troi: chan troi = mau suong mu (dia hinh xa hoa vao troi), dinh troi dam hon, dia mat troi + quang sang.
Shader "SereneIsle/Sky"
{
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "RenderPipeline" = "UniversalPipeline" "PreviewType" = "Skybox" }
        ZWrite Off
        Cull Front

        Pass
        {
            Name "Sky"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WebLighting.hlsl"

            float4 _SI_SkyTop;   // rgb sRGB: mau dinh troi
            float4 _SI_SunDisc;  // rgb sRGB mau mat troi, w = do sang (0 ban dem)

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dirWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
            #if UNITY_REVERSED_Z
                o.positionCS.z = o.positionCS.w * 1e-6; // luon nam o xa nhat
            #else
                o.positionCS.z = o.positionCS.w * (1 - 1e-6);
            #endif
                o.dirWS = ws - GetCameraPositionWS();
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dirWS);
                float h = saturate(d.y);
                half3 horizon = _SI_FogColor.rgb;
                half3 top = _SI_SkyTop.w > .5 ? _SI_SkyTop.rgb : horizon * .8;
                half3 c = lerp(horizon, top, pow(h, .32));
                // mat troi
                Light sun = GetMainLight();
                float s = dot(d, sun.direction);
                half3 sunCol = _SI_SunDisc.rgb;
                c += sunCol * (smoothstep(.9986, .9993, s) * .9 + pow(saturate(s), 48) * .22) * _SI_SunDisc.w;
                return half4(SI_Output(c), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
