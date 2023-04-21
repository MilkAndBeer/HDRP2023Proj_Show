
#ifndef TESSELLATION_ON
    #include "InTerra_Functions.hlsl"
#endif

#ifdef INTERRA_OBJECT
    void SplatmapMix_float(float heightOffset, float3 tangentViewDirTerrain, float3 worldViewDir, float3 worldNormal, float3 worldTangent, float3 worldBitangent, float3 worldPos, float4 terrainNormals, float4 mUV, out float3 albedo, out float3 mixedNormal, out float smoothness, out float metallic, out float occlusion)
#else
    void SplatmapMix(float2 splatBaseUV, float3 worldNormal, float3 tangentViewDirTerrain, float3 worldPos, out float3 mixedAlbedo, out float smoothness, out float metallic, out float occlusion, inout float3 mixedNormal)
#endif
{
    float4 mixedDiffuse;
    float3 wTangent;
    float3 wBTangent;
    #include "InTerra_SplatMapControl.hlsl"

    //====================================================================================
    //-----------------------------------  MASK MAPS  ------------------------------------
    //====================================================================================
    SampleMask(mask, uvSplat, blendMask);
    #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
        #ifdef _TERRAIN_TRIPLANAR_ONE
            SampleMaskTOL(mask_front, mask, uvSplat_front);
            SampleMaskTOL(mask_side, mask, uvSplat_side);
        #else
            SampleMask(mask_front, uvSplat_front, blendMask);
            SampleMask(mask_side, uvSplat_side, blendMask);
        #endif 
        MaskWeight(mask, mask_front, mask_side, blendMask, weights, _HeightTransition);
    #endif
    #ifdef _TERRAIN_DISTANCEBLEND		
        SampleMask(dMask, distantUV, dBlendMask);
        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            #ifdef _TERRAIN_TRIPLANAR_ONE
                SampleMaskTOL(dMask_front, dMask, distantUV_front);
                SampleMaskTOL(dMask_side, dMask, distantUV_side);
            #else
                SampleMask(dMask_front, distantUV_front, dBlendMask);
                SampleMask(dMask_side, distantUV_side, dBlendMask);
            #endif
        MaskWeight(dMask, dMask_front, dMask_side, dBlendMask, weights, _Distance_HeightTransition);
        #endif
    #endif             

    //========================================================================================
    //------------------------------ HEIGHT MAP SPLAT BLENDINGS ------------------------------
    //========================================================================================
    #if defined(_TERRAIN_BLEND_HEIGHT) && !defined(_LAYERS_ONE) && !defined(TERRAIN_SPLAT_ADDPASS)
        HeightBlend(mask, blendMask, _HeightTransition);
        #ifdef _TERRAIN_DISTANCEBLEND
            HeightBlend(dMask, dBlendMask, _Distance_HeightTransition);
        #endif
    #endif 

    //========================================================================================
    //-------------------------------  ALBEDO, SMOOTHNESS & NORMAL ---------------------------
    //========================================================================================
    SampleSplat(uvSplat, blendMask, mask, mixedDiffuse, mixedNormal);
    #ifdef INTERRA_OBJECT
        wTangent = worldTangent;
        wBTangent = worldBitangent;

        mixedNormal = WorldTangent(wTangent, wBTangent, mixedNormal);
    #endif
        
    #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
        float4 frontDiffuse;
        float3 frontNormal;
        float4 sideDiffuse;
        float3 sideNormal;

        #ifdef _TERRAIN_TRIPLANAR_ONE
            SampleSplatTOL(frontDiffuse, frontNormal, mixedDiffuse, mixedNormal, uvSplat_front, blendMask, mask);
            SampleSplatTOL(sideDiffuse, sideNormal, mixedDiffuse, mixedNormal, uvSplat_side, blendMask, mask);
        #else
            SampleSplat(uvSplat_front, blendMask, mask, frontDiffuse, frontNormal);
            SampleSplat(uvSplat_side, blendMask, mask, sideDiffuse, sideNormal);
        #endif 
        mixedDiffuse = (mixedDiffuse * weights.y) + (frontDiffuse * weights.z) + (sideDiffuse * weights.x);
        mixedNormal = TriplanarNormal(mixedNormal, wTangent, wBTangent, frontNormal, sideNormal, weights, flipUV);
    #endif

    #ifdef _TERRAIN_DISTANCEBLEND  
    
        float4 distantDiffuse;   
        float3 distantNormal;

        SampleSplat(distantUV, dBlendMask, dMask, distantDiffuse, distantNormal);
        #ifdef INTERRA_OBJECT
            distantNormal = WorldTangent(wTangent, wBTangent, distantNormal);
        #endif
        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            float4 dFrontDiffuse;
            float3 dFontNormal;
            float4 dSideDiffuse;
            float3 dSideNormal;

            #ifdef _TERRAIN_TRIPLANAR_ONE
                SampleSplatTOL(dFrontDiffuse, dFontNormal, distantDiffuse, distantNormal, distantUV_front, dBlendMask, dMask);
                SampleSplatTOL(dSideDiffuse, dSideNormal, distantDiffuse, distantNormal, distantUV_side, dBlendMask, dMask);
            #else
                SampleSplat(distantUV_front, dBlendMask, dMask, dFrontDiffuse, dFontNormal);
                SampleSplat(distantUV_side, dBlendMask, dMask, dSideDiffuse, dSideNormal);
            #endif
            distantDiffuse = (distantDiffuse * weights.y) + (dFrontDiffuse * weights.z) + (dSideDiffuse * weights.x);
            distantNormal = TriplanarNormal(distantNormal, wTangent, wBTangent, dFontNormal, dSideNormal, weights, flipUV);
        #endif
                
        float dist = smoothstep(_HT_distance.x, _HT_distance.y, (distance(worldPos, _WorldSpaceCameraPos)));
        distantDiffuse = lerp(mixedDiffuse, distantDiffuse, _HT_cover);
        distantNormal = lerp(mixedNormal, distantNormal, _HT_cover);
        #ifdef _TERRAIN_BASEMAP_GEN            
            mixedDiffuse = distantDiffuse;
        #else
            mixedDiffuse = lerp(mixedDiffuse, distantDiffuse, dist); 
            mixedNormal = lerp(mixedNormal, distantNormal, dist);
        #endif        
    #endif

    #if !defined(TRIPLANAR_TINT) 
        mixedDiffuse.rgb = lerp(mixedDiffuse.rgb, (mixedDiffuse.rgb * tint), _TerrainColorTintStrenght).rgb;
    #endif


    //========================================================================================
    //--------------------------------   AMBIENT OCCLUSION   ---------------------------------
    //========================================================================================
    occlusion = 1;
    #if defined(_TERRAIN_MASK_MAPS) || defined(_TERRAIN_NORMAL_IN_MASK)
        occlusion = AmbientOcclusion(mask, blendMask);
        #if defined (_TERRAIN_DISTANCEBLEND)
            float dAo = AmbientOcclusion(dMask, dBlendMask);
            dAo = lerp(occlusion, dAo, _HT_cover);
            occlusion = lerp(occlusion, dAo, dist);
        #endif
    #endif

    //========================================================================================
    //--------------------------------------   METALLIC   ------------------------------------
    //========================================================================================

        metallic = MetallicMask(mask, blendMask);
        #if defined (_TERRAIN_DISTANCEBLEND)
            float dMetallic = MetallicMask(dMask, dBlendMask);
            dMetallic = lerp(metallic, dMetallic, _HT_cover);
            metallic = lerp(metallic, dMetallic, dist);
        #endif


    //=======================================================================================
    //==============================|   OBJECT INTEGRATION   |===============================
    //=======================================================================================
    #ifdef INTERRA_OBJECT	
        float steepWeights = _SteepIntersection == 1 ? saturate(worldNormal.y + _Steepness) : 1;
        float intersect1 = smoothstep(_Intersection.y, _Intersection.x, heightOffset) * steepWeights;
        float intersect2 = smoothstep(_Intersection2.y, _Intersection2.x, heightOffset) * (1 - steepWeights);
        float intersection = intersect1 + intersect2;
        float intersectNormal = smoothstep(_NormIntersect.y, _NormIntersect.x, heightOffset);
         
        float4 objectMask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, mainUV);
        objectMask.rgba = objectMask.rgba * _MaskMapRemapScale.rgba + _MaskMapRemapOffset.rgba;
        objectAlbedo.a = _HasMask == 1 ? objectMask.a : _Smoothness;
        float objectMetallic = _HasMask == 1 ? objectMask.r : _Metallic;
        float objectAo = _HasMask == 1 ? objectMask.g : _Ao;
        float height = objectMask.b;

        float sSum;
        #ifdef _TERRAIN_BLEND_HEIGHT 
            sSum = lerp(HeightSum(mask, blendMask), 1, intersection);
        #else 	
            sSum = 0.5;
        #endif 

        float2 heightIntersect = (1 / (1 * pow(2, float2(((1 - intersection) * height), (intersection * sSum)) * (-(_Sharpness)))) + 1) * 0.5;
        heightIntersect /= (heightIntersect.r + heightIntersect.g);

        #ifdef _OBJECT_DETAIL 
            #ifdef _OBJECT_PARALLAX
                detailUV += mainParallaxOffset * (_DetailMap_ST.xy / _BaseColorMap_ST.xy);
            #endif
            float3 dt = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV).rgb;
            objectAlbedo.rgb = lerp(objectAlbedo.rgb, half(2.0) * dt, _DetailStrenght).rgb;
        #endif
                    
        float3 mainNorm = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, mainUV), _NormalScale);

        // avoid risk of NaN when normalizing.
        mainNorm.z += 1e-5f;
        
        #ifdef _OBJECT_DETAIL            
            float3 mainNormD = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUV), _DetailNormalMapScale);                  
            mainNorm = (lerp(mainNorm, BlendNormalRNM(mainNorm, mainNormD), _DetailStrenght));
        #endif
        mixedDiffuse = lerp(mixedDiffuse, objectAlbedo, heightIntersect.r);


        float3 terrainNormal = (mixedNormal.z * terrainNormals.xyz) + 1e-5f;
        terrainNormal.xy = mixedNormal.xy + terrainNormal.xy;
        mixedNormal = lerp(mixedNormal, terrainNormal, intersectNormal);
        mixedNormal = lerp(mixedNormal, mainNorm, heightIntersect.r);


        metallic = lerp(metallic, objectMetallic, heightIntersect.r);
        occlusion = lerp(occlusion, objectAo, heightIntersect.r);
        albedo = mixedDiffuse.rgb;
    #else
        mixedAlbedo = mixedDiffuse.rgb;
    #endif

    smoothness = mixedDiffuse.a; 
    //=========================================================================================             
}
