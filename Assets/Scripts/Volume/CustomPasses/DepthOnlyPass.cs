using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class DepthOnlyPass : CustomPass
{
    private RTHandle onlyColorBuffer;
    private RTHandle onlyDepthBuffer;
    public LayerMask showLayer = 0;

    private int screenWidth = 1920;
    private int screenHeight = 1080;

    ShaderTagId[] shaderTags;

    public enum onlyBufferType
    {
        Player,
        Monster,
        Scene,
        EffectBlur,
    }

    public onlyBufferType onlyShow = onlyBufferType.Player;

    private string colorBufferName = "";
    private string depthBufferName = "";

    protected override bool executeInSceneView => false;
    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        shaderTags = new ShaderTagId[4]
        {
            new ShaderTagId("Forward"),
            new ShaderTagId("ForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("FirstPass"),
        };
        string tempName = "";
        switch (onlyShow)
        {
            case onlyBufferType.Player:
                tempName = "Player";
                break;
            case onlyBufferType.Monster:
                tempName = "Monster";
                break;
            case onlyBufferType.Scene:
                tempName = "Scene";
                break;
            case onlyBufferType.EffectBlur:
                tempName = "EffectBlur";
                break;
            default: break;
        }

        colorBufferName = string.Format("_Only{0}ColorTex", tempName);
        depthBufferName = string.Format("_Only{0}DepthTex", tempName);

        InitBuffer(true);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.
#if UNITY_EDITOR
        if (ctx.hdCamera.camera.name == "SceneCamera")
            return;
#endif
        bool isChangeSreenSize = (Screen.width != screenWidth || Screen.height != screenHeight);
        InitBuffer(isChangeSreenSize);
        if (onlyColorBuffer == null || onlyDepthBuffer == null) return;
        var scale = RTHandles.rtHandleProperties.rtHandleScale;

        CoreUtils.SetRenderTarget(ctx.cmd, onlyColorBuffer, onlyDepthBuffer, ClearFlag.All);
        if (onlyShow == onlyBufferType.Scene)
        {
            CustomPassUtils.DrawRenderers(ctx, showLayer, overrideRenderState: new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) });
        }
        else
        {
            CustomPassUtils.DrawRenderers(ctx, showLayer, overrideRenderState:
                new RenderStateBlock(RenderStateMask.Blend | RenderStateMask.Depth)
                {
                    blendState = new BlendState(false, true),
                    depthState = new DepthState(true, CompareFunction.LessEqual)
                });
        }

        if (onlyColorBuffer != null)
        {
            Shader.SetGlobalTexture(Shader.PropertyToID(colorBufferName), onlyColorBuffer);
        }
        if (onlyDepthBuffer != null)
        {
            Shader.SetGlobalTexture(Shader.PropertyToID(depthBufferName), onlyDepthBuffer);
        }
    }

    protected override void Cleanup()
    {
        // Cleanup code
        RelaseBuffers();
    }
    
    private void RelaseBuffers()
    {
        if (onlyDepthBuffer != null)
        {
            onlyDepthBuffer.Release();
            onlyDepthBuffer = null;
        }
        if (onlyColorBuffer != null)
        {
            onlyColorBuffer.Release();
            onlyColorBuffer = null;
        }
    }

    private void InitBuffer(bool changeScreenSize)
    {
        if (changeScreenSize)
        {
            RelaseBuffers();
            screenWidth = Screen.width;
            screenHeight = Screen.height;
        }
        if (onlyColorBuffer == null)
        {
            onlyColorBuffer = RTHandles.Alloc(
                    screenWidth, screenHeight, TextureXR.slices, dimension: TextureXR.dimension,
                    colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                    useDynamicScale: true, name: "Render Only"
                );
        }
        if (onlyDepthBuffer == null)
        {
            onlyDepthBuffer = RTHandles.Alloc(
                        screenWidth, screenHeight, TextureXR.slices, dimension: TextureDimension.Tex2DArray,
                        colorFormat: GraphicsFormat.R8_UInt, useDynamicScale: true,
                        name: "Render Only Depth", depthBufferBits: DepthBits.Depth16
                        );
        }
    }
}