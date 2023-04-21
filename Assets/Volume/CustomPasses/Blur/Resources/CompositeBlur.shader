Shader "Hidden/FullScreen/CompositeBlur"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

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

    //--########## Custom Shader #############--
    TEXTURE2D_X(_Source); //羽化图
    TEXTURE2D_X(_ColorBufferCopy); //原图 可以用于混合但是这里用的透明裁剪，未使用上
    TEXTURE2D_X_HALF(_Mask); //遮罩图
    TEXTURE2D_X_HALF(_MaskDepth); //遮罩深度图
    float4 _ViewPortSize;
    float _InvertMask;

    //We need to clamp the UVs to avoid bleeding from bigger render targets (when we have multiple cameras)
    float2 ClampUVs(float2 uv)
    {
        //Global 参数不太懂？？？ _RTHandleScale _ScreenSize
        uv = clamp(uv, 0, _RTHandleScale.xy - _ScreenSize.zw * 2); // clamp UV to 1 pixel to avoid bleeding
        return uv;
    }
    
    float2 GetSampleUVs(Varyings varyings)
    {
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        return posInput.positionNDC.xy * _RTHandleScale.xy;
    }
    
    float4 CompositeMaskedBlur(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        float2 uv = ClampUVs(varyings.positionCS.xy * _ScreenSize.zw * _RTHandleScale.xy);

        float4 blurredBuffer = SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0);
        float4 mask = SAMPLE_TEXTURE2D_X_LOD(_Mask, s_linear_clamp_sampler, uv, 0);
        float maskDepth = SAMPLE_TEXTURE2D_X_LOD(_MaskDepth, s_linear_clamp_sampler, uv, 0);
        float maskValue = 0;

        //遮罩处颜色不为黑色，深度大于等于屏幕对应深度（非无深度部分）
        maskValue = any(mask.rgb > 0.1) || (maskDepth > depth - 0.0001 && maskDepth != 0);
        if(_InvertMask < 0.5) maskValue = !maskValue;

        return float4(blurredBuffer.rgb, maskValue);
    }

    //------------------------------------------
    
    float4 FullScreenPass(Varyings varyings) : SV_Target
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
            Blend SrcAlpha OneMinusSrcAlpha  //透明混合
            Cull Off

            HLSLPROGRAM
                #pragma fragment CompositeMaskedBlur
            ENDHLSL
        }
    }
    Fallback Off
}
