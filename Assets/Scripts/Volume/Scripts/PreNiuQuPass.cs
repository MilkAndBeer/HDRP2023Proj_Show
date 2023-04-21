using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

class PreNiuQuPass : CustomPass
{
    RTHandle colorCopy;
    string m_GrabPassName = "_GrabTexture";

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Setup code here
        Debug.Log("PreNiuQuPass Setup");

        var hdrpAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
        var colorBufferFormat = hdrpAsset.currentPlatformRenderPipelineSettings.colorBufferFormat;

        colorCopy = RTHandles.Alloc(
                Vector2.one, TextureXR.slices, dimension: TextureXR.dimension, 
                colorFormat: (GraphicsFormat)colorBufferFormat,
                useDynamicScale: true, name: "Color Copy"
            );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Executed every frame for all the camera inside the pass volume.
        // The context contains the command buffer to use to enqueue graphics commands.
        RTHandle source = ctx.cameraColorBuffer;
        for (int i = 0; i < source.rt.volumeDepth; i++)
            ctx.cmd.CopyTexture(source, i, colorCopy, i);
        ctx.cmd.SetGlobalTexture(m_GrabPassName, colorCopy.nameID);
    }

    protected override void Cleanup()
    {
        colorCopy?.Release();
    }
}