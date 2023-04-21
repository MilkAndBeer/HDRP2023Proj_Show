Shader "FullScreen/EdgeDetectPass"
{
    Properties
    {
        _EdgeColor("EdgeColor", Color) = (0, 0, 0, 1)
        _Sensitivity("Sensitivity", Vector) = (0, 0.02, 0, 0)
        _DepthAlpha("DepthAlpha", Vector) = (0, 0.02, 0, 0)
        _Intensity("Intensity", Float) = 1
        _Width("Width", Float) = 1
        _IsDebug("Debug", Float) = 1
        _FarPlane("Far Plane", Float) = 55000
    }

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

    float4 _EdgeColor;
    float4 _Sensitivity;
    float4 _DepthAlpha;
    float _Intensity;
    float _Width;
    float _IsDebug;
    float _FarPlane;

    half LuminanceEdge(float4 color)
    {
        return  0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
    }

    half Sobel(half2 uv[9])
    {
        const half Gx[9] = { -1, 0, 1,
                                        -2, 0, 2,
                                        -1, 0, 1 };
        const half Gy[9] = { -1, -2, -1,
                                        0, 0, 0,
                                        1, 2, 1 };

        half depth = 0;
        half depthX = 0;
        half depthY = 0;
                            
        half depthCenter = LoadCameraDepth(uv[4]).r;
        for (int it = 0; it < 9; it++)
        {
            depth = LoadCameraDepth(uv[it]);

            depth = LuminanceEdge(depth);

            depthX += depth * Gx[it];
            depthY += depth * Gy[it];
        }
        depth = sqrt(abs(depthX) * abs(depthX) + abs(depthY) * abs(depthY));
        depth = smoothstep(_Sensitivity.x, _Sensitivity.y, depth - depthCenter);

        //获取相机FarPlane 和 默认55000 比较做比例显示
        depth = 1 - Linear01Depth(depth, _ZBufferParams);
        //depth = depth/_ProjectionParams.z * _FarPlane;

        half edge = max(0, depth);
        return edge;
    }


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
        half size = _Width;
        half edge = 1;

        half2 uv0 = posInput.positionSS;
        half2 uv[9];
        uv[0] = uv0 + size * half2(-1, -1);
        uv[1] = uv0 + size * half2(0, -1);
        uv[2] = uv0 + size * half2(1, -1);
        uv[3] = uv0 + size * half2(-1, 0);
        uv[4] = uv0 + size * half2(0, 0);
        uv[5] = uv0 + size * half2(1, 0);
        uv[6] = uv0 + size * half2(-1, 1);
        uv[7] = uv0 + size * half2(0, 1);
        uv[8] = uv0 + size * half2(1, 1);
        edge = Sobel(uv);

        //edge = saturate(edge * _Intensity * 500);
        
        //获取相机FarPlane 和 默认55000 比较做比例显示
        depth = 1 - Linear01Depth(depth, _ZBufferParams);
        //depth = depth/_ProjectionParams.z * _FarPlane;
        half depthAlpha = smoothstep(_DepthAlpha.x, _DepthAlpha.y, depth);

        edge *= _EdgeColor.a * depthAlpha;

        if (_IsDebug) {
            return half4((half3)edge, 1);
        }

        color = lerp(color, _EdgeColor * _Intensity, edge);

        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "DrawProcedural"

            ZWrite Off
            ZTest Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
