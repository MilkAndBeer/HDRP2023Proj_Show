using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TestSetInteraveCamData : MonoBehaviour
{
    public List<Material> waterMats = new List<Material>();
    
    private Camera interWaveCam;

    private void Start()
    {
        interWaveCam ??= transform.GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        SetDataToMats();
    }

    private void SetDataToMats()
    {
        if (!interWaveCam) return;
        float camSize = interWaveCam.orthographicSize;
        Vector3 camPos = interWaveCam.transform.position;
        foreach(var mat in waterMats)
        {
            mat.SetVector("_InteraveWaveCenterPos", camPos);
            mat.SetFloat("_InteraveWaterPlaneWith", camSize);
        }
    }

}
