using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

class RippleWavePass : CustomPass
{
    public List<Material> waterMats;

    public List<Camera> bakingCameraList;
    public bool render = true;
    public List<Transform> hitTransList;
    [Range(0, 1f)]
    public float drawRadius = 0.2f;
    public float rippleSpeed = 1f;
    [Range(-0.99f, 1)]
    public float rippleLifeTime = 0f;

    public Shader drawShader;
    public Material drawMat;
    public Shader rippleShader; //涟漪计算Shader
    public Material rippleMat;

    private RenderTexture prevRT;   //上一帧
    private RenderTexture currentRT; //当前帧
    private RenderTexture tempRT; //临时RT
    private string rippleWaveTexName = "_RippleWave";
    private string rippleCamPosName = "_RippleCamPos";
    private string rippleCamSizeName = "_RippleCamSize";

    //RippleShader变量控制
    private string rippleSpeedName = "_RippleSpeed";
    private string rippleLifeTimeName = "_RippleLifeTime";
    private int TextureSize = 4096;

    [HideInInspector]
    public class BakingCamData
    {
        public Vector3 bakingPos; // 上一帧的位置
        public float orthographicSize;
    }

    private BakingCamData lastCamData;
    private BakingCamData currentCamData;
    private Dictionary<Transform, Vector3> hitTransLastPosDic = new Dictionary<Transform, Vector3>();

    protected override bool executeInSceneView => false;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        prevRT = CreateRT();
        currentRT = CreateRT();
        tempRT = CreateRT();

        if (!drawMat)
            drawMat = new Material(drawShader);

        Camera bakingCam = bakingCameraList[0];
        currentCamData = new BakingCamData();
        currentCamData.bakingPos = bakingCam.transform.position;
        currentCamData.orthographicSize = bakingCam.orthographicSize;
        lastCamData = currentCamData;

        RefreshCamData(bakingCam);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (!render || bakingCameraList == null || bakingCameraList.Contains(ctx.hdCamera.camera) || ctx.hdCamera.camera.cameraType == CameraType.SceneView)
            return;

        //绘制图案
        GetRipple(tempRT);
        foreach (var hitTrans in hitTransList)
        {
            AutoChooseBakingCam(hitTrans.position);
            //绘制条件判断
            bool isDrawAT = false;
            //判断是否为CamSize内
            Vector3 disPos = hitTrans.position - currentCamData.bakingPos;
            if (Mathf.Abs(disPos.x) <= currentCamData.orthographicSize 
                && Mathf.Abs(disPos.z) <= currentCamData.orthographicSize)
            {
                //判断是否为一直移动
                if (hitTransLastPosDic.ContainsKey(hitTrans))
                {
                    Vector3 lastPos = hitTransLastPosDic[hitTrans];
                    if (lastPos != hitTrans.position)
                    {
                        isDrawAT = true;
                        hitTransLastPosDic[hitTrans] = hitTrans.position;
                    }
                }
                else
                {
                    isDrawAT = true;
                    hitTransLastPosDic.Add(hitTrans, hitTrans.position);
                }
            }
            //进行输入点绘制
            if (isDrawAT)
            {
                Vector2 uvPos = GetUVPos(hitTrans);
                DrawAT(uvPos.x, uvPos.y, drawRadius);
            }
        }
        //Shader.SetGlobalTexture(interWaveTex, currentRT);
        SetDataToMats();
    }

    protected override void Cleanup()
    {
        hitTransLastPosDic.Clear();
    }

    public void AddHitTransList(Transform hitTrans)
    {
        if (!hitTransList.Contains(hitTrans))
            hitTransList.Add(hitTrans);
    }
    public void RemoveHitTransList(Transform hitTrans)
    {
        if (hitTransList.Contains(hitTrans))
            hitTransList.Remove(hitTrans);
    }


    private void SetDataToMats()
    {
        Vector3 camPos = currentCamData.bakingPos;
        float camSize = currentCamData.orthographicSize;
        foreach (var mat in waterMats)
        {
            mat.SetVector(rippleCamPosName, camPos);
            mat.SetFloat(rippleCamSizeName, camSize);
            mat.SetTexture(rippleWaveTexName, currentRT);
        }
    }

    /// <summary>
    /// 自动定位当前最近相机
    /// </summary>
    /// <param name="moveTransPos"></param>
    private void AutoChooseBakingCam(Vector3 moveTransPos)
    {
        if (bakingCameraList.Count <= 0) return;
        Camera chooseCam = bakingCameraList[0];
        float minDis = (chooseCam.transform.position - moveTransPos).sqrMagnitude;
        foreach (var tempCam in bakingCameraList)
        {
            float tempDis = (tempCam.transform.position - moveTransPos).sqrMagnitude;
            if (tempDis < minDis)
            {
                minDis = tempDis;
                chooseCam = tempCam;
            }
        }
        RefreshCamData(chooseCam);
    }

    /// <summary>
    /// 刷新当前使用的相机参数
    /// </summary>
    /// <param name="bakingCam"></param>
    private void RefreshCamData(Camera bakingCam)
    {
        if (currentCamData.bakingPos == bakingCam.transform.position &&
            currentCamData.orthographicSize == bakingCam.orthographicSize)
            return;
        lastCamData = currentCamData;
        BakingCamData bakingCamData = new BakingCamData();
        bakingCamData.bakingPos = bakingCam.transform.position;
        bakingCamData.orthographicSize = bakingCam.orthographicSize;
        currentCamData = bakingCamData;
    }

    /// <summary>
    /// 相机不移动情况
    /// </summary>
    /// <param name="hitTrans"></param>
    /// <returns></returns>
    private Vector2 GetUVPos(Transform hitTrans)
    {
        Vector3 tempPos = hitTrans.position - currentCamData.bakingPos;
        float tempSize = currentCamData.orthographicSize;
        if (tempPos.x > tempSize)
        {
            tempPos.x = tempSize;
        }
        else if (tempPos.x < -tempSize)
        {
            tempPos.x = -tempSize;
        }
        if (tempPos.z > tempSize)
        {
            tempPos.z = tempSize;
        }
        else if (tempPos.z < -tempSize)
        {
            tempPos.z = -tempSize;
        }

        float tempU = tempPos.x / (tempSize * 2) + .5f;
        float tempV = tempPos.z / (tempSize * 2) + .5f;

        //相机切换时位移UV
        float moveU = 0f;
        float moveV = 0f;
        //if (currentCamData.bakingPos != lastCamData.bakingPos)
        //{
        //    Vector3 tempMovePos = currentCamData.bakingPos - lastCamData.bakingPos;
        //    moveU = tempMovePos.x / (tempSize * 2);
        //    moveV = tempMovePos.z / (tempSize * 2);
        //}

        return new Vector2(tempU + moveU, tempV + moveV);
    }

    /// <summary>
    /// 创建RT
    /// </summary>
    /// <returns></returns>
    private RenderTexture CreateRT()
    {
        RenderTexture rt = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
    /// <summary>
    /// 交互波绘制
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="radius">半径</param>
    private void DrawAT(float x, float y, float radius)
    {
        drawMat.SetTexture("_SourceTex", currentRT);
        drawMat.SetVector("_Pos", new Vector4(x, y, radius, 0));

        Graphics.Blit(null, tempRT, drawMat);
        RenderTexture rt = tempRT;
        tempRT = currentRT;
        currentRT = rt;
    }
    /// <summary>
    /// 计算涟漪
    /// </summary>
    private void GetRipple(RenderTexture tempRT)
    {
        if (!rippleMat)
            rippleMat = new Material(rippleShader);

        rippleMat.SetTexture("_prevRT", prevRT);
        rippleMat.SetTexture("_currentRT", currentRT);
        rippleMat.SetFloat(rippleSpeedName, rippleSpeed);
        rippleMat.SetFloat(rippleLifeTimeName, rippleLifeTime);
        
        Graphics.Blit(null, tempRT, rippleMat);
        //Change TO currentRT
        Graphics.Blit(tempRT, prevRT);
        RenderTexture rt = prevRT;
        prevRT = currentRT;
        currentRT = rt;
    }
}