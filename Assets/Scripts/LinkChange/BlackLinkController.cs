using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class BlackLinkController : MonoBehaviour
{
    private GameObject branchLinkObj;
    private Transform customVolumeTran;
    private CustomPassVolume passVolume;
    private Camera mainCam;
    private Camera blackCam;
    //显示物体的队列
    private Dictionary<string, List<GameObject>> showObjsDic = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, List<int>> showLayerDic = new Dictionary<string, List<int>>();

    private bool includeChild = false;

    private Animator transferAnim;

    //--用于判断动画播放状态
    private bool isOpenAnimMid = false;
    private bool isOpenAnimOver = false;
    private bool isCloseAnimMid = false;
    private bool isCloseAnimOver = false;

    private const string openMidAnimName = "Ef_Arrisa_BranchLink_OpenMid";
    private const string openOverAnimName = "Ef_Arrisa_BranchLink_OpenOver";
    private const string closeMidAnimName = "Ef_Arrisa_BranchLink_CloseMid";
    private const string closeOverAnimName = "Ef_Arrisa_BranchLink_CloseOver";
    private const string branchAnimName = "blackLinkWork";
    //剔除金枝上的水
    private const string branchWaterName = "Ef_Branch_BlackWater";
    //Layer
    private const string branchLayerName = "BranchBlackWorld";

    /// <summary>
    /// 进入黑球场景
    /// </summary>
    public void EnterBlackLinkWorld(GameObject transferObj, bool includeChild = false)
    {
        if (transferObj != null)
        {
            branchLinkObj = transferObj;
            transferObj.SetActive(true);
            
            transferAnim ??= transferObj.GetComponent<Animator>();
            customVolumeTran ??= GameObject.Find("Manager/CustomJingZhiViewVolume").transform;
            passVolume = customVolumeTran.GetComponent<CustomPassVolume>();
        }
        this.includeChild = includeChild;
        if(Camera.main.transform == null)
        {
            Debug.Log("MainCamera Not find!");
            return;
        }

        //Layer Change
        foreach (var key in showObjsDic.Keys)
        {
            List<GameObject> showObjsList = showObjsDic[key];

            for (int i = 0; i < showObjsList.Count; i++)
            {
                GameObject obj = showObjsList[i];
                int layerIndex = LayerMask.NameToLayer(key);
                SetLayerAll(obj, layerIndex, includeChild);
            }
        }

        //Pass Worked
        if (passVolume && passVolume.customPasses.Count > 0)
        {
            passVolume.customPasses[0].enabled = true;
            passVolume.gameObject.layer = LayerMask.NameToLayer("Default");
        }

        //Open Mid
        transferAnim.Play(openMidAnimName);
    }

    /// <summary>
    /// 金枝世界加载完毕时调用
    /// </summary>
    public void BlackLinkWorldLoaded()
    {
        //--## 多金枝监听事件需要防空判断
        if (!transferAnim) return;

        if (isOpenAnimMid)
        {
            //Anim
            transferAnim.Play(openOverAnimName);
            isOpenAnimMid = false;
        }
        else
        {
            isOpenAnimOver = true;
        }
    }

    /// <summary>
    /// 退出黑球场景
    /// </summary>
    public void LeaveBlackLinkWorld()
    {
        //--## 多金枝监听事件需要防空判断
        if (!transferAnim) return;
        
        if (passVolume && passVolume.customPasses.Count > 0)
            passVolume.customPasses[0].enabled = true;

        //Anim
        transferAnim.Play(closeMidAnimName);
    }

    private void Update()
    {
        //--## 多金枝监听事件需要防空判断
        if (!transferAnim) return;

        //保底动画播放
        if (isOpenAnimMid && isOpenAnimOver)
        {
            transferAnim.Play(openOverAnimName);
            isOpenAnimMid = false;
            isOpenAnimOver = false;
        }
    }

    private void CamShowChanged( bool isEnterBlack)
    {
        if (blackCam == null || mainCam == null)
        {
            //Camera change
            mainCam = Camera.main;
            GameObject blackCamObj = GameObject.Find("TransCam");
            if(blackCamObj)
                blackCam = blackCamObj.transform.GetComponent<Camera>();
        }
        if (blackCam != null && mainCam != null)
        {
            //CameraLayer Setting
            mainCam.enabled = !isEnterBlack;
            blackCam.enabled = isEnterBlack;
        }


        if (passVolume && passVolume.customPasses.Count > 0)
            passVolume.gameObject.layer = LayerMask.NameToLayer(isEnterBlack? branchLayerName : "Default");
    }

    //----- Mask Animation ---------
    public void BlackMaskOpenAnimMid()
    {
        //黑球遮罩进入完毕
        isOpenAnimMid = true;
        CamShowChanged(true);

        //--##处理特殊显示层切换--
        GameObject groundPlane = GameObject.Find("GroundPlane");
        if (groundPlane)
        {
            groundPlane.layer = LayerMask.NameToLayer(branchLayerName);
        }
    }
    public void BlackMaskOpenAnimOver()
    {
        if (passVolume && passVolume.customPasses.Count > 0)
            passVolume.customPasses[0].enabled = false;
    }
    public void BlackMaskCloseAnimMid()
    {
        //黑球遮罩进入完毕
        EventDispatcher.TriggerEvent(GameEvent.UnLoadBranchWorld);
        CamShowChanged(false);

        transferAnim.Play(closeOverAnimName);
        //--##处理特殊显示层切换--
        GameObject groundPlane = GameObject.Find("GroundPlane");
        if (groundPlane)
        {
            groundPlane.layer = LayerMask.NameToLayer("Default");
        }
    }
    public void BlackMaskCloseAnimOver()
    {
        foreach (var key in showObjsDic.Keys)
        {
            List<GameObject> showObjsList = showObjsDic[key];
            List<int> showLayerList = showLayerDic[key];

            if (showLayerList.Count != showObjsList.Count) return;
            for (int i = 0; i < showObjsList.Count; i++)
                SetLayerAll(showObjsList[i], showLayerList[i], includeChild);
            showLayerList.Clear();
            showObjsList.Clear();
        }
        transferAnim = null;
        if (passVolume && passVolume.customPasses.Count > 0)
            passVolume.customPasses[0].enabled = false;

        //## 关闭晶值遮罩
        ClearObjs();
        if(branchLinkObj)
        {
            branchLinkObj.SetActive(false);
            branchLinkObj = null;
        }
    }
    //------------------------------

    /// <summary>
    /// 添加显示物体
    /// </summary>
    public void AddShowObj(GameObject obj, string groupName, bool onlyMesh = false, 
        bool isWithOutName = false, string withOutName = "")
    {
        if (obj == null) return;
        if (showObjsDic == null) return;

        List<GameObject> showObjsList;
        List<int> showLayerList;
        if (!showObjsDic.ContainsKey(groupName))
        {
            showObjsList = new List<GameObject>();
            showLayerList = new List<int>();
            showObjsDic.Add(groupName, showObjsList);
            showLayerDic.Add(groupName, showLayerList);
        }
        else
        {
            showObjsList = showObjsDic[groupName];
            showLayerList = showLayerDic[groupName];
        }

        if (onlyMesh) GetAllMeshRender(obj, showObjsList, showLayerList, isWithOutName, withOutName);
        else GetAllObj(obj, showObjsList, showLayerList, isWithOutName, withOutName);
    }
    public void AddShowObj(List<Renderer> meshRenderers, string groupName)
    {
        if (meshRenderers == null) return;
        for(int i = 0; i < meshRenderers.Count; i++)
        {
            GameObject tempObj = meshRenderers[i].gameObject;
            List<GameObject> showObjsList;
            List<int> showLayerList;
            if (!showObjsDic.ContainsKey(groupName))
            {
                showObjsList = new List<GameObject>();
                showLayerList = new List<int>();
                showObjsDic.Add(groupName, showObjsList);
                showLayerDic.Add(groupName, showLayerList);
            }
            else
            {
                showObjsList = showObjsDic[groupName];
                showLayerList = showLayerDic[groupName];
            }
            showObjsList.Add(tempObj);
            showLayerList.Add(tempObj.layer);
        }
    }
    /// <summary>
    /// 移除显示物体
    /// </summary>
    /// <param name="obj"></param>
    public void RemoveShowObj(GameObject obj)
    {
        if (obj == null) return;
        if (showObjsDic == null) return;

        foreach (var key in showObjsDic.Keys)
        {
            List<GameObject> showObjsList = showObjsDic[key];
            List<int> showLayerList = showLayerDic[key];

            if (showLayerList.Count != showObjsList.Count) return;
            for (int i = 0; i < showObjsList.Count; i++)
            {
                if (showObjsList[i] == obj)
                {
                    showObjsList.RemoveAt(i);
                    showLayerList.RemoveAt(i);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 清楚所有显示物体
    /// </summary>
    public void ClearObjs()
    {
        showObjsDic.Clear();
        showLayerDic.Clear();
    }

    private void GetAllObj(GameObject obj, List<GameObject> showObjsList, List<int> showLayerList, bool isWithOut, string withOutName)
    {
        //排除特殊物体
        if (isWithOut && obj.name == withOutName)
        {
            return;
        }

        showObjsList.Add(obj);
        showLayerList.Add(obj.layer);
        if (obj.transform.childCount <= 0) return;
        for(int i = 0; i < obj.transform.childCount; i++)
        {
            GetAllObj(obj.transform.GetChild(i).gameObject, showObjsList, showLayerList, isWithOut, withOutName);
        }
    }

    private void GetAllMeshRender(GameObject obj, List<GameObject> showObjsList, List<int> showLayerList, bool isWithOut, string withOutName)
    {
        //排除特殊物体
        if (isWithOut && obj.name == withOutName) return;

        if (obj.GetComponent<MeshRenderer>())
        {
            showObjsList.Add(obj);
            showLayerList.Add(obj.layer);
        }
        if (obj.transform.childCount > 0)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Transform tempTran = obj.transform.GetChild(i);
                GetAllMeshRender(tempTran.gameObject, showObjsList, showLayerList, isWithOut, withOutName);
            }
        }
    }

    private void SetLayerAll(GameObject obj, int layer, bool includeChild)
    {
        if (!obj) return;
        //剔除水
        if (!obj.transform.parent || obj.transform.parent.name == branchWaterName) return;
        
        obj.layer = layer;
        if (obj.transform.childCount <= 0 || !includeChild) return;
        int childCount = obj.transform.childCount;
        for (int i = 0; i < childCount; i++)
            SetLayerAll(obj.transform.GetChild(i).gameObject, layer, includeChild);
    }
}
