//=======================================================================
//----------------------------- InTerra SPLAT BLEND ---------------------
//=======================================================================
#include "InTerra_SplatmapMix.hlsl" 

void InTerraTerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData, float3 positionWS, float3 normalWS, float3 tangentView)
{
    SplatmapMix(uv,
                normalWS,
                tangentView, 
                positionWS,
                surfaceData.albedo,
                surfaceData.smoothness, 
                surfaceData.metallic, 
                surfaceData.ao, 
                surfaceData.normalData);
}
//=======================================================================

void TerrainLitDebug(float2 uv, inout float3 baseColor)
{
#ifdef DEBUG_DISPLAY
    if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_CONTROL)
        baseColor = GetTextureDataDebug(_DebugMipMapMode, uv, _Control0, _Control0_TexelSize, _Control0_MipInfo, baseColor);
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER0)
        baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat0_ST.xy + _Splat0_ST.zw, _Splat0, _Splat0_TexelSize, _Splat0_MipInfo, baseColor);
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER1)
        baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat1_ST.xy + _Splat1_ST.zw, _Splat1, _Splat1_TexelSize, _Splat1_MipInfo, baseColor);
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER2)
        baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat2_ST.xy + _Splat2_ST.zw, _Splat2, _Splat2_TexelSize, _Splat2_MipInfo, baseColor);
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER3)
        baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat3_ST.xy + _Splat3_ST.zw, _Splat3, _Splat3_TexelSize, _Splat3_MipInfo, baseColor);
    #ifdef _TERRAIN_8_LAYERS
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER4)
            baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat4_ST.xy + _Splat4_ST.zw, _Splat4, _Splat4_TexelSize, _Splat4_MipInfo, baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER5)
            baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat5_ST.xy + _Splat5_ST.zw, _Splat5, _Splat5_TexelSize, _Splat5_MipInfo, baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER6)
            baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat6_ST.xy + _Splat6_ST.zw, _Splat6, _Splat6_TexelSize, _Splat6_MipInfo, baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER7)
            baseColor = GetTextureDataDebug(_DebugMipMapMode, uv * _Splat7_ST.xy + _Splat7_ST.zw, _Splat7, _Splat7_TexelSize, _Splat7_MipInfo, baseColor);
    #endif
#endif
}
