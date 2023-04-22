using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkChangeController : MonoBehaviour
{
    public GameObject showObjParentObj;         //��������ʾ�����������1
    public GameObject branchLinkObj;            //�����������
    public GameObject branchMaskObj;            //��������

    //-------------------------------------------
    private bool linkStatus = false;
    private BlackLinkController blackLinkController;
    private BranchLinkEventListener branchLinkMaskEventListener;
    private LinkSceneControlLoad controlSceneLoad;

    //---------------- Test Data ------------------
    private string linkBranchWorldName = "LinkChangeWorld002";
    //---------------- Test Over ------------------

    //Layers
    private const string layerJingZhiMask = "JingZhiMask";          //����ʱ�ɼ���
    private const string layerEffectBlack = "BranchBlackWorld";     //���Ⱥ�ɼ��㣬����ǰ���ɼ���

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
    /// �����֦
    /// </summary>
    private void GetInBranchWorld() 
    {
        //1���л��ĺ��򶯻�����
        //2��Layer�л�
        //��Ч�����ر�
        blackLinkController.AddShowObj(showObjParentObj, layerJingZhiMask, isWithOutName: true, withOutName: "Ef_Branch_PointLight");
        blackLinkController.EnterBlackLinkWorld(branchMaskObj);
        controlSceneLoad.LoadSceneAdd(linkBranchWorldName, delegate {
            blackLinkController.BlackLinkWorldLoaded();
        });
    }
    /// <summary>
    /// �뿪��֦
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
