
    float2 uvSplat[_LAYER_COUNT];
    float4 mask[_LAYER_COUNT];
    #ifdef TRIPLANAR
        float2 uvSplat_front[_LAYER_COUNT], uvSplat_side[_LAYER_COUNT];
        float4 mask_front[_LAYER_COUNT], mask_side[_LAYER_COUNT];
    #endif
    #ifdef _TERRAIN_DISTANCEBLEND
        float2 distantUV[_LAYER_COUNT];
        float4 dMask[_LAYER_COUNT];
        #ifdef TRIPLANAR
            float2 distantUV_front[_LAYER_COUNT], distantUV_side[_LAYER_COUNT];
            float4 dMask_front[_LAYER_COUNT], dMask_side[_LAYER_COUNT];
        #endif
    #endif

    #ifdef INTERRA_OBJECT 
        _Smoothness0 = _TerrainSmoothness.x;    _Metallic0 = _TerrainMetallic.x;    _NormalScale0 = _TerrainNormalScale.x;
        _Smoothness1 = _TerrainSmoothness.y;    _Metallic1 = _TerrainMetallic.y;    _NormalScale1 = _TerrainNormalScale.y;
        #ifndef _LAYERS_TWO
            _Smoothness2 = _TerrainSmoothness.z;    _Metallic2 = _TerrainMetallic.z;    _NormalScale2 = _TerrainNormalScale.z;
            _Smoothness3 = _TerrainSmoothness.w;    _Metallic3 = _TerrainMetallic.w;    _NormalScale3 = _TerrainNormalScale.w;
            #ifdef _TERRAIN_8_LAYERS
                _Smoothness4 = _TerrainSmoothness1.x;   _Metallic4 = _TerrainMetallic1.x;   _NormalScale4 = _TerrainNormalScale1.x;
                _Smoothness5 = _TerrainSmoothness1.y;   _Metallic5 = _TerrainMetallic1.y;   _NormalScale5 = _TerrainNormalScale1.y;
                _Smoothness6 = _TerrainSmoothness1.z;   _Metallic6 = _TerrainMetallic1.z;   _NormalScale6 = _TerrainNormalScale1.z;
                _Smoothness7 = _TerrainSmoothness1.w;   _Metallic7 = _TerrainMetallic1.w;   _NormalScale7 = _TerrainNormalScale1.w;
            #endif
        #endif          
    #endif

    //-------------------- MIP MAP LOD ------------------------- 
    #if defined(PARALLAX) || defined(TESSELLATION_ON)
        float lod = smoothstep(_MipMapFade.x, _MipMapFade.y, (distance(worldPos, _WorldSpaceCameraPos)));       
    #endif

    //====================================================================================
    //--------------------------------- SPLAT MAP CONTROL --------------------------------
    //====================================================================================
    float4 blendMask[2];
    blendMask[0] = 0;
    blendMask[1] = 0;

    #ifdef INTERRA_OBJECT
        float2 splatBaseUV = (worldPos.xz - _TerrainPosition.xz) * (1 / _TerrainSize.xz);

        #ifndef _LAYERS_ONE     
            float2 splatMapUV = (splatBaseUV * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy; 

            #ifndef TESSELLATION_SAMPLING
                blendMask[0] = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatMapUV);
            #else
                blendMask[0] = SAMPLE_TEXTURE2D_LOD(_Control, sampler_Control, splatMapUV, 0);
            #endif
            #ifdef _TERRAIN_8_LAYERS
                #ifndef TESSELLATION_SAMPLING
                    blendMask[1] = SAMPLE_TEXTURE2D(_Control1, sampler_Control, splatMapUV);
                #else
                    blendMask[1] = SAMPLE_TEXTURE2D_LOD(_Control1, sampler_Control, splatMapUV, 0);
                #endif
            #endif 
        #else
            blendMask[0] = float4(1, 0, 0, 0);
            blendMask[1] = float4(0, 0, 0, 0);
        #endif 
    #else
        float2 blendUV0 = (splatBaseUV.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;                      
        #ifndef TESSELLATION_SAMPLING
            blendMask[0] = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, blendUV0);
        #else
            blendMask[0] = SAMPLE_TEXTURE2D_LOD(_Control0, sampler_Control0, blendUV0, 0);
        #endif

        #ifdef _TERRAIN_8_LAYERS
            #ifndef TESSELLATION_SAMPLING
                blendMask[1] = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, blendUV0);
            #else
                blendMask[1] = SAMPLE_TEXTURE2D_LOD(_Control1, sampler_Control0, blendUV0, 0);
            #endif           
        #endif
    #endif

    float2 tintUV = splatBaseUV * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw;
    float3 tint = SAMPLE_TEXTURE2D(_TerrainColorTintTexture, sampler_TerrainColorTintTexture, tintUV).rgb;
 
    #if defined(INTERRA_OBJECT) || defined(TRIPLANAR) || defined(_TERRAIN_TRIPLANAR_ONE)
        float3 flipUV = worldNormal.rgb < 0 ? -1 : 1;
        float3  weights = abs(worldNormal.rgb);
        weights = pow(weights, _TriplanarSharpness);
        weights = weights / (weights.x + weights.y + weights.z);

        #ifdef INTERRA_OBJECT            
            TriplanarOneToAllSteep(blendMask, (1 - terrainNormals.w));
        #else
            TriplanarOneToAllSteep(blendMask, (1 - weights.y));
        #endif
    #endif  

    #if defined(_LAYERS_TWO)
            blendMask[0].r = _ControlNumber == 0 ? blendMask[0].r : _ControlNumber == 1 ? blendMask[0].g : _ControlNumber == 2 ? blendMask[0].b : blendMask[0].a;
            blendMask[0].g = 1 - blendMask[0].r;
    #endif

    #if defined(_TERRAIN_BLEND_HEIGHT) && !defined(_TERRAIN_BASEMAP_GEN) && !defined(_LAYERS_ONE) 
       float4 splatControlSum = blendMask[0] + blendMask[1];
       blendMask[0] = (splatControlSum.r + splatControlSum.g + splatControlSum.b + splatControlSum.a == 0.0f ? 1 : blendMask[0]); //this is preventing the black area when more than one pass
    #endif

    #if defined(_TERRAIN_BASEMAP_GEN_TRIPLANAR) || defined(_LAYERS_ONE) 
        blendMask[0] = float4(1, 0, 0, 0);
        blendMask[1] = float4(0, 0, 0, 0);
    #endif

    #if defined(_TERRAIN_DISTANCEBLEND)
        float4 dBlendMask[2];
        dBlendMask[0] = blendMask[0];
        dBlendMask[1] = blendMask[1];
    #endif

    //================================================================================
    //-------------------------------------- UVs -------------------------------------
    //================================================================================
    #ifdef INTERRA_OBJECT
        float2 mainUV = (mUV.xy * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw);
        float2 mainParallaxOffset = 0;
        #ifdef _OBJECT_PARALLAX
            float3x3 objectToTangent = float3x3(worldTangent.xyz, worldBitangent.xyz, worldNormal.xyz);
            mainParallaxOffset = ParallaxOffset(_MaskMap, sampler_MaskMap, _ParallaxSteps, _ParallaxHeight, mainUV, mul(objectToTangent, worldViewDir), _ParallaxAffineSteps, _MipMapLevel + (lod * (log2(max(_MaskMap_TexelSize.z, _MaskMap_TexelSize.w)) + 1)));
            mainUV += mainParallaxOffset;
        #endif
        #ifndef TESSELLATION_SAMPLING
            float2 detailUV = (mUV.xy * _DetailMap_ST.xy + _DetailMap_ST.zw);
            float4 objectAlbedo = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, mainUV) * _BaseColor;
        
            #ifndef _OBJECT_TRIPLANAR            
                _SteepDistortion = worldNormal.y > 0.5 ? 0 : (1 - worldNormal.y) * _SteepDistortion;
                _SteepDistortion *= objectAlbedo.r;
            #else            
                _SteepDistortion = 0;
            #endif
        #else
            _SteepDistortion = 0;
        #endif
        float3 positionOffset = worldPos - _TerrainPosition;

        #ifndef TRIPLANAR
            UvSplat(uvSplat, positionOffset);
        #else
            float offsetZ = _DisableOffsetY == 1 ? -flipUV.z * worldPos.y : heightOffset * -flipUV.z + (worldPos.z);
            float offsetX = _DisableOffsetY == 1 ? -flipUV.x * worldPos.y : heightOffset * -flipUV.x + (worldPos.x);
              
            offsetZ -= _TerrainPosition.z;
            offsetX -= _TerrainPosition.x;

            UvSplat(uvSplat, uvSplat_front, uvSplat_side, positionOffset, offsetZ, offsetX, flipUV);
        #endif
    #else
        #ifndef TRIPLANAR
            UvSplat(uvSplat, splatBaseUV.xy);
        #else
            UvSplat(uvSplat, uvSplat_front, uvSplat_side, worldPos, splatBaseUV.xy);
        #endif
    #endif          

    //-------------------- PARALLAX OFFSET -------------------------                  
    #if defined(_TERRAIN_PARALLAX) && !defined(TESSELLATION_ON)
        ParallaxUV(uvSplat, tangentViewDirTerrain, lod);
    #endif

    //--------------------- DISTANCE UV ------------------------
    #ifdef _TERRAIN_DISTANCEBLEND
        DistantUV(distantUV, uvSplat);
        #ifdef TRIPLANAR
            #ifdef _TERRAIN_TRIPLANAR_ONE
                distantUV_front[0] = uvSplat_front[0] * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale;
                distantUV_side[0] = uvSplat_side[0] * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale;
            #else
                DistantUV(distantUV_front, uvSplat_front);
                DistantUV(distantUV_side, uvSplat_side);
            #endif  
        #endif
    #endif




