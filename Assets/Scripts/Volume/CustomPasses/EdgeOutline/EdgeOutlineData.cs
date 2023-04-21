using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(fileName = "EdgeOutlineData", menuName = "Rendering/CustomData/EdgeOutlineData")]
public class EdgeOutlineData : ScriptableObject
{
    public bool IsEditor = false;
    //Styling
    public Color edgeColor = new Color(0,0,0,1);
    public float edgeSize = 1;
    public float edgeOpacity = 2;
    public bool depthAlphaDebug = false;
    public float defaultFarPlane = 5;
    public Vector2 edgeDepthAlpha = new Vector2(0, 5);
    public float backGroundOpacity = 0;
    //Edge Detection
    public bool depthDetectionOn = false;
    [Range(0, 1f)]
    public float depthDetectionStepValue = 0.1f;
    [Range(0, 1f)]
    public float depthDetectionFadeDepth = 0.7f;
    public bool normalDetectionOn = false;
    [Range(0, 1f)]
    public float normalDetectionStepValue = 0.1f;
    [Range(0, 1f)]
    public float normalDetectionFadeDepth = 0.7f;
}