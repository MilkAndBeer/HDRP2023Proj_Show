using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor.Rendering.HighDefinition;

[CustomPassDrawer(typeof(FPSForeground))]
class FPSForegroundEditor : CustomPassDrawer
{
    protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
}
#endif

class FPSForeground : CustomPass
{
    public float fov = 45;
    public LayerMask foregroundMask;
    private Camera foregroundCamera;
    private const string kCameraTag = "_FPSForegroundCamera";
    Material depthClearMaterial;
    RTHandle trueDepthBuffer;

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    {
        cullingParameters.cullingMask |= (uint)foregroundMask.value;
    }

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        //Hidden foreground camera:
        var cam = GameObject.Find(kCameraTag);
        if(cam == null)
        {
            cam = new GameObject(kCameraTag);
            cam.AddComponent<Camera>();
        }

        Shader tempShader = Shader.Find("Hidden/Renderers/ForegroundDepthClear");
        if (tempShader == null) return;
        depthClearMaterial = new Material(tempShader);
        var temp_trueDepthBuffer = new RenderTargetIdentifier(BuiltinRenderTextureType.Depth);
        trueDepthBuffer = RTHandles.Alloc(temp_trueDepthBuffer);
        foregroundCamera = cam.GetComponent<Camera>();
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.

        if (depthClearMaterial == null) return;

        //Disable it for scene view because it`s horrible
        if (ctx.hdCamera.camera.cameraType == CameraType.SceneView) return;
        var currentCam = ctx.hdCamera.camera;
        //Make sure the camera is disabled, we don`t want it to render anything.
        foregroundCamera.enabled = false;
        foregroundCamera.fieldOfView = fov;
        foregroundCamera.cullingMask = foregroundMask;

        var depthTestOverride = new RenderStateBlock(RenderStateMask.Depth) {
            depthState = new DepthState(true, CompareFunction.LessEqual)
        };

        //TODO: Nuke the depth in the after depth and normal injection point
        // Override depth to 0 (avoid artifacts with screen-space effects)
        CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", true);
        ctx.cmd.SetRenderTarget(ctx.cameraNormalBuffer, trueDepthBuffer, 0, CubemapFace.Unknown, 0);
        RenderFromCameraDepthPass(ctx, foregroundCamera, null, null, ClearFlag.None, foregroundMask, overrideMaterial: depthClearMaterial, overrideMaterialIndex: 0);
        CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", false);

        //Render the Object color or normal + depth depending o nthe injection point
        if(injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal)
        {
            CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", true);
            RenderFromCameraDepthPass(ctx, foregroundCamera, ctx.cameraNormalBuffer, ctx.cameraDepthBuffer, ClearFlag.None, foregroundMask, overrideRenderState: depthTestOverride);
            CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", false);
        }
        else
        {
            CustomPassUtils.RenderFromCamera(ctx, foregroundCamera, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None, foregroundMask, overrideRenderState: depthTestOverride);
        }
    }

    public void RenderFromCameraDepthPass(in CustomPassContext ctx, Camera view, RTHandle targetColor, RTHandle targetDepth, ClearFlag clearFlag, LayerMask layerMask, 
                CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default(RenderStateBlock))
    {
        ShaderTagId[] depthTags = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        if (targetColor != null && targetDepth != null)
            CoreUtils.SetRenderTarget(ctx.cmd, targetColor, targetDepth, clearFlag);
        else if (targetColor != null)
            CoreUtils.SetRenderTarget(ctx.cmd, targetColor, clearFlag);
        else if (targetDepth != null)
            CoreUtils.SetRenderTarget(ctx.cmd, targetDepth, clearFlag);

        //停止当前相机渲染
        using(new CustomPassUtils.DisableSinglePassRendering(ctx))
        {
            //使用指定相机View 参数进行渲染画面
            using(new CustomPassUtils.OverrideCameraRendering(ctx, view))
            {
                //using(new ProfilingScope(ctx.cmd, renderFromCameraSampler))
                CustomPassUtils.DrawRenderers(ctx, depthTags, layerMask, renderQueueFilter, overrideMaterial, overrideMaterialIndex, overrideRenderState);
            }
        }

    }

    protected override void Cleanup()
    {
        // Cleanup code
        trueDepthBuffer?.Release();
        CoreUtils.Destroy(depthClearMaterial);
        // CoreUtils.Destroy(foregroundCamera);
    }
}