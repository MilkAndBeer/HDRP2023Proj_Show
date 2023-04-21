
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetRotMove : MonoBehaviour
{
    public Transform targetTran;

    private void Start()
    {
        Transform spot1 = targetTran;
        Transform spot2 = targetTran.GetChild(0);
        Transform spot3 = spot2.GetChild(0);
        Transform spot4 = spot3.GetChild(0);

        Vector3 spot1Rot = spot1.localEulerAngles;
        //this.transform.rotation = spot1.localRotation * spot2.localRotation * spot3.localRotation * spot4.localRotation;
        this.transform.rotation = spot1.localRotation;
        this.transform.Translate(spot2.localPosition, Space.Self);
        this.transform.rotation *= spot2.localRotation;
        this.transform.Translate(spot3.localPosition, Space.Self);
        this.transform.rotation *= spot3.localRotation;
        this.transform.Translate(spot4.localPosition, Space.Self);
        this.transform.rotation *= spot4.localRotation;
    }
}
