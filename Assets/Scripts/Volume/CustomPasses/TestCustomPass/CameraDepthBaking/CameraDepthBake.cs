using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class CameraDepthBake : CustomPass
{
    public Camera bakingCamera = null;
    public RenderTexture depthTexture = null;
    public RenderTexture normalTexture = null;
    public RenderTexture tangentTexture = null;
    public bool render = true;

    protected override bool executeInSceneView => false;

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.

        if (!render || ctx.hdCamera.camera == bakingCamera || bakingCamera == null || ctx.hdCamera.camera.cameraType == CameraType.SceneView)
            return;
        if (depthTexture == null && normalTexture == null && tangentTexture == null)
        {
            Debug.LogError("No texture to render to");
            return;
        }

        //We need to be careful about the aspect ratio of render textures when doing the culling, otherwise it could result in objects poping:
        if (depthTexture != null)
            bakingCamera.aspect = Mathf.Max(bakingCamera.aspect, depthTexture.width/(float)depthTexture.height);
        if(normalTexture != null)
            bakingCamera.aspect = Mathf.Max(bakingCamera.aspect, normalTexture.width / (float)normalTexture.height);
        if(tangentTexture != null)
            bakingCamera.aspect = Mathf.Max(bakingCamera.aspect, tangentTexture.width / (float)tangentTexture.height);
        bakingCamera.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.None;

        //Assign the custom culling result to the context
        //so it`ll be used for the following operations
        ctx.cullingResults = ctx.renderContext.Cull(ref cullingParams);
        var overrideDepthTest = new RenderStateBlock(RenderStateMask.Depth) { depthState = new DepthState(true, CompareFunction.LessEqual) };

        //Depth
        if(depthTexture != null)
            CustomPassUtils.RenderDepthFromCamera(ctx, bakingCamera, depthTexture, ClearFlag.Depth, bakingCamera.cullingMask, overrideRenderState: overrideDepthTest);
        //Normal
        if(normalTexture != null)
            CustomPassUtils.RenderNormalFromCamera(ctx, bakingCamera, normalTexture, ClearFlag.All, bakingCamera.cullingMask, overrideRenderState: overrideDepthTest);
        //Tangent
        if (tangentTexture != null)
            CustomPassUtils.RenderTangentFromCamera(ctx, bakingCamera, tangentTexture, ClearFlag.All, bakingCamera.cullingMask, overrideRenderState: overrideDepthTest);
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}