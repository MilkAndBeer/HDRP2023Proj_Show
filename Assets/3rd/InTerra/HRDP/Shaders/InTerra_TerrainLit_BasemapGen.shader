Shader "Hidden/InTerra/HDRP/TerrainLit_BasemapGen"
{
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }

    SubShader
    { 
        PackageRequirements { "com.unity.render-pipelines.high-definition":"[12.1,15.1.3]" }
        Tags { "RenderPipeline" = "HDRenderPipeline" "SplatCount" = "8" }

        HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #define SURFACE_GRADIENT // Must use Surface Gradient as the normal map texture format is now RG floating point
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitSurfaceData.hlsl"

        // Terrain builtin keywords
        #pragma shader_feature_local _TERRAIN_8_LAYERS
        #pragma shader_feature_local _NORMALMAP
        #pragma shader_feature_local _MASKMAP

        // InTerra Keywords
        #pragma shader_feature_local __ _TERRAIN_MASK_MAPS _TERRAIN_NORMAL_IN_MASK
        #pragma shader_feature_local __ _TERRAIN_TRIPLANAR_ONE _TERRAIN_TRIPLANAR
        #pragma shader_feature_local_fragment _TERRAIN_DISTANCEBLEND
        #pragma shader_feature_local_fragment _TERRAIN_BLEND_HEIGHT

        #pragma shader_feature_local _LAYERS_TWO       

        #define _TERRAIN_BASEMAP_GEN

        #if defined(_TERRAIN_TRIPLANAR) || defined(_TERRAIN_TRIPLANAR_ONE)
            #ifdef _TERRAIN_TRIPLANAR_ONE
                #define TRIPLANAR_TINT
            #endif
            #undef _TERRAIN_TRIPLANAR
            #undef _TERRAIN_TRIPLANAR_ONE
        #endif

        #include "InTerra_TerrainLit_Splatmap_Includes.hlsl"

        CBUFFER_START(UnityTerrain)
            UNITY_TERRAIN_CB_VARS
            float4 _Control0_ST;
        CBUFFER_END

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float4 texcoord : TEXCOORD0;
        };

        #pragma vertex Vert
        #pragma fragment Frag

        float2 ComputeControlUV(float2 uv)
        {
            // adjust splatUVs so the edges of the terrain tile lie on pixel centers
            return (uv) ;
        }

        Varyings Vert(uint vertexID : SV_VertexID)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(vertexID);
            output.texcoord.xy = TRANSFORM_TEX(GetFullScreenTriangleTexCoord(vertexID), _Control0);
            output.texcoord.zw = ComputeControlUV(output.texcoord.xy);
            return output;
        }

        ENDHLSL

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #include "InTerra_TerrainLit_Splatmap.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                TerrainLitSurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(surfaceData);
                InTerraTerrainLitShade(input.texcoord.zw, surfaceData, float3(0, 0, 0), float3(0, 1, 0), float3(0, 0, 0));
                return float4(surfaceData.albedo, surfaceData.smoothness);
            }

            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

           
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
            #include "InTerra_TerrainLit_Splatmap.hlsl"

            float3 Frag(Varyings input) : SV_Target
            {
                TerrainLitSurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(surfaceData);
                InTerraTerrainLitShade(input.texcoord.zw, surfaceData, float3(0, 0, 0), float3(0, 1, 0), float3(0, 0, 0));
                float splatControl = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, input.texcoord.zw).r;

                return float3(surfaceData.metallic, surfaceData.ao, splatControl);
            }

            ENDHLSL
        }

        

        Pass
        {
            Tags
            {
                "Name" = "_TriplanarTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }
            
            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #define _TERRAIN_BASEMAP_GEN_TRIPLANAR
            #include "InTerra_TerrainLit_Splatmap.hlsl"

            float4 Frag(Varyings input) : SV_Target
            {
                
                TerrainLitSurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(surfaceData);
                InTerraTerrainLitShade(input.texcoord.zw, surfaceData, float3(0, 0, 0), float3(0, 1, 0), float3(0, 0, 0));
                return float4(surfaceData.albedo, surfaceData.smoothness);
            }

            ENDHLSL
        }
                
        Pass
        {
            Tags
            {
                "Name" = "_Triplanar_MetallicAO"
                "Format" = "RG16"
                "Size" = "1"
            }
            
            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]

            HLSLPROGRAM

            #define _TERRAIN_BASEMAP_GEN_TRIPLANAR
            #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
            #include "InTerra_TerrainLit_Splatmap.hlsl"

            float2 Frag(Varyings input) : SV_Target
            {               
                TerrainLitSurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(surfaceData);
                InTerraTerrainLitShade(input.texcoord.zw, surfaceData, float3(0, 0, 0), float3(0, 1, 0), float3(0, 0, 0));
                return float2(surfaceData.metallic, surfaceData.ao);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
