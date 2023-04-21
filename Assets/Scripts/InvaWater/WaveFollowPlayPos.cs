using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class WaveFollowPlayPos : MonoBehaviour
{
    public Transform followTrans;
    private Vector3 latePos;

    private bool waveShow = true;
    private Transform waterWave;
    private InteractiveShaderRT shaderRT;
    private MeshRenderer targetRender;

    private float waitTime = 1;
    private float startTime = 0;
    private bool stopWave = false;

    private void Start()
    {
        latePos = followTrans.position;
        waterWave = transform.GetChild(1);
        waitTime = waterWave.GetComponent<ParticleSystem>().main.startLifetime.constantMax * 2;

    }

    public void OnDestroy()
    {
    }

    public void InitRender(MeshRenderer render)
    {
        if (targetRender != render)
        {
            targetRender = render;
            if (shaderRT == null)
                shaderRT = transform.GetComponentInChildren<InteractiveShaderRT>();
            if (shaderRT != null)
            {
                MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                shaderRT.InitMat(materialPropertyBlock, render);
            }
        }
    }

    private void Update()
    {
        if (followTrans != null)
        {
            transform.position = followTrans.position;
        }

        //HideInStop();

        latePos = followTrans.position;
    }

    private void HideInStop()
    {
        waveShow = (followTrans.position - latePos).magnitude > 0.01f;
        if (waveShow)
        {
            stopWave = false;
            waterWave.localScale = Vector3.one;
        }
        else
        {
            if (!stopWave)
            {
                startTime = Time.realtimeSinceStartup;
                stopWave = true;
            }
            if (Time.realtimeSinceStartup + waitTime > startTime)
            {
                if (waterWave.localScale.x > 0.5f)
                    waterWave.localScale -= Vector3.one * .025f;
                else
                    waterWave.localScale = Vector3.one * .5f;
            }
        }
    }
}
