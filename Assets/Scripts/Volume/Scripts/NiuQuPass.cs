using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class NiuQuPass : CustomPass
{
    public LayerMask maskLayer = 0;
    private ShaderTagId[] shaderTags;

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        shaderTags = new ShaderTagId[1] {
            new ShaderTagId("EffectNiuQiu"),
        };
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.

        //var drawSettings = CreateDrawRenderersPass(RenderQueueType.AllTransparent, maskLayer, null, shaderTags[0].name, SortingCriteria.CommonTransparent);
        CustomPassUtils.DrawRenderers(ctx, shaderTags, maskLayer, RenderQueueType.AllTransparent, null, 0, default, SortingCriteria.CommonTransparent);
    }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}