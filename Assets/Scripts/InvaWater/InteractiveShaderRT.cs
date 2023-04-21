using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractiveShaderRT : MonoBehaviour
{
    [SerializeField]
    public Transform m_Target;
    [SerializeField]
    public Camera m_Camera;
    [SerializeField]
    public float waveHeight = 1;

    public MaterialPropertyBlock targetMat;
    private MeshRenderer targetRender;

    private void Start()
    {
        if (targetMat == null || !targetRender) return;
        InitMat();
    }

    private void FixedUpdate()
    {

    }

    public void SetMatDatas(RTHandle targetBuffer)
    {
        if (targetMat == null || !targetRender) return;
        transform.position = new Vector3(m_Target.transform.position.x, transform.position.y, m_Target.transform.position.z);
        targetMat.SetVector("_InteraveWaveCenterPos", transform.position);
        targetMat.SetTexture("_InteraveWaterTex", targetBuffer);
        targetMat.SetFloat("_InteraveWaterPlaneWith", m_Camera.orthographicSize);
        targetRender.SetPropertyBlock(targetMat);
    }


    private void OnEnable()
    {
        if (targetMat != null && targetRender)
        {
            targetMat.SetFloat("_InteraveWaveOn", 1);
            targetRender.SetPropertyBlock(targetMat);
        }
    }
    private void OnDisable()
    {
        if (targetMat != null && targetRender)
        {
            targetMat.SetFloat("_InteraveWaveOn", 0);
            targetRender.SetPropertyBlock(targetMat);
        }
    }

    private void OnDestroy()
    {

        if (targetMat != null && targetRender)
        {
            targetMat.SetFloat("_InteraveWaveOn", 0);
            targetRender.SetPropertyBlock(targetMat);
        }
    }

    public void InitMat(MaterialPropertyBlock materialPropertyBlock, MeshRenderer render)
    {
        if (targetRender == render) return;
        ReleaseMat();
        targetMat = materialPropertyBlock;
        targetRender = render;
        InitMat();
    }

    private void ReleaseMat()
    {
        if (targetMat != null)
        {
            targetMat.SetFloat("_InteraveWaveOn", 0);
            targetMat.SetFloat("_InteraveWaterPlaneWith", 1);
            targetRender.SetPropertyBlock(targetMat);
        }
    }

    private void InitMat()
    {
        if(targetMat != null)
        {
            targetMat.SetFloat("_InteraveWaveOn", 1);
            targetMat.SetFloat("_InteraveWaterPlaneWith", m_Camera.orthographicSize);
            targetRender.SetPropertyBlock(targetMat);
        }
    }
}
