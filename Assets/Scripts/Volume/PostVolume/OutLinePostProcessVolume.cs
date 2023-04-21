using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/OutLinePostProcessVolume")]
public sealed class OutLinePostProcessVolume : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedIntParameter scale = new ClampedIntParameter(1, 1, 6);
    public FloatParameter depthThreshold = new FloatParameter(0.2f);
    public FloatParameter normalThreshold = new FloatParameter(0.4f);
    public FloatParameter depthNormalThreshold = new FloatParameter(0.2f);
    public FloatParameter depthNormalThresholdScale = new FloatParameter(1f);
    public ColorParameter edgeColor = new ColorParameter(Color.white);

    Material m_Material;

    public bool IsActive() => m_Material != null && intensity.value > 0f;

    // Do not forget to add this post process in the Custom Post Process Orders list (Project Settings > Graphics > HDRP Global Settings).
    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterOpaqueAndSky;

    const string kShaderName = "Hidden/Shader/OutLinePostProcessVolume";

    public override void Setup()
    {
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume OutLinePostProcessVolume is unable to load.");
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        Matrix4x4 clipToView = GL.GetGPUProjectionMatrix(camera.camera.projectionMatrix, true).inverse;

        m_Material.SetFloat("_Intensity", intensity.value);
        m_Material.SetTexture("_MainTex", source);
        m_Material.SetInt("_Scale", scale.value);
        m_Material.SetFloat("_DepthThreshold", depthThreshold.value);
        m_Material.SetFloat("_NormalThreshold", normalThreshold.value);
        m_Material.SetFloat("_DepthNormalThreshold", depthNormalThreshold.value);
        m_Material.SetFloat("_DepthNormalThresholdScale", depthNormalThresholdScale.value);
        m_Material.SetMatrix("_ClipToView", clipToView);
        m_Material.SetColor("_EdgeColor", edgeColor.value);

        HDUtils.DrawFullScreen(cmd, m_Material, destination, shaderPassId: 0);
    }

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
