using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class InvaWavePass : CustomPass
{
    private RTHandle targetColorBuffer = null;

    public Camera bakingCamera = null;
    public bool render = true;

    private int camSize = 4096;

    private InteractiveShaderRT interWaveCamData = null;
    protected override bool executeInSceneView => false;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        InitBuffer();
        interWaveCamData = bakingCamera.GetComponent<InteractiveShaderRT>();
    }

    protected override void Execute(CustomPassContext ctx)
    {
        InitBuffer();

        if (targetColorBuffer == null || bakingCamera == null) return;

        bakingCamera.TryGetCullingParameters(out var cullingParams);
        cullingParams.cullingOptions = CullingOptions.None;

        ctx.cullingResults = ctx.renderContext.Cull(ref cullingParams);
        CustomPassUtils.RenderFromCamera(ctx, bakingCamera, targetColorBuffer, null, ClearFlag.All, bakingCamera.cullingMask); //, overrideRenderState: ovrrideDepthTest
        if(interWaveCamData)
            interWaveCamData.SetMatDatas(targetColorBuffer);
    }

    protected override void Cleanup()
    {
        ClearBuffer();
    }

    private void InitBuffer()
    {
        if (targetColorBuffer == null)
        {
            targetColorBuffer = RTHandles.Alloc(
                    camSize, camSize,
                    colorFormat: GraphicsFormat.R32G32B32A32_SFloat, filterMode: FilterMode.Bilinear,
                    useDynamicScale: false, name: "Render InteraveWater"
                );
        }
    }

    private void ClearBuffer()
    {
        if (targetColorBuffer != null)
        {
            targetColorBuffer.Release();
            targetColorBuffer = null;
        }
    }
}