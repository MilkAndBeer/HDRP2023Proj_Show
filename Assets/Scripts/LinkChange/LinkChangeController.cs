using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkChangeController : MonoBehaviour
{
    public GameObject showObjParentObj;         //过渡能显示的物体测试组1
    public GameObject branchLinkObj;            //触摸火点物体
    public GameObject branchMaskObj;            //遮罩物体

    //-------------------------------------------
    private bool linkStatus = false;
    private BlackLinkController blackLinkController;
    private BranchLinkEventListener branchLinkMaskEventListener;
    private LinkSceneControlLoad controlSceneLoad;

    //---------------- Test Data ------------------
    private string linkBranchWorldName = "LinkChangeWorld002";
    //---------------- Test Over ------------------

    //Layers
    private const string layerJingZhiMask = "JingZhiMask";          //过渡时可见层
    private const string layerEffectBlack = "BranchBlackWorld";     //过度后可见层，过度前不可见层

    private void Start()
    {
        controlSceneLoad = transform.GetComponent<LinkSceneControlLoad>();
        if (branchLinkObj)
        {
            blackLinkController = branchLinkObj.GetComponent<BlackLinkController>();
            if (branchMaskObj)
            {
                branchLinkMaskEventListener = branchMaskObj.GetComponent<BranchLinkEventListener>();
                branchLinkMaskEventListener.InitEvent(blackLinkController.BlackMaskOpenAnimMid, blackLinkController.BlackMaskOpenAnimOver,
                                blackLinkController.BlackMaskCloseAnimMid, blackLinkController.BlackMaskCloseAnimOver);
            }
        }
        
        InitListener();
    }

    public void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space) && !linkStatus)
        //{
        //    linkStatus = true;
        //    GetInBranchWorld();
        //}

        //if (Input.GetKeyDown(KeyCode.Escape) && linkStatus)
        //{
        //    linkStatus = false;
        //    GetOutBranchWorld();
        //}

        if (Input.GetKeyDown(KeyCode.Space))
        {
            linkStatus = !linkStatus;
            if(linkStatus) GetInBranchWorld(); 
            else GetOutBranchWorld();
        }
    }

    public void OnDestroy()
    {
        RemoveListener();
    }

    #region Listener
    private void InitListener()
    {
        EventDispatcher.RegEventListener(GameEvent.UnLoadBranchWorld, UnLoadBranchWorld);
    }

    private void RemoveListener()
    {
        EventDispatcher.UnRegEventListener(GameEvent.UnLoadBranchWorld, UnLoadBranchWorld);
    }
    #endregion

    /// <summary>
    /// 进入金枝
    /// </summary>
    private void GetInBranchWorld() 
    {
        //1、切换的黑球动画播放
        //2、Layer切换
        //后效开启关闭
        blackLinkController.AddShowObj(showObjParentObj, layerJingZhiMask, isWithOutName: true, withOutName: "Ef_Branch_PointLight");
        blackLinkController.EnterBlackLinkWorld(branchMaskObj);
        controlSceneLoad.LoadSceneAdd(linkBranchWorldName, delegate {
            blackLinkController.BlackLinkWorldLoaded();
        });
    }
    /// <summary>
    /// 离开金枝
    /// </summary>
    private void GetOutBranchWorld()
    {
        blackLinkController.LeaveBlackLinkWorld();
    }
    
    private void UnLoadBranchWorld()
    {
        controlSceneLoad.UnLoadScene(linkBranchWorldName);
    }
}
