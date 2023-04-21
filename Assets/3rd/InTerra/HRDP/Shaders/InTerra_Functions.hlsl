#if defined(_TERRAIN_MASK_MAPS) || defined(_TERRAIN_NORMAL_IN_MASK) || defined(_TERRAIN_BLEND_HEIGHT) || defined(_TERRAIN_PARALLAX)
    #define TERRAIN_MASK
#endif

#if defined (_OBJECT_TRIPLANAR) || defined (_TERRAIN_TRIPLANAR_ONE)  || defined (_TERRAIN_TRIPLANAR)
    #define TRIPLANAR
#endif

#if defined(_TERRAIN_PARALLAX) || defined(_OBJECT_PARALLAX) 
    #define PARALLAX
#endif

#ifdef INTERRA_OBJECT
    #ifdef _LAYERS_ONE
        #define _LAYER_COUNT 1
    #else
        #ifdef _LAYERS_TWO
            #define _LAYER_COUNT 2
        #else
            #if defined(_LAYERS_EIGHT) 
                #define _TERRAIN_8_LAYERS
                #define _LAYER_COUNT 8
            #else
                #define _LAYER_COUNT 4
            #endif
        #endif
    #endif

    float _Smoothness0, _Smoothness1, _Metallic0, _Metallic1, _NormalScale0, _NormalScale1;
    #ifndef _LAYERS_TWO
        float _Smoothness2, _Smoothness3, _Metallic2, _Metallic3, _NormalScale2, _NormalScale3;
        #ifdef _TERRAIN_8_LAYERS
            float _Smoothness4, _Smoothness5, _Smoothness6, _Smoothness7;
            float _Metallic4, _Metallic5, _Metallic6, _Metallic7;
            float _NormalScale4, _NormalScale5, _NormalScale6, _NormalScale7;
        #endif
    #endif
#else
    #if defined(_TERRAIN_NORMAL_IN_MASK)
        #undef _NORMALMAP
        #define _NORMALMAP
    #endif

    #include "InTerra_LayersDeclaration.hlsl"

    half4 _HT_distance;
    float _HT_distance_scale, _HT_cover;
    half _Distance_Height_blending, _Distance_HeightTransition, _TriplanarSharpness;
    half _ControlNumber;
    half _ParallaxAffineStepsTerrain;
    float _TerrainColorTintStrenght;
    float4 _TerrainColorTintTexture_ST;
    
    half _TriplanarOneToAllSteep;
    float3 _TerrainSizeXZPosY;

    float4 _MipMapFade;
    float _MipMapLevel;

    TEXTURE2D(_TerrainColorTintTexture);    SAMPLER(sampler_TerrainColorTintTexture);

    float2 TerrainFrontUV(float3 wPos, float4 splatUV, float2 tc)
    {
        return  float2(tc.x, (wPos.y - _TerrainSizeXZPosY.z) * (splatUV.y / _TerrainSizeXZPosY.y) + splatUV.w);
    }

    float2 TerrainSideUV(float3 wPos, float4 splatUV, float2 tc)
    {
        return  float2(tc.y, (wPos.y - _TerrainSizeXZPosY.z) * (splatUV.x / _TerrainSizeXZPosY.x) + splatUV.z);
    }
#endif



#ifndef _TERRAIN_BASEMAP
    //==========================================================================================
    //======================================   FUNCTIONS   =====================================
    //==========================================================================================
    float2 ObjectFrontUV(float posOffset, float4 splatUV, float offsetZ)
    {
        return  float2((posOffset + splatUV.z) / splatUV.x, (offsetZ + splatUV.w) / splatUV.y);
    }

    float2 ObjectSideUV(float posOffset, float4 splatUV, float offsetX)
    {
        return  float2((offsetX + splatUV.z) / splatUV.x, (posOffset + splatUV.w) / splatUV.y);
    }

    float3 WorldTangent(float3 wTangent, float3 wBTangent, float3 mixedNormal)
    {
        mixedNormal.xy = mul(float2x2(wTangent.xz, wBTangent.xz), mixedNormal.xy);
        return  half3(mixedNormal);
    }

    float2 HeightBlendTwoTextures(float2 splat, float2 heights, float sharpness)
    {
        splat *= (1 / (1 * pow(2, heights * (-(sharpness)))) + 1) * 0.5;
        splat /= (splat.r + splat.g);

        return  splat;
    }

    void TriplanarOneToAllSteep(in out float4 blendMask[2], float weightY)
    {   
        if (_TriplanarOneToAllSteep == 1)
        {
            blendMask[0] = float4(saturate(blendMask[0].r + weightY), saturate((blendMask[0].gba) - weightY));
            blendMask[1] = float4(saturate((blendMask[1].rgba) - weightY)); 
        }
    }  

    float3 TriplanarNormal(float3 normal, float3 tangent, float3 bTangent, float3 normal_front, float3 normal_side, float3 weights, half3 flipUV)
    {
        #ifdef INTERRA_OBJECT
            normal_front.y *= -flipUV.z;
            normal_front.xy = mul(float2x2(tangent.xy, bTangent.xy), normal_front.xy);

            normal_side.x *= -flipUV.x;
            normal_side.xy = mul(float2x2(tangent.yz, bTangent.yz), normal_side.xy);
        #else
             normal_front.y *= -flipUV.z;
             normal_side.xy = normal_side.yx; //this is needed because the uv was rotated
             normal_side.x *= -flipUV.x;
        #endif

        return half3 (normal * weights.y + normal_front * weights.z + normal_side * weights.x);
    }

    #ifndef _TERRAIN_BASEMAP_GEN
        #if defined (PARALLAX)
            float GetParallaxHeight(texture2D maskT, sampler maskS, float2 uv, float lod, float2 offset)
            {
                return SAMPLE_TEXTURE2D_LOD(maskT, maskS, float2(uv + offset), lod).b; 
            }

            //this function is based on Parallax Occlusion Mapping from Shader Graph URP
            float2 ParallaxOffset(texture2D maskT, sampler maskS, int numSteps, float amplitude, float2 uv, float3 tangentViewDir, float affineSteps, float lod)
            {    
                float2 offset = 0;

                if (numSteps > 0)
                {
                    float3 viewDir = float3(tangentViewDir.xy * amplitude * -0.01, tangentViewDir.z);
                    float stepSize = (1.0 / numSteps);

                    float2 texOffsetPerStep = stepSize * viewDir.xy;
                

                    // Do a first step before the loop to init all value correctly
                    float2 texOffsetCurrent = float2(0.0, 0.0); 
                    float prevHeight = GetParallaxHeight(maskT, maskS, uv, lod, texOffsetCurrent);
                    texOffsetCurrent += texOffsetPerStep;
                    float currHeight = GetParallaxHeight(maskT, maskS, uv, lod, texOffsetCurrent);
                    float rayHeight = 1.0 - stepSize; // Start at top less one sample

                    for (int stepIndex = 0; stepIndex < numSteps; ++stepIndex)
                    {
                        // Have we found a height below our ray height ? then we have an intersection
                        if (currHeight > rayHeight)
                            break; // end the loop

                        prevHeight = currHeight;
                        rayHeight -= stepSize;
                        texOffsetCurrent += texOffsetPerStep;

                        currHeight = GetParallaxHeight(maskT, maskS, uv, lod, texOffsetCurrent);
                    }

                    if (affineSteps <= 1)
                    {
                        float delta0 = currHeight - rayHeight;
                        float delta1 = (rayHeight + stepSize) - prevHeight;
                        float ratio = delta0 / (delta0 + delta1);
                        offset = texOffsetCurrent - ratio * texOffsetPerStep;

                        currHeight = GetParallaxHeight(maskT, maskS, uv, lod, texOffsetCurrent);
                    }
                    else
                    {
                        float pt0 = rayHeight + stepSize;
                        float pt1 = rayHeight;
                        float delta0 = pt0 - prevHeight;
                        float delta1 = pt1 - currHeight;

                        float delta;

                       // Secant method to affine the search
                        // Ref: Faster Relief Mapping Using the Secant Method - Eric Risser
                       for (int i = 0; i < affineSteps; ++i)
                        {
                            // intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
                            float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
                            // Retrieve offset require to find this intersectionHeight
                            offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;

                            currHeight = GetParallaxHeight(maskT, maskS, uv, lod, offset);

                            delta = intersectionHeight - currHeight;

                            if (abs(delta) <= 0.01)
                                break;

                            // intersectionHeight < currHeight => new lower bounds
                            if (delta < 0.0)
                            {
                                delta1 = delta;
                                pt1 = intersectionHeight;
                            }
                            else
                            {
                                delta0 = delta;
                                pt0 = intersectionHeight;
                            }
                        }
                    }
                }  
                return offset;
            }
        #endif
    #endif

    #if defined(_NORMALMAP) && !defined(_TERRAIN_NORMAL_IN_MASK) 
        float3 UnpackNormals(float4 packednormal, float normalScale)
        {    
            #ifdef SURFACE_GRADIENT
                #ifdef UNITY_NO_DXT5nm
                    return float3(UnpackDerivativeNormalRGB(packednormal, normalScale), 0);
                #else
                    return float3(UnpackDerivativeNormalRGorAG(packednormal, normalScale), 0);
                #endif
            #else
                #ifdef UNITY_NO_DXT5nm
                    return UnpackNormalRGB(packednormal, normalScale);
                #else
                    return UnpackNormalmapRGorAG(packednormal, normalScale);
                #endif
            #endif
        }
    #elif defined(_TERRAIN_NORMAL_IN_MASK) 
        float3 UnpackNormalGAWithScale(float4 packednormal, float scale, half hasMask)
        {
            UNITY_BRANCH if (hasMask > 0)
            {
                #ifdef SURFACE_GRADIENT
                    return float3(UnpackDerivativeNormalAG(packednormal, scale), 0);         
                #else
                    return UnpackNormalAG(packednormal, scale);
                #endif
            }
            else
            {
                return float3(0, 0, 0);
            }
        }
    #endif

    #if defined(_NORMALMAP) && !defined(_TERRAIN_NORMAL_IN_MASK) 
        #define SampleNormals(i) UnpackNormals(SAMPLE_TEXTURE2D(_Normal##i, sampler_Splat0, uv[i]), _NormalScale##i);
    #elif defined(_TERRAIN_NORMAL_IN_MASK) 
        #define SampleNormals(i) float3(UnpackNormalGAWithScale(mask[i], _NormalScale##i, _LayerHasMask##i))
    #else
        #define SampleNormals(i) float3(0, 0, 0)
    #endif

    float3 SmoothMaskOrAlbedo(half mask, half albedo, float hasMask, float smoothness)
    {
        UNITY_BRANCH if (hasMask > 0)
        {                                                                               
            albedo = mask;
        }
        else                                                                      
        {                                                                           
            albedo *= smoothness;
        }
        return albedo;
    }

    #ifdef _TERRAIN_MASK_MAPS
        #define Smoothness(i) SmoothMaskOrAlbedo(mask[i].a, albedo[i].a, _LayerHasMask##i, _Smoothness##i)
    #else
        #define Smoothness(i) albedo[i].a *= _Smoothness##i
    #endif

    #ifdef INTERRA_OBJECT
        #define UV(i) (posOffset.xz + _SplatUV##i.zw  + _SteepDistortion) / _SplatUV##i.xy;
        #ifdef PARALLAX                                                      
            #define fUV(i) ObjectFrontUV(posOffset.x, _SplatUV##i, offsetZ + (_DiffuseRemapScale##i.w * 0.004 * _SplatUV##i.x) * -flip.z);
            #define sUV(i) ObjectSideUV(posOffset.z, _SplatUV##i, offsetX + (_DiffuseRemapScale##i.w * 0.004 * _SplatUV##i.y) * -flip.x);
        #else   
            #if defined(TESSELLATION_ON)
                #define fUV(i) ObjectFrontUV(posOffset.x, _SplatUV##i, offsetZ + (-_DiffuseRemapOffset##i.y * 0.005 - _TerrainTessOffset) * -flip.z);
                #define sUV(i) ObjectSideUV(posOffset.z, _SplatUV##i, offsetX + (-_DiffuseRemapOffset##i.y * 0.005 - _TerrainTessOffset) * -flip.x);

            #else
                #define fUV(i) ObjectFrontUV(posOffset.x, _SplatUV##i, offsetZ);
                #define sUV(i) ObjectSideUV(posOffset.z, _SplatUV##i, offsetX);

            #endif
        #endif
    #else
        #define UV(i) splatBaseUV * _Splat##i##_ST.xy + _Splat##i##_ST.zw;
        #define fUV(i) TerrainFrontUV(worldPos, _Splat##i##_ST, uvSplat[i]);    
        #define sUV(i) TerrainSideUV(worldPos, _Splat##i##_ST, uvSplat[i]); 
    #endif

    float4 RemapMasks(float4 mask, float4 remapScale, float4 remapOffset)
    {
        #ifdef _TERRAIN_NORMAL_IN_MASK
            mask.rb = mask.rb * remapScale.gb + remapOffset.gb;
            return mask;
        #else
            return mask * remapScale + remapOffset;
        #endif

    }

    #ifdef TERRAIN_MASK
        #define Mask(i) SAMPLE_TEXTURE2D(_Mask##i, sampler_Splat0, uv[i]);

        #ifdef _TERRAIN_NORMAL_IN_MASK
            #define RemapMask(i) mask[i] * float4(_MaskMapRemapScale##i.g, 1, _MaskMapRemapScale##i.b, 1)  \
                                        + float4(_MaskMapRemapOffset##i.g, 0, _MaskMapRemapOffset##i.b, 0);
        #else
            #define RemapMask(i) mask[i] * _MaskMapRemapScale##i + _MaskMapRemapOffset##i;
        #endif
    #else
        #define Mask(i) float4(_Metallic##i, 1, 0.5, 0);
        #define RemapMask(i) mask[i];
    #endif

    void SampleMask(out float4 mask[_LAYER_COUNT], float2 uv[_LAYER_COUNT], float4 blendMask[2])
    {
        #define SampleMasks(i, blendMask)                                       \
            UNITY_BRANCH if (blendMask > 0 && _LayerHasMask##i  > 0 )           \
            {                                                                   \
                mask[i] = Mask(i);                                              \
                mask[i] = RemapMask(i);                                         \
            }                                                                   \
            else                                                                \
            {                                                                   \
                mask[i] = float4(_Metallic##i, 1, 0.5, 0);                      \
            }                                                                   \

        SampleMasks(0, blendMask[0].r);
        #ifndef _LAYERS_ONE
            SampleMasks(1, blendMask[0].g);
            #ifndef _LAYERS_TWO
                SampleMasks(2, blendMask[0].b);
                SampleMasks(3, blendMask[0].a);
                #ifdef _TERRAIN_8_LAYERS
                    SampleMasks(4, blendMask[1].r);
                    SampleMasks(5, blendMask[1].g);
                    SampleMasks(6, blendMask[1].b);
                    SampleMasks(7, blendMask[1].a);
                #endif
            #endif
        #endif
    #undef SampleMasks
    }

    #ifdef INTERRA_OBJECT
        #define DiffuseRemap(i) float4(_DiffuseRemapScale##i.xyzw)
    #else
        #define DiffuseRemap(i) float4(_DiffuseRemapScale##i.xyzw + _DiffuseRemapOffset##i.xyzw)
    #endif

    void SampleSplat(float2 uv[_LAYER_COUNT], float4 blendMask[2], inout float4 mask[_LAYER_COUNT], out float4 mixAlbedo, out float3 mixNormal)
    {
        float4 albedo[_LAYER_COUNT];
        float3 normal[_LAYER_COUNT];

        mixAlbedo = 0;
        mixNormal = 0;

        #define Samples(i,  blendMask)                                                  \
        UNITY_BRANCH if (blendMask > 0)                                                 \
        {                                                                               \
            albedo[i] = SAMPLE_TEXTURE2D(_Splat##i, sampler_Splat0, uv[i]);             \
            albedo[i].rgb *= DiffuseRemap(i).xyz;                                       \
            albedo[i].a = Smoothness(i);                                                \
            normal[i] = SampleNormals(i);                                               \
            mixAlbedo += albedo[i] * blendMask;                                         \
            mixNormal += normal[i] * blendMask;                                         \
        }                                                                               \
        else                                                                            \
        {                                                                               \
            albedo[i] = float4(0, 0, 0, 0);                                             \
            normal[i] = float3(0, 1, 0);                                                \
        }                                                                               \


        Samples(0, blendMask[0].r);
        #ifndef _LAYERS_ONE
            Samples(1, blendMask[0].g);
            #ifndef _LAYERS_TWO
                Samples(2, blendMask[0].b);
                Samples(3, blendMask[0].a);
                #ifdef _TERRAIN_8_LAYERS
                    Samples(4, blendMask[1].r);
                    Samples(5, blendMask[1].g);
                    Samples(6, blendMask[1].b);
                    Samples(7, blendMask[1].a);
                #endif
            #endif
        #endif
        #undef Samples       
    }

    #ifdef INTERRA_OBJECT
        #ifndef TRIPLANAR
            void UvSplat(out float2 uvSplat[_LAYER_COUNT], float3 posOffset)
        #else
            void UvSplat(out float2 uvSplat[_LAYER_COUNT], out float2 uvFront[_LAYER_COUNT], out float2 uvSide[_LAYER_COUNT], float3 posOffset, float offsetZ, float offsetX, float3 flip)
        #endif
    #else
        #ifndef TRIPLANAR
            void UvSplat(out float2 uvSplat[_LAYER_COUNT], float2 splatBaseUV)
        #else
            void UvSplat(out float2 uvSplat[_LAYER_COUNT], out float2 uvFront[_LAYER_COUNT], out float2 uvSide[_LAYER_COUNT], float3 worldPos, float2 splatBaseUV)
        #endif
    #endif
    {
        #ifndef TRIPLANAR
            #define SplatUV(i)                              \
            uvSplat[i] = UV(i);       
        #else
            #define SplatUV(i)                              \
            uvSplat[i] = UV(i);                             \
            uvFront[i] = fUV(i);                            \
            uvSide[i] = sUV(i);                             \

        #endif

        SplatUV(0);
        #ifndef _LAYERS_ONE
            SplatUV(1);
            #ifndef _LAYERS_TWO
                SplatUV(2);
                SplatUV(3);
                #ifdef _TERRAIN_8_LAYERS
                    SplatUV(4);
                    SplatUV(5);
                    SplatUV(6);
                    SplatUV(7);
                #endif
            #endif    
        #endif
    }

    void DistantUV(out float2 distantUV[_LAYER_COUNT], float2 uvSplat[_LAYER_COUNT])
    {
        #define uvDistant(i)                                                                \
        distantUV[i] = uvSplat[i] * (_DiffuseRemapOffset##i.r + 1) * _HT_distance_scale;    \
        
        uvDistant(0);
        #ifndef _LAYERS_ONE
            uvDistant(1);
            #ifndef _LAYERS_TWO
                uvDistant(2);
                uvDistant(3);
                #ifdef _TERRAIN_8_LAYERS
                    uvDistant(4);
                    uvDistant(5);
                    uvDistant(6);
                    uvDistant(7);
                #endif
            #endif    
        #endif
    }

    #ifdef PARALLAX
        void ParallaxUV(inout float2 uv[_LAYER_COUNT], float3 tangentViewDir, float lod)
        {
        #define uvParallax(i)                                                                                                                                       \
        uv[i] += ParallaxOffset(_Mask##i, sampler_Splat0, _DiffuseRemapOffset##i.w, DiffuseRemap(i).w, uv[i], tangentViewDir, _ParallaxAffineStepsTerrain, _MipMapLevel +  (lod * (log2(max(_Mask##i##_TexelSize.z, _Mask##i##_TexelSize.w)) + 1)));   \
            
        uvParallax(0);
        #ifndef _LAYERS_ONE
            uvParallax(1);
            #ifndef _LAYERS_TWO
                uvParallax(2);
                uvParallax(3);
                #ifdef _TERRAIN_8_LAYERS
                    uvParallax(4);
                    uvParallax(5);
                    uvParallax(6);
                    uvParallax(7);
                #endif
            #endif
        #endif
        }
    #endif

    void MaskWeight(inout float4 mask[_LAYER_COUNT], float4 mask_front[_LAYER_COUNT], float4 mask_side[_LAYER_COUNT], float4 blendMask[2], inout float3 triplanarWeights, float heightBlendingSharpness)
    {
        float splatWeight[_LAYER_COUNT];
        float3 heights = 0;

        splatWeight[0] = blendMask[0].x;
        #ifndef _LAYERS_ONE
            splatWeight[1] = blendMask[0].y;
            #ifndef _LAYERS_TWO
                splatWeight[2] = blendMask[0].z;
                splatWeight[3] = blendMask[0].w;
                #ifdef _TERRAIN_8_LAYERS
                    splatWeight[4] = blendMask[1].x;
                    splatWeight[5] = blendMask[1].y;
                    splatWeight[6] = blendMask[1].z;
                    splatWeight[7] = blendMask[1].w;
                #endif
            #endif
        #endif
       
        for (int i = 0; i < _LAYER_COUNT; ++i)
        {
            mask[i] = (mask[i] * triplanarWeights.y) + (mask_front[i] * triplanarWeights.z) + (mask_side[i] * triplanarWeights.x);
            
        #if defined(_TERRAIN_BLEND_HEIGHT)
            heights += float3(mask_side[i].b, mask[i].b, mask_front[i].b) * splatWeight[i];
        #endif
        }

        #if defined(_TERRAIN_BLEND_HEIGHT)
            triplanarWeights.rgb *= (1 / (1 * pow(2, (heights + triplanarWeights) * (-(heightBlendingSharpness)))) + 1) * 0.5;
            triplanarWeights.rgb /= (triplanarWeights.r + triplanarWeights.g + triplanarWeights.b);
        #endif
    }

    #ifdef TERRAIN_MASK
        float AmbientOcclusion(float4 mask[_LAYER_COUNT], float4 blendMask[2])
        {
            float occlusion[_LAYER_COUNT];
            #ifdef _TERRAIN_NORMAL_IN_MASK
                UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
                {
                    occlusion[i] = mask[i].r;
                }
            #else
                UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
                {
                    occlusion[i] = mask[i].g;
                }
            #endif  

            float ao = 0;

            ao = occlusion[0] * blendMask[0].r;
            #ifndef _LAYERS_ONE
                ao += occlusion[1] * blendMask[0].g;
                #ifndef _LAYERS_TWO
                    ao += occlusion[2] * blendMask[0].b
                        + occlusion[3] * blendMask[0].a;
                    #ifdef _TERRAIN_8_LAYERS
                        ao += occlusion[4] * blendMask[1].r
                            + occlusion[5] * blendMask[1].g
                            + occlusion[6] * blendMask[1].b
                            + occlusion[7] * blendMask[1].a;
                    #endif
                #endif
            #endif
            return  ao;
        }
    #endif

    #ifndef _TERRAIN_MASK_MAPS
        #define Metallic(i, blendMask) _Metallic##i * blendMask;
    #else
        #define Metallic(i, blendMask) mask[i].r * blendMask;
    #endif

    float MetallicMask(float4 mask[_LAYER_COUNT], float4 blendMask[2])
    {    
        float metallic = 0;

        #define Metallics(i,  blendMask)                        \
        UNITY_BRANCH if (blendMask > 0)                         \
        {                                                       \
            metallic += Metallic(i, blendMask);                 \
        }

        Metallics(0, blendMask[0].r);
        #ifndef _LAYERS_ONE
            Metallics(1, blendMask[0].g);
            #ifndef _LAYERS_TWO
                Metallics(2, blendMask[0].b);
                Metallics(3, blendMask[0].a);
                #ifdef _TERRAIN_8_LAYERS
                    Metallics(4, blendMask[1].r);
                    Metallics(5, blendMask[1].g);
                    Metallics(6, blendMask[1].b);
                    Metallics(7, blendMask[1].a);
                #endif
            #endif
        #endif

        return metallic;
    } 

    float HeightSum(float4 mask[_LAYER_COUNT], float4 blendMask[2])
    {   
        #ifdef _LAYERS_ONE
            return float(mask[0].b);
        #else
            float heightSum = dot(blendMask[0].rg, float2(mask[0].b, mask[1].b));
            #ifndef _LAYERS_TWO
                heightSum += dot(blendMask[0].ba, float2(mask[2].b, mask[3].b));
                #ifdef _TERRAIN_8_LAYERS
                    heightSum += dot(blendMask[1], float4(mask[4].b, mask[5].b, mask[6].b, mask[7].b));
                #endif
            #endif
            return heightSum;
        #endif
    }

    #ifdef _TERRAIN_BLEND_HEIGHT
        void HeightBlend(float4 mask[_LAYER_COUNT], inout float4 blendMask[2], float sharpness)
        {
            #ifdef _LAYERS_TWO
                float2 height = float2(mask[0].b, mask[1].b);

                blendMask[0].rg *= (1 / (1 * pow(2, (height + blendMask[0].rg) * (-(sharpness)))) + 1) * 0.5;
                blendMask[0].rg /= (blendMask[0].r + blendMask[0].g);
            #else
                float4 height = float4 (mask[0].b, mask[1].b, mask[2].b, mask[3].b);
                blendMask[0].rgba *= (1 / (1 * pow(2, (height + blendMask[0].rgba) * (-(sharpness)))) + 1) * 0.5;
                float heightSum = blendMask[0].r + blendMask[0].g + blendMask[0].b + blendMask[0].a;
                #ifdef _TERRAIN_8_LAYERS  
                    float4 height1 = float4 (mask[4].b, mask[5].b, mask[6].b, mask[7].b);
                    blendMask[1].rgba *= (1 / (1 * pow(2, (height1 + blendMask[1].rgba) * (-(sharpness)))) + 1) * 0.5;
                    heightSum += blendMask[1].r + blendMask[1].g + blendMask[1].b + blendMask[1].a;
                    blendMask[1].rgba /= heightSum;               
                #endif
                blendMask[0].rgba /= heightSum;
            #endif
        }
    #endif


    #ifndef _TERRAIN_BASEMAP_GEN
        void SampleSplatTOL(out float4 mixedAlbedo, out float3 mixedNormal, float4 noTriplanarAlbedo, float3 noTriplanarNormal, float2 uv[_LAYER_COUNT], float4 blendMask[2], float4 mask[_LAYER_COUNT])
        {
            float4 albedo[1];
            float3 normal[1];
            albedo[0] = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uv[0]);
            albedo[0].rgb *= DiffuseRemap(0).rgb;
            albedo[0].a = Smoothness(0);
            normal[0] = SampleNormals(0);

            float noTriplanarBlendMask = (blendMask[0].g + blendMask[0].b + blendMask[0].a
                + blendMask[1].r + blendMask[1].g + blendMask[1].b + blendMask[1].a);

            albedo[0] *= blendMask[0].r;
            normal[0] *= blendMask[0].r;
            noTriplanarAlbedo *= (1 - blendMask[0].r);
            noTriplanarNormal *= (1 - blendMask[0].r);

            mixedAlbedo = albedo[0] + noTriplanarAlbedo;
            mixedNormal = normal[0] + noTriplanarNormal;
        }

        void SampleMaskTOL(out float4 mask[_LAYER_COUNT], float4 noTriplanarMask[_LAYER_COUNT], float2 uv[_LAYER_COUNT])
        {
            mask[0] = Mask(0);                                             
                        
            mask[1] = noTriplanarMask[1];
            #ifndef _LAYERS_TWO
                mask[2] = noTriplanarMask[2];            
                mask[3] = noTriplanarMask[3];
                #ifdef _TERRAIN_8_LAYERS
                    mask[4] = noTriplanarMask[4];
                    mask[5] = noTriplanarMask[5];
                    mask[6] = noTriplanarMask[6];
                    mask[7] = noTriplanarMask[7];
                #endif
            #endif
        }
    #endif
#endif
#ifdef INTERRA_OBJECT
    float ObjectTerrainIntersection(float3 worldNormal, float TerrainHeightOffset, float4 mask[_LAYER_COUNT], float4 blendMask[2], float objectHeightMap)
    {
        float steepWeights = _SteepIntersection == 1 ? saturate(worldNormal.y + _Steepness) : 1;
        float intersect1 = smoothstep(_Intersection.y, _Intersection.x, TerrainHeightOffset) * steepWeights;
        float intersect2 = smoothstep(_Intersection2.y, _Intersection2.x, TerrainHeightOffset) * (1 - steepWeights);
        float intersection = intersect1 + intersect2;

        float heightSum;
        #ifdef _TERRAIN_BLEND_HEIGHT 
            heightSum = lerp(HeightSum(mask, blendMask), 1, intersection);
        #else 	
            heightSum = 0.5;
        #endif 

        float sharpness;
        #ifdef TESSELLATION_SAMPLING
            sharpness = _Tessellation_Sharpness;
        #else 	
            sharpness = _Sharpness;
        #endif 


        float2 heightIntersect = (1 / (1 * pow(2, float2(((1 - intersection) * objectHeightMap), (intersection * heightSum)) * (-(sharpness)))) + 1) * 0.5;
        heightIntersect /= (heightIntersect.r + heightIntersect.g);
        return heightIntersect.x;
    }
#endif