#define _TERRAIN_BASEMAP

TEXTURE2D(_MainTex);
TEXTURE2D(_MetallicTex);
TEXTURE2D(_TriplanarTex);
TEXTURE2D(_Triplanar_MetallicAO); 

SAMPLER(sampler_MainTex);
SAMPLER(sampler_TriplanarTex);

float4 _MainTex_ST;


#include "InTerra_Functions.hlsl"

void TriplanarBase(in out half4 baseMap, half4 front, half4 side, float3 weights, float2 splat, half firstToAllSteep)
{
    baseMap = firstToAllSteep == 1 ? (baseMap * weights.y + front * weights.z + side * weights.x) 
                                   : (baseMap * saturate(weights.y + (1 - splat.g))) + (((front * weights.z) + (side * weights.x)) * (splat.r));
}


void InTerraTerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData, float3 positionWS, float3 normalWS, float3 tangentViewDir)
{
    float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    float4 metallic_AO_splat01 = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MainTex, uv);
    float4 triplanarTex = SAMPLE_TEXTURE2D(_TriplanarTex, sampler_TriplanarTex, uv);

    #ifdef _TERRAIN_TRIPLANAR_ONE
        float3  weights = abs(normalWS);
        weights = pow(weights, _TriplanarSharpness);
        weights = weights / (weights.x + weights.y + weights.z);

        float2 frontUV = TerrainFrontUV(positionWS, _MainTex_ST, uv);
        float2 sideUV = TerrainSideUV(positionWS, _MainTex_ST, uv);

        half4 cFront = SAMPLE_TEXTURE2D(_TriplanarTex, sampler_TriplanarTex, frontUV);
        half4 cSide = SAMPLE_TEXTURE2D(_TriplanarTex, sampler_TriplanarTex, sideUV);
        TriplanarBase(mainTex, cFront, cSide, weights, metallic_AO_splat01.b, _TriplanarOneToAllSteep);

        float3 tint = SAMPLE_TEXTURE2D(_TerrainColorTintTexture, sampler_TerrainColorTintTexture, uv * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw).rgb;
        mainTex.rgb = lerp(mainTex.rgb, ((mainTex.rgb) * (tint)), _TerrainColorTintStrenght).rgb;

        half4 mAoFront = SAMPLE_TEXTURE2D(_Triplanar_MetallicAO, sampler_TriplanarTex, frontUV);
        half4 mAoSide = SAMPLE_TEXTURE2D(_Triplanar_MetallicAO, sampler_TriplanarTex, sideUV);
        TriplanarBase(metallic_AO_splat01, mAoFront, mAoSide, weights, metallic_AO_splat01.b, _TriplanarOneToAllSteep);       
    #endif

    surfaceData.albedo = mainTex.rgb;
    surfaceData.normalData = 0;
    surfaceData.smoothness = mainTex.a;
    surfaceData.metallic = metallic_AO_splat01.r;
    surfaceData.ao = metallic_AO_splat01.g;
}

void TerrainLitDebug(float2 uv, inout float3 baseColor)
{
#ifdef DEBUG_DISPLAY
    baseColor = GetTextureDataDebug(_DebugMipMapMode, uv, _MainTex, _MainTex_TexelSize, _MainTex_MipInfo, baseColor);
#endif
}
