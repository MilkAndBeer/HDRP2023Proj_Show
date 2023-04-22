using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BranchLinkEventListener : MonoBehaviour
{
    public Action maskShowOpenMidAction;
    public Action maskShowOpenOverAction;
    public Action maskShowCloseMidAction;
    public Action maskShowCloseOverAction;

    public void InitEvent(Action callBackOpenMid, Action callBackOpenOver, Action callBackCloseMid, Action callBackCloseOver)
    {
        maskShowOpenMidAction = callBackOpenMid;
        maskShowOpenOverAction = callBackOpenOver;
        maskShowCloseMidAction = callBackCloseMid;
        maskShowCloseOverAction = callBackCloseOver;
    }

    public void MaskAnimationOpenMid()
    {
        maskShowOpenMidAction.Invoke();
    }
    public void MaskAnimationOpenOver()
    {
        maskShowOpenOverAction.Invoke();
    }
    public void MaskAnimationCloseMid()
    {
        maskShowCloseMidAction.Invoke();
    }
    public void MaskAnimationCloseOver()
    {
        maskShowCloseOverAction.Invoke();
    }

    public void OnDestroy()
    {
        maskShowOpenMidAction = null;
        maskShowOpenOverAction = null;
        maskShowCloseMidAction = null;
        maskShowCloseOverAction = null;
    }

}