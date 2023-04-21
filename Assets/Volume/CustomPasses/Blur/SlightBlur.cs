using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class SlightBlur : CustomPass
{
    [Range(0, 16)]
    public float radius = 4;
    public bool useMask = false;
    public LayerMask maskLayer = 0;
    public bool invertMask = false;

    Material compositeMaterial;
    Material whiteRenderersMaterial; //多余的没啥作用
    RTHandle downSampleBuffer;
    RTHandle blurBuffer;
    RTHandle maskBuffer;
    RTHandle maskDepthBuffer;
    RTHandle colorCopy;

    ShaderTagId[] shaderTags;

    // Trich to always include these shaders in build
    [SerializeField, HideInInspector]
    Shader compositeShader;
    [SerializeField, HideInInspector]
    Shader whiteRenderersShader;

    static class ShaderID
    {
        public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
        public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public static readonly int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");
        public static readonly int _Radius = Shader.PropertyToID("_Radius");
        public static readonly int _Source = Shader.PropertyToID("_Source");
        public static readonly int _ColorBufferCopy = Shader.PropertyToID("_ColorBufferCopy");
        public static readonly int _Mask = Shader.PropertyToID("_Mask");
        public static readonly int _MaskDepth = Shader.PropertyToID("_MaskDepth");
        public static readonly int _InvertMask = Shader.PropertyToID("_InvertMask");
        public static readonly int _ViewPortSize = Shader.PropertyToID("_ViewPortSize");
    }

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        compositeShader ??= Resources.Load<Shader>("CompositeBlur");
        whiteRenderersShader ??= Shader.Find("Hidden/Renderers/WhiteRenderers");

        compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
        whiteRenderersMaterial = CoreUtils.CreateEngineMaterial(whiteRenderersShader);

        //Allocate the buffers used for the blur in half resolution to save some menory
        downSampleBuffer = RTHandles.Alloc(
                Vector2.one * .5f, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, //we don`t need alpha in the blur
                useDynamicScale: true, name: "DownSampleBuffer"
            );

        blurBuffer = RTHandles.Alloc(
                Vector2.one * .5f, TextureXR.slices, dimension: TextureXR.dimension,
                colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
                useDynamicScale: true, name: "BlurBuffer"
            );

        shaderTags = new ShaderTagId[4]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("FirstPass")
        };
    }

    void AllocateMaskBuffersIfNeeded()
    {
        if(useMask)
        {
            if (colorCopy?.rt == null || !colorCopy.rt.IsCreated())
            {
                var hdrpAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
                var colorBufferFormat = hdrpAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;

                colorCopy = RTHandles.Alloc(
                        Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                        colorFormat: (GraphicsFormat)colorBufferFormat, useDynamicScale: true, name: "Color Copy"
                    );
            }
            if (maskBuffer?.rt == null || !maskBuffer.rt.IsCreated())
            {
                maskBuffer = RTHandles.Alloc(
                    Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R8_UNorm, useDynamicScale: true, name: "Blur Mask"
                );
            }
            if (maskDepthBuffer?.rt == null || !maskDepthBuffer.rt.IsCreated())
            {
                maskDepthBuffer = RTHandles.Alloc(
                    Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16_UInt, useDynamicScale: true, name: "Blur Mask Depth",
                    depthBufferBits: DepthBits.Depth16
                );
            }
        }
    }
    

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.

        AllocateMaskBuffersIfNeeded();

        if (compositeMaterial != null && radius > 0)
        {
            if (useMask)
            {
                //指定这次绘制的输出对象
                CoreUtils.SetRenderTarget(ctx.cmd, maskBuffer, maskDepthBuffer, ClearFlag.All);
                //特定层小于等于深度的时候绘制出来
                CustomPassUtils.DrawRenderers(ctx, maskLayer, overrideRenderState: new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) });
            }

            GenrateGaussianMips(ctx);
        }
    }

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    {
        //base.AggregateCullingParameters(ref cullingParameters, hdCamera);
        cullingParameters.cullingMask |= (uint)maskLayer.value;
    }


    void GenrateGaussianMips(CustomPassContext ctx)
    {
        RTHandle source = (targetColorBuffer == TargetBuffer.Camera) ? ctx.cameraColorBuffer : ctx.customColorBuffer.Value;
        //Save the non blurred color into a copy if the mask is enabled;
        if (useMask)
        {
            for(int i = 0; i < source.rt.volumeDepth; i ++)
            {
                ctx.cmd.CopyTexture(source, i, colorCopy, i);
            }
        }

        //## 其实Shader中能直接获取屏幕大小（估计是CustomBuffer需要？）
        // We need the viewport size in our shader because we're using half resolution render targets (and so the _ScreenSize
        // variable in the shader does not match the viewport).
        void SetViewPortSize(CommandBuffer cmd, MaterialPropertyBlock block, RTHandle target)
        {
            Vector2Int scaledViewportSize = target.GetScaledSize(target.rtHandleProperties.currentViewportSize);
            block.SetVector(ShaderID._ViewPortSize, new Vector4(scaledViewportSize.x, scaledViewportSize.y, 1.0f / (float)scaledViewportSize.x, 1.0f / (float)scaledViewportSize.y));
        }

        var targetBuffer = (useMask) ? downSampleBuffer : source;
        //HDRP 羽化函数调用
        CustomPassUtils.GaussianBlur(ctx, source, targetBuffer, blurBuffer, radius: radius);

        if(useMask)
        {
            using(new ProfilingScope(ctx.cmd, new ProfilingSampler("Compose Mask Blur")))
            {
                var compositingProperties = new MaterialPropertyBlock();

                compositingProperties.SetFloat(ShaderID._Radius, radius / 4f);  // The blur is 4 pixel wide tn the shader
                compositingProperties.SetTexture(ShaderID._Source, downSampleBuffer); //羽化效果图
                compositingProperties.SetTexture(ShaderID._ColorBufferCopy, colorCopy); //原相机效果图
                compositingProperties.SetTexture(ShaderID._Mask, maskBuffer);   //遮罩效果图
                compositingProperties.SetTexture(ShaderID._MaskDepth, maskDepthBuffer); //遮罩深度图
                compositingProperties.SetFloat(ShaderID._InvertMask, invertMask ? 1 : 0); //是否遮罩反转
                // SetViewPortSize(ctx.cmd, compositingProperties, source);

                //通过HDRP 绘制屏幕方法 CompositeMaterial 处理返回给 Pass 渲染对象source
                HDUtils.DrawFullScreen(ctx.cmd, compositeMaterial, source, compositingProperties, shaderPassId: 0);
            }
        }

    }

    protected override void Cleanup()
    {
        // Cleanup code
        CoreUtils.Destroy(compositeMaterial);
        CoreUtils.Destroy(whiteRenderersMaterial);
        downSampleBuffer.Release();
        blurBuffer.Release();
        maskDepthBuffer?.Release();
        maskBuffer?.Release();
        colorCopy?.Release();
    }
}