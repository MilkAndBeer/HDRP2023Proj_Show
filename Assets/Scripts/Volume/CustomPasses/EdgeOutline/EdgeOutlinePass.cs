using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif 
using DG.Tweening;

class EdgeOutlinePass : CustomPass
{
    /// <summary>
    /// Material used for the fullscreen pass, it's shader must have been created with.
    /// </summary>
    public Material fullscreenPassMaterial;
    [SerializeField]
    int materialPassIndex = 0;
    /// <summary>
    /// Name of the pass to use in the fullscreen material.
    /// </summary>
    public string materialPassName = "Custom Pass 0";
    /// <summary>
    /// Set to true if your shader will sample in the camera color buffer.
    /// </summary>
    public bool fetchColorBuffer;

    public EdgeOutlineData edgeOutlineData;

    public static class ShaderProps
    {
        public static int isEditor = Shader.PropertyToID("_IsEditor");
        //Styling
        public static int edgeColor = Shader.PropertyToID("_Edge_Color");
        public static int edgeSize = Shader.PropertyToID("_Edge_Size");
        public static int edgeOpacity = Shader.PropertyToID("_Edge_Opacity");
        public static int depthAlphaDebug = Shader.PropertyToID("_DepthAlphaDebug");
        public static int defaultFarPlane = Shader.PropertyToID("_DefualtFarPlane");
        public static int edgeDepthAlpha = Shader.PropertyToID("_Edge_Depth_Alpha");
        public static int backgroundOpacity = Shader.PropertyToID("_Background_Opacity");
        //EdgeDetection
        public static int depthDetectionOn = Shader.PropertyToID("_DepthDetectionOn");
        public static int depthDetectionStepValue = Shader.PropertyToID("_Depth_Detection_Step_Value");
        public static int depthDetectionFadeValue = Shader.PropertyToID("_Depth_Detection_Fade_Value");
        public static int normalDectionOn = Shader.PropertyToID("_NormalDectionOn");
        public static int normalDectectionStepValue = Shader.PropertyToID("_Normal_Detection_Step_Value");
        public static int normalDectectionFadeValue = Shader.PropertyToID("_Normal_Detection_Fade_Value");
    }
    
    //protected override bool executeInSceneView => false;

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // In case there was a pass index assigned, we retrieve the name of this pass
        if (string.IsNullOrEmpty(materialPassName) && fullscreenPassMaterial != null)
            materialPassName = fullscreenPassMaterial.GetPassName(materialPassIndex);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (edgeOutlineData == null) return;
       
        if (fullscreenPassMaterial != null && fullscreenPassMaterial.passCount > 0)
        {
            SetMatProps();
            
            if (fetchColorBuffer)
            {
                ResolveMSAAColorBuffer(ctx.cmd, ctx.hdCamera);
                //reset the render target to the UI
                SetRenderTargetAuto(ctx.cmd);
            }

            //In case the pass name is not found, we fallback on the first one instead of drawing all of them (default behavior with pass id = -1)
            int passIndex = fullscreenPassMaterial.FindPass(materialPassName);
            if (passIndex == -1) passIndex = 0;

            CoreUtils.DrawFullScreen(ctx.cmd, fullscreenPassMaterial, shaderPassId: passIndex);
        }
    }


    private void SetMatProps()
    {
        fullscreenPassMaterial.SetInt(ShaderProps.isEditor, edgeOutlineData.IsEditor ? 1 : 0);
        //Styling
        fullscreenPassMaterial.SetColor(ShaderProps.edgeColor, edgeOutlineData.edgeColor);
        fullscreenPassMaterial.SetFloat(ShaderProps.edgeSize, edgeOutlineData.edgeSize);
        fullscreenPassMaterial.SetFloat(ShaderProps.edgeOpacity, edgeOutlineData.edgeOpacity);
        fullscreenPassMaterial.SetInt(ShaderProps.depthAlphaDebug, edgeOutlineData.depthAlphaDebug ? 1 : 0);
        fullscreenPassMaterial.SetFloat(ShaderProps.defaultFarPlane, edgeOutlineData.defaultFarPlane);
        fullscreenPassMaterial.SetVector(ShaderProps.edgeDepthAlpha, edgeOutlineData.edgeDepthAlpha);
        fullscreenPassMaterial.SetFloat(ShaderProps.backgroundOpacity, edgeOutlineData.backGroundOpacity);
        //EdgeDetection
        fullscreenPassMaterial.SetInt(ShaderProps.depthDetectionOn, edgeOutlineData.depthDetectionOn ? 1 : 0);
        fullscreenPassMaterial.SetFloat(ShaderProps.depthDetectionStepValue, edgeOutlineData.depthDetectionStepValue);
        fullscreenPassMaterial.SetFloat(ShaderProps.depthDetectionFadeValue, edgeOutlineData.depthDetectionFadeDepth);
        fullscreenPassMaterial.SetInt(ShaderProps.normalDectionOn, edgeOutlineData.normalDetectionOn ? 1 : 0);
        fullscreenPassMaterial.SetFloat(ShaderProps.normalDectectionStepValue, edgeOutlineData.normalDetectionStepValue);
        fullscreenPassMaterial.SetFloat(ShaderProps.normalDectectionFadeValue, edgeOutlineData.normalDetectionFadeDepth);
    }

    //--###### 切换过度实现 ##### --
    private float ChangeFloatValueFade(float value, float tempValue) 
    {
        return (value - (value - tempValue) * (1 - fadeValue));
    }

    private Color ChangeColorValueFade(Color value, Color tempValue)
    {
        return Color.Lerp(tempValue, value, fadeValue);
    }
    //-----------------------------

    /// <summary>
    /// List all the materials that need to be displayed at the bottom of the component.
    /// All the materials gathered by this method will be used to create a Material Editor and then can be edited directly on the custom pass.
    /// </summary>
    /// <returns>An enumerable of materials to show in the inspector. These materials can be null, the list is cleaned afterwards</returns>
    public override IEnumerable<Material> RegisterMaterialForInspector() { yield return fullscreenPassMaterial; }

    protected override void Cleanup()
    {
        // Cleanup code
    }
}