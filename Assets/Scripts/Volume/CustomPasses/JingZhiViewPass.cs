using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

//#if UNITY_EDITOR
//using UnityEditor.Rendering.HighDefinition;

//[CustomPassDrawer(typeof(JingZhiViewPass))]
//class JingZhiViewPassEditor : CustomPassDrawer
//{
//    protected override PassUIFlag commonPassUIFlags => PassUIFlag.Name;
//}
//#endif

class JingZhiViewPass : CustomPass
{
    public LayerMask blackShowMask;
    private Camera blackCam;

    protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
    => cullingParameters.cullingMask |= (uint)blackShowMask.value;

    protected override bool executeInSceneView => false;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        Transform blackCamTrans = Camera.main.transform.Find("TransCam");
        if (blackCamTrans != null)
            blackCam = blackCamTrans.GetComponent<Camera>();
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (blackCam == null) return;
        if (ctx.hdCamera.camera.cameraType == CameraType.SceneView) return;

        var depthTestOverride = new RenderStateBlock(RenderStateMask.Depth)
        {
            depthState = new DepthState(true, CompareFunction.LessEqual),
        };

        SetBlackCamWithMainCam();
        //Render the object depth + normal depending on the injection point
        if (injectionPoint == CustomPassInjectionPoint.AfterOpaqueDepthAndNormal)
        {
            CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", true);
            RenderFromCameraDepthPass(ctx, blackCam, ctx.cameraNormalBuffer, ctx.cameraDepthBuffer, ClearFlag.None, blackShowMask, overrideRenderState: depthTestOverride);
            CoreUtils.SetKeyword(ctx.cmd, "WRITE_NORMAL_BUFFER", false);
        }
        else
            CustomPassUtils.RenderFromCamera(ctx, blackCam, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None, blackShowMask, overrideRenderState: depthTestOverride);
    }

    public void RenderFromCameraDepthPass(in CustomPassContext ctx, Camera view, RTHandle targetColor, RTHandle targetDepth, ClearFlag clearFlag, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default(RenderStateBlock))
    {
        ShaderTagId[] depthTags = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        if (targetColor != null && targetDepth != null)
            CoreUtils.SetRenderTarget(ctx.cmd, targetColor, targetDepth, clearFlag);
        else if (targetColor != null)
            CoreUtils.SetRenderTarget(ctx.cmd, targetColor, clearFlag);
        else if (targetDepth != null)
            CoreUtils.SetRenderTarget(ctx.cmd, targetDepth, clearFlag);

        using (new CustomPassUtils.DisableSinglePassRendering(ctx))
        {
            using(new CustomPassUtils.OverrideCameraRendering(ctx, view))
            {
                CustomPassUtils.DrawRenderers(ctx, depthTags, layerMask, renderQueueFilter, overrideMaterial, overrideMaterialIndex, overrideRenderState);
            }
        }
    }

    private void SetBlackCamWithMainCam()
    {
        Camera mainCam = Camera.main;
        if (!mainCam || !blackCam) return;
        blackCam.fieldOfView = mainCam.fieldOfView;
        blackCam.nearClipPlane = mainCam.nearClipPlane;
        blackCam.farClipPlane = mainCam.farClipPlane;
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}