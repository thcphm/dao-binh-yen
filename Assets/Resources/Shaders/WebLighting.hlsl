// Mo phong cach chieu sang cua ban web (three.js r128, khong tone mapping, khong gamma):
//   mau = albedo_sRGB * (HemisphereLight + sun * NdotL * shadow), roi tron suong mu smoothstep(near, far, viewDepth).
// Tinh toan trong khong gian sRGB, chi doi sang linear o buoc cuoi neu project dung Linear color space.
#ifndef SERENE_WEB_LIGHTING_INCLUDED
#define SERENE_WEB_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// Dat boi IslandEnvironment (Shader.SetGlobalVector -> khong bi doi mau).
float4 _SI_HemiSky;     // rgb = mau troi * cuong do (sRGB), w = 1 neu da duoc dat
float4 _SI_HemiGround;  // rgb = mau dat * cuong do (sRGB)
float4 _SI_FogColor;    // rgb = mau suong mu (sRGB)
float4 _SI_FogParams;   // x = near, y = far, z = 1 bat suong mu

half3 SI_ToSRGB(half3 c)
{
#if defined(UNITY_COLORSPACE_GAMMA)
    return c;
#else
    return LinearToSRGB(c);
#endif
}

half3 SI_Output(half3 c)
{
    c = saturate(c);
#if defined(UNITY_COLORSPACE_GAMMA)
    return c;
#else
    return SRGBToLinear(c);
#endif
}

half3 SI_Hemi(float3 n)
{
    if (_SI_HemiSky.w < 0.5)
        return SI_ToSRGB(max(half3(0, 0, 0), SampleSH(n))); // chua co IslandEnvironment: dung ambient cua Unity
    return lerp(_SI_HemiGround.rgb, _SI_HemiSky.rgb, n.y * 0.5 + 0.5);
}

half3 SI_Fog(half3 c, float3 positionWS)
{
    if (_SI_FogParams.z < 0.5) return c;
    float depth = -TransformWorldToView(positionWS).z;
    float f = smoothstep(_SI_FogParams.x, _SI_FogParams.y, depth);
    return lerp(c, _SI_FogColor.rgb, f);
}

#endif
