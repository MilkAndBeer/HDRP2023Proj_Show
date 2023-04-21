// NOTE! Almost all code in this file and the file InTerra_TerrainLitTemplate is written by Unity and are used because there is no longer the possibility to use the Standard Shader (or contains code for Terrain Instancing etc.) and there would be need to write basically the same Template/LitData for InTerra Shader Functions.

//-------------------------------------------------------------------------------------
// Defines
//-------------------------------------------------------------------------------------

// Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
#define SURFACE_GRADIENT

// Use surface gradient normal mapping as it handle correctly triplanar normal mapping and multiple UVSet
#ifndef UNITY_2022_2_OR_NEWER 
    #define SURFACE_GRADIENT
#else
    #ifndef SHADER_STAGE_RAY_TRACING        
        #define SURFACE_GRADIENT
    #endif
#endif

//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"

#ifndef UNITY_TERRAIN_CB_VARS
    #define UNITY_TERRAIN_CB_VARS
#endif

#ifndef UNITY_TERRAIN_CB_DEBUG_VARS
    #define UNITY_TERRAIN_CB_DEBUG_VARS
#endif

CBUFFER_START(UnityTerrain)
    UNITY_TERRAIN_CB_VARS
#ifdef UNITY_INSTANCING_ENABLED
    float4 _TerrainHeightmapRecipSize;  // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
    float4 _TerrainHeightmapScale;      // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
#endif
#ifdef DEBUG_DISPLAY
    UNITY_TERRAIN_CB_DEBUG_VARS
#endif
#ifdef SCENESELECTIONPASS
    int _ObjectId;
    int _PassValue;
#endif
CBUFFER_END

#ifdef UNITY_INSTANCING_ENABLED
    TEXTURE2D(_TerrainHeightmapTexture);
    TEXTURE2D(_TerrainNormalmapTexture);
    #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
        SAMPLER(sampler_TerrainNormalmapTexture);
    #endif
#endif

#ifdef _ALPHATEST_ON
    TEXTURE2D(_TerrainHolesTexture);
    SAMPLER(sampler_TerrainHolesTexture);
    #ifndef UNITY_2022_2_OR_NEWER 
        void ClipHoles(float2 uv)
        {
            float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r;
            DoAlphaTest(hole, 0.5);
        }
    #endif
#endif



#if !defined(SHADER_STAGE_RAY_TRACING)
    // Vertex height displacement
    #ifdef HAVE_MESH_MODIFICATION

        UNITY_INSTANCING_BUFFER_START(Terrain)
        UNITY_DEFINE_INSTANCED_PROP(float4, _TerrainPatchInstanceData)  // float4(xBase, yBase, skipScale, ~)
        UNITY_INSTANCING_BUFFER_END(Terrain)

        float4 ConstructTerrainTangent(float3 normal, float3 positiveZ)
        {
            // Consider a flat terrain. It should have tangent be (1, 0, 0) and bitangent be (0, 0, 1) as the UV of the terrain grid mesh is a scale of the world XZ position.
            // In CreateTangentToWorld function (in SpaceTransform.hlsl), it is cross(normal, tangent) * sgn for the bitangent vector.
            // It is not true in a left-handed coordinate system for the terrain bitangent, if we provide 1 as the tangent.w. It would produce (0, 0, -1) instead of (0, 0, 1).
            // Also terrain's tangent calculation was wrong in a left handed system because cross((0,0,1), terrainNormalOS) points to the wrong direction as negative X.
            // Therefore all the 4 xyzw components of the tangent needs to be flipped to correct the tangent frame.
            // (See TerrainLitData.hlsl - GetSurfaceAndBuiltinData)
            float3 tangent = cross(normal, positiveZ);
            return float4(tangent, -1);
        }

        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
        {
        #ifdef UNITY_INSTANCING_ENABLED
            float2 patchVertex = input.positionOS.xy;
            float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

            float2 sampleCoords = (patchVertex.xy + instanceData.xy) * instanceData.z; // (xy + float2(xBase,yBase)) * skipScale
            float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(int3(sampleCoords, 0)));

            input.positionOS.xz = sampleCoords * _TerrainHeightmapScale.xz;
            input.positionOS.y = height * _TerrainHeightmapScale.y;

            #ifdef ATTRIBUTES_NEED_NORMAL
                input.normalOS = _TerrainNormalmapTexture.Load(int3(sampleCoords, 0)).rgb * 2 - 1;
            #endif
        
            #if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
                #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
                    input.uv0 = sampleCoords;
                #else
                    input.uv0 = sampleCoords * _TerrainHeightmapRecipSize.zw;
                #endif
            #endif
        #endif

        #ifdef ATTRIBUTES_NEED_TANGENT
            input.tangentOS = ConstructTerrainTangent(input.normalOS, float3(0, 0, 1));
        #endif
            return input;
        }

    #endif // HAVE_MESH_MODIFICATION
#endif // !defined(SHADER_STAGE_RAY_TRACING)

// We don't use emission for terrain
#define _EmissiveColor float3(0,0,0)
#define _AlbedoAffectEmissive 0
#define _EmissiveExposureWeight 0
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitBuiltinData.hlsl"
#undef _EmissiveColor
#undef _AlbedoAffectEmissive
#undef _EmissiveExposureWeight

#ifndef SHADER_STAGE_RAY_TRACING
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitSurfaceData.hlsl"


void TerrainLitDebug(float2 uv, inout float3 baseColor);

void InTerraTerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData, float3 positionWS, float3 normalWS, float3 tangentViewDir);

float3 ConvertToNormalTS(float3 normalData, float3 tangentWS, float3 bitangentWS)
{
#ifdef _NORMALMAP
    #ifdef SURFACE_GRADIENT
        return SurfaceGradientFromTBN(normalData.xy, tangentWS, bitangentWS);
    #else
        return normalData;
    #endif
#else
    #ifdef SURFACE_GRADIENT
        return float3(0.0, 0.0, 0.0); // No gradient
    #else
        return float3(0.0, 0.0, 1.0);
    #endif
#endif
}


#ifndef UNITY_2022_2_OR_NEWER 
    void GetSurfaceAndBuiltinData(inout FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
    {
#else
    void GetSurfaceAndBuiltinData(inout FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
    {
    ZERO_INITIALIZE(SurfaceData, surfaceData);
    ZERO_INITIALIZE(BuiltinData, builtinData);
#endif

    #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
        float2 terrainNormalMapUV = (input.texCoord0.xy + 0.5f) * _TerrainHeightmapRecipSize.xy;
        input.texCoord0.xy *= _TerrainHeightmapRecipSize.zw;
    #endif

    #ifdef _ALPHATEST_ON
        #ifndef UNITY_2022_2_OR_NEWER 
            ClipHoles(input.texCoord0.xy);
        #else
            float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, input.texCoord0.xy).r;
            GENERIC_ALPHA_TEST(hole, 0.5);
        #endif
    #endif
    float3 positionWS = GetAbsolutePositionWS(posInput.positionWS).xyz;
    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;

    TerrainLitSurfaceData terrainLitSurfaceData;
    InitializeTerrainLitSurfaceData(terrainLitSurfaceData); 

    #ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
        #ifdef TERRAIN_PERPIXEL_NORMAL_OVERRIDE
            float3 normalWS = terrainLitSurfaceData.normalData.xyz; // normalData directly contains normal in world space.
        #else
            float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, terrainNormalMapUV).rgb * 2 - 1;
            float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
        #endif
        float4 tangentWS = ConstructTerrainTangent(normalWS, GetObjectToWorldMatrix()._13_23_33);
        input.tangentToWorld = BuildTangentToWorld(tangentWS, normalWS);
        surfaceData.normalWS = normalWS;
    #else
        #ifndef UNITY_2022_2_OR_NEWER
            surfaceData.normalWS = float3(0.0, 1.0, 0.0); 
        #else
            surfaceData.normalWS = float3(0.0, 0.0, 0.0);
        #endif
    #endif

        surfaceData.tangentWS = normalize(input.tangentToWorld[0].xyz); // The tangent is not normalize in tangentToWorld for mikkt. Tag: SURFACE_GRADIENT
        surfaceData.geomNormalWS = input.tangentToWorld[2];
        float3 tangentViewDir = float3(0.0, 1.0, 0.0);

        #ifdef _TERRAIN_PARALLAX
            float3x3 objectToTangent = float3x3(surfaceData.tangentWS.xyz, (cross(surfaceData.geomNormalWS.xyz, surfaceData.tangentWS.xyz)) * -1, surfaceData.geomNormalWS.xyz);
            tangentViewDir = normalize(mul(objectToTangent, GetWorldSpaceNormalizeViewDir(posInput.positionWS)));
    
            float3 wNormal = surfaceData.geomNormalWS;
            half3 axisSign = sign(wNormal);
            half3 tangentY = normalize(cross(wNormal.xyz, half3(0, 0, axisSign.y)));
            half3 bitangentY = normalize(cross(tangentY.xyz, wNormal.xyz)) * axisSign.y;
            half3x3 tbnY = half3x3(tangentY, bitangentY, wNormal);
        #endif


        InTerraTerrainLitShade(input.texCoord0.xy, terrainLitSurfaceData, positionWS, input.tangentToWorld[2], tangentViewDir);


        surfaceData.baseColor = terrainLitSurfaceData.albedo;
        surfaceData.perceptualSmoothness = terrainLitSurfaceData.smoothness;
        surfaceData.metallic = terrainLitSurfaceData.metallic;
        surfaceData.ambientOcclusion = terrainLitSurfaceData.ao;

        surfaceData.subsurfaceMask = 0;
        surfaceData.thickness = 1;
        surfaceData.diffusionProfileHash = 0;
        #ifdef UNITY_2022_2_OR_NEWER
            surfaceData.transmissionMask = 0;
        #endif
        surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

        // Init other parameters
        surfaceData.anisotropy = 0.0;
        surfaceData.specularColor = float3(0.0, 0.0, 0.0);
        surfaceData.coatMask = 0.0;
        surfaceData.iridescenceThickness = 0.0;
        surfaceData.iridescenceMask = 0.0;

        // Transparency parameters
        // Use thickness from SSS
        surfaceData.ior = 1.0;
        surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
        surfaceData.atDistance = 1000000.0;
        surfaceData.transmittanceMask = 0.0;

        // This need to be init here to quiet the compiler in case of decal, but can be override later.
        surfaceData.specularOcclusion = 1.0;
    

    #if defined(DECAL_SURFACE_GRADIENT) && !defined(SHADER_STAGE_RAY_TRACING)

    #if !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL) || !defined(TERRAIN_PERPIXEL_NORMAL_OVERRIDE)
        float3 normalTS = ConvertToNormalTS(terrainLitSurfaceData.normalData, input.tangentToWorld[0], input.tangentToWorld[1]);

        #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0; // unused
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, input, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, input.tangentToWorld[2], surfaceData, normalTS);
        }
        #endif
        GetNormalWS(input, normalTS, surfaceData.normalWS, float3(1.0, 1.0, 1.0));
    #elif HAVE_DECALS
        if (_EnableDecals)
        {
            float3 normalTS = SurfaceGradientFromPerturbedNormal(input.tangentToWorld[2], surfaceData.normalWS);

            float alpha = 1.0; // unused
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, input, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, input.tangentToWorld[2], surfaceData, normalTS);

            GetNormalWS(input, normalTS, surfaceData.normalWS, float3(1.0, 1.0, 1.0));
        }
    #endif

    #else // defined(DECAL_SURFACE_GRADIENT) && !defined(SHADER_STAGE_RAY_TRACING)

    #if !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL) || !defined(TERRAIN_PERPIXEL_NORMAL_OVERRIDE)
        float3 normalTS = ConvertToNormalTS(terrainLitSurfaceData.normalData, input.tangentToWorld[0], input.tangentToWorld[1]);
        GetNormalWS(input, normalTS, surfaceData.normalWS, float3(1.0, 1.0, 1.0));
    #endif

    #if HAVE_DECALS && !defined(SHADER_STAGE_RAY_TRACING)
        if (_EnableDecals)
        {
            float alpha = 1.0; // unused
                               // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, input, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, input.tangentToWorld[2], surfaceData);
        }
    #endif

    #endif // DECAL_SURFACE_GRADIENT

    float3 bentNormalWS = surfaceData.normalWS;

    #if defined(DEBUG_DISPLAY) && !defined(SHADER_STAGE_RAY_TRACING)
        if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
        {
            TerrainLitDebug(input.texCoord0.xy, surfaceData.baseColor);
            surfaceData.metallic = 0;
        }
        // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
        // as it can modify attribute use for static lighting
        ApplyDebugToSurfaceData(input.tangentToWorld, surfaceData);
    #endif

        // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
        // Don't do spec occ from Ambient if there is no mask mask
    #if defined(_MASKMAP) && !defined(_SPECULAR_OCCLUSION_NONE)
        surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    #endif

    GetBuiltinData(input, V, posInput, surfaceData, 1, bentNormalWS, 0, builtinData);
    #ifdef UNITY_2022_2_OR_NEWER 
        RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
    #endif
}


#ifdef TESSELLATION_ON 
    float _TessellationFactor;
    float _TessellationFactorMinDistance;
    float _TessellationFactorMaxDistance;

    float _TessellationFactorTriangleSize;
    float _TessellationShapeFactor;
    float _TessellationBackFaceCullEpsilon;

    float _TessellationMaxDisplacement;
    float _EnableBlendModePreserveSpecularLighting;
    float _HeightCenter, _HeightAmplitude;
    float _Tessellation_HeightTransition;
    float _TessellationShadowQuality;
    
    struct LayerTexCoord
    {
        #ifndef LAYERED_LIT_SHADER
            UVMapping base;
            UVMapping details;
        #else
            // Regular texcoord
            UVMapping base0;
            UVMapping base1;
            UVMapping base2;
            UVMapping base3;

            UVMapping details0;
            UVMapping details1;
            UVMapping details2;
            UVMapping details3;

            // Dedicated for blend mask
            UVMapping blendMask;
        #endif

            // Store information that will be share by all UVMapping
            float3 vertexNormalWS;
            float3 triplanarWeights;

        #ifdef SURFACE_GRADIENT
            // tangent basis for each UVSet - up to 4 for now
            float3 vertexTangentWS0, vertexBitangentWS0;
            float3 vertexTangentWS1, vertexBitangentWS1;
            float3 vertexTangentWS2, vertexBitangentWS2;
            float3 vertexTangentWS3, vertexBitangentWS3;
        #endif
    };

    float GetMaxDisplacement()
    {
        return _TessellationMaxDisplacement;
    }

    void ApplyDisplacementTileScale(inout float height)
    {
    #ifdef _DISPLACEMENT_LOCK_TILING_SCALE
        height *= _InvTilingScale;
    #endif
    }
    #include "InTerra_Tessellation.hlsl"

    float3 ComputePerVertexTerrainDisplacement(float3 positionRWS, float3 normalWS, float2 uv, float4 vertexColor)
    {
        float3 height;
        Tessellation(positionRWS, normalWS, uv, height);
        return height;
    }

    float3 GetVertexDisplacement(float3 positionRWS, float3 normalWS, float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3, float4 vertexColor)
    {       
        float2 sampleCoords = float2(0.0, 0.0);

        #ifdef VARYINGS_DS_NEED_TEXCOORD0       
            sampleCoords = texCoord0.xy;
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD0
            sampleCoords = texCoord0.xy; 
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD1
            sampleCoords = texCoord1.xy;
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD2
            sampleCoords = texCoord2.xy; 
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD3
            sampleCoords = texCoord3.xy;
        #endif
   
        return ComputePerVertexTerrainDisplacement(positionRWS, normalWS, sampleCoords, vertexColor); 
    }


    float GetTessellationFactor(AttributesMesh input)
    {
    #if SHADERPASS == SHADERPASS_SHADOWS
        return _TessellationFactor * _TessellationShadowQuality;
    #else
        return _TessellationFactor;
    #endif
    }

    VaryingsMeshToDS ApplyTessellationModification(VaryingsMeshToDS input, float3 timeParameters)
    {
    #if defined(_TESSELLATION_DISPLACEMENT)

        input.positionRWS += GetVertexDisplacement(input.positionRWS + _WorldSpaceCameraPos, input.normalWS,
        #ifdef VARYINGS_DS_NEED_TEXCOORD0
            input.texCoord0.xy,
        #else
            float2(0.0, 0.0),
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD1
            input.texCoord1.xy,
        #else
            float2(0.0, 0.0),
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD2
            input.texCoord2.xy,
        #else
            float2(0.0, 0.0),
        #endif
        #ifdef VARYINGS_DS_NEED_TEXCOORD3
            input.texCoord3.xy,
        #else
            float2(0.0, 0.0),
        #endif
        #ifdef VARYINGS_DS_NEED_COLOR
            input.color
        #else
            float4(0.0, 0.0, 0.0, 0.0)
        #endif
            );

    #endif // _TESSELLATION_DISPLACEMENT

        return input;
    }

#endif
