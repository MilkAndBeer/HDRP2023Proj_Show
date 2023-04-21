Shader "FullScreen/OutlinePass"
{
    Properties
    {
        _Scale("Scale", int) = 1
        _DepthThreshold("DepthThreshold", float) = 0.2
        _NormalThreshold ("NormalThreshold", float) = 0.4
        _DepthNormalThreshold ("DepthNormalThreshold", float) = 0.2
        _DepthNormalThresholdScale ("DepthNormalThresholdScale", float) = 1.0
        _EdgeColor("EdgeColor", Color) = (1, 1, 1, 1)
    }
    
    HLSLINCLUDE

    #pragma vertex VertOutLine

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
    
    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float4 CustomPassSampleCustomColor(float2 uv);
    // float4 CustomPassLoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.
    int _Scale;
    float _DepthThreshold;
    float _NormalThreshold;
    float _DepthNormalThreshold;
    float _DepthNormalThresholdScale;
    float4x4 _ClipToView;
    float4 _EdgeColor;

    struct VaryingsOutLine
    {
        float4 positionCS : SV_POSITION;
        //Add to the Varyings struct.
        float3 viewSpaceDir : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    VaryingsOutLine VertOutLine(Attributes input)
    {
        VaryingsOutLine output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        //Add to the vertex shader, below the line assigning o.vertex.
        output.viewSpaceDir = mul(_ClipToView, float4(output.positionCS.x, -output.positionCS.y, 0, 1)).xyz;

        return output;
    }

    float4 FullScreenPass(VaryingsOutLine varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        float4 color = float4(0.0, 0.0, 0.0, 0.0);

        // Load the camera color buffer at the mip 0 if we're not at the before rendering injection point
        if (_CustomPassInjectionPoint != CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING)
            color = float4(CustomPassLoadCameraColor(varyings.positionCS.xy, 0), 1);

        // Add your custom pass code here
        float halfScaleFloor = floor(_Scale * 0.5);
        float halfScaleCeil = ceil(_Scale * 0.5);
        
        float2 bottomLeftUV = varyings.positionCS.xy - (float2)halfScaleFloor;
        float2 topRightUV = varyings.positionCS.xy + (float2)halfScaleCeil;
        float2 bottomRightUV = varyings.positionCS.xy + float2(halfScaleCeil, -halfScaleFloor);
        float2 topLeftUV = varyings.positionCS.xy + float2(-halfScaleFloor, halfScaleCeil);
        
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
        float NdotV = 1 - dot(viewNormal, -varyings.viewSpaceDir);
        //Add below the line declaring NdotV.
        float normalThreshold01 = saturate(NdotV - _DepthNormalThreshold) / (1 - _DepthNormalThreshold);
        //Add below the line declaring normalThreshold01.
        float normalThreshold = normalThreshold01 * _DepthNormalThresholdScale + 1;
        //Modify the existing line declaring depthThreshold.
        float depthThreshold = _DepthThreshold * depth0 * normalThreshold;
        edgeDepth = edgeDepth > depthThreshold ? 1 : 0;
        
        //Add at the bottom, just above the line declaring float4 color.
        float edge = max(edgeDepth, edgeNormal);
        
        ////--@@@@@Test
        //return half4((half3)edge, 1);
        float4 edgeColor = float4(_EdgeColor.rgb, _EdgeColor.a * edge);
        color.rgb = lerp(color.rgb, edgeColor.rgb, edgeColor.a);
        // Fade value allow you to increase the strength of the effect while the camera gets closer to the custom pass volume
        float f = 1 - abs(_FadeValue * 2 - 1);
        return float4(color.rgb + f, color.a);
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
