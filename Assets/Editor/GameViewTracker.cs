using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameViewTracker
{
    [MenuItem("Tools/CToolsPackage/GameViewTracker _%#M", true)]
    public static bool ToggleGameViewTrackingValidate()
    {
        Menu.SetChecked("GameViewTracker", s_Enabled);
        return true;
    }

    [MenuItem("Tools/CToolsPackage/GameViewTracker _%#M")]
    public static void ToggleGameViewTracking()
    {
        SetEnabled(!s_Enabled);
    }

    static void SetEnabled(bool enabled)
    {
        if (enabled && !s_Enabled)
        {
            SceneView.duringSceneGui += sceneGUICallback;
            s_Enabled = true;
        }
        else if (!enabled && s_Enabled)
        {
            SceneView.duringSceneGui -= sceneGUICallback;
            s_Enabled = false;
        }
    }

    static void sceneGUICallback(SceneView s)
    {
        s = SceneView.lastActiveSceneView;
        if (Camera.main == null)
            return;
        if (!s.camera.orthographic)
        {
            Camera.main.transform.SetPositionAndRotation(s.camera.transform.position - 0.1f * s.camera.transform.forward, s.camera.transform.rotation);
            Camera[] cameraArr = SceneView.GetAllSceneCameras();
            for (int i = 0; i < cameraArr.Length; i++)
            {
                if (cameraArr[i] != s.camera)
                {
                    SceneView tempSceneView = SceneView.sceneViews[i] as SceneView;
                    tempSceneView.pivot = s.pivot;
                    tempSceneView.rotation = s.rotation;
                    tempSceneView.Repaint();
                }
            }
        }
    }

    static bool s_Enabled;
}