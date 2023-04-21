Shader "Hidden/Shader/OutLinePostProcessVolume"
{
    Properties
    {
        // This property is necessary to make the CommandBuffer.Blit bind the source texture to _MainTex
        _MainTex("Main Texture", 2DArray) = "grey" {}
        _Scale("Scale", int) = 1
        _DepthThreshold("DepthThreshold", float) = 0.2
        _NormalThreshold ("NormalThreshold", float) = 0.4
        _DepthNormalThreshold ("DepthNormalThreshold", float) = 0.2
        _DepthNormalThresholdScale ("DepthNormalThresholdScale", float) = 1.0
        _EdgeColor("EdgeColor", Color) = (1, 1, 1, 1)
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        //Add to the Varyings struct.
        float3 viewSpaceDir : TEXCOORD1;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    // List of properties to control your post process effect
    int _Scale;
    float _DepthThreshold;
    float _NormalThreshold;
    float _DepthNormalThreshold;
    float _DepthNormalThresholdScale;
    float4x4 _ClipToView;
    float4 _EdgeColor;
    float _Intensity;
    TEXTURE2D_X(_MainTex);
    
    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
         //Add to the vertex shader, below the line assigning o.vertex.
        output.viewSpaceDir = mul(_ClipToView, float4(output.positionCS.x, -output.positionCS.y, 0, 1)).xyz;
        
        return output;
    }

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Note that if HDUtils.DrawFullScreen is not used to render the post process, you don't need to call ClampAndScaleUVForBilinearPostProcessTexture.

        float3 sourceColor = SAMPLE_TEXTURE2D_X(_MainTex, s_linear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(input.texcoord.xy)).xyz;

        //OutLine 
        float halfScaleFloor = floor(_Scale * 0.5);
        float halfScaleCeil = ceil(_Scale * 0.5);
        
        float2 bottomLeftUV = input.positionCS.xy - (float2)halfScaleFloor;
        float2 topRightUV = input.positionCS.xy + (float2)halfScaleCeil;
        float2 bottomRightUV = input.positionCS.xy + float2(halfScaleCeil, -halfScaleFloor);
        float2 topLeftUV = input.positionCS.xy + float2(-halfScaleFloor, halfScaleCeil);
        
        float depth0 = LoadCameraDepth(bottomLeftUV);
        float depth1 = LoadCameraDepth(topRightUV);
        float depth2 = LoadCameraDepth(bottomRightUV);
        float depth3 = LoadCameraDepth(topLeftUV);

        //Add above the return depth0 line.
        float depthFiniteDifference0 = depth1 - depth0;
        float depthFiniteDifference1 = depth3 - depth2;
        //Add below the lines computing the finite differences.
        float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100;
        ////Add below the line declaring edgeDepth.
        //float depthThreshold = _DepthThreshold * depth0;
        //edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

        //Get Normal Data
        NormalData normalData0;
        DecodeFromNormalBuffer(bottomLeftUV, normalData0);
        NormalData normalData1;
        DecodeFromNormalBuffer(topRightUV, normalData1);
        NormalData normalData2;
        DecodeFromNormalBuffer(bottomRightUV, normalData2);
        NormalData normalData3;
        DecodeFromNormalBuffer(topLeftUV, normalData3);
        float3 normal0 = normalData0.normalWS;
        float3 normal1 = normalData1.normalWS;
        float3 normal2 = normalData2.normalWS;
        float3 normal3 = normalData3.normalWS;

        //Add below the code sampling the normals.
        float3 normalFiniteDifference0 = normal1 - normal0;
        float3 normalFiniteDifference1 = normal3 - normal2;
        
        float3 edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
        edgeNormal = edgeNormal > _NormalThreshold ?  1 : 0;

        float3 viewNormal = normal0 * 2 - 1;
        float NdotV = 1 - dot(viewNormal, -input.viewSpaceDir);
        //Add below the line declaring NdotV.
        float normalThreshold01 = saturate(NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold);
        //Add below the line declaring normalThreshold01.
        float normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1;
        //Modify the existing line declaring depthThreshold.
        float depthThreshold = _DepthThreshold * depth0 * normalThreshold;
        edgeDepth = edgeDepth > depthThreshold ? 1 : 0;
        
        //Add at the bottom, just above the line declaring float4 color.
        float edge = max(edgeDepth, edgeNormal);

        float4 edgeColor = float4(_EdgeColor.rgb, _EdgeColor.a * edge);
        edgeColor.rgb = lerp(sourceColor.rgb, edgeColor.rgb, edgeColor.a);

        // Apply greyscale effect
        float3 color = lerp(sourceColor, edgeColor, _Intensity);

        return float4(color, 1);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "OutLinePostProcessVolume"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
