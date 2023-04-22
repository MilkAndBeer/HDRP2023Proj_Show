using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class LinkSceneControlLoad : MonoBehaviour
{
    private Action loadSceneEvent;
    private Action unloadSceneEvent;

    /// <summary>
    /// ���س���
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="loadScene"></param>
    public void LoadSceneAdd(string sceneName, Action loadScene = null)
    {
        loadSceneEvent = loadScene;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }
    /// <summary>
    /// ���س����ص�
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("OnSceneLoaded: " + scene.name);
        if(loadSceneEvent != null)
        {
            loadSceneEvent.Invoke();
            loadSceneEvent = null;
        }
    }

    /// <summary>
    /// ж�س���
    /// </summary>
    /// <param name="sceneName"></param>
    /// <param name="loadScene"></param>
    public void UnLoadScene(string sceneName, Action unloadScene = null)
    {
        unloadSceneEvent = unloadScene;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.UnloadSceneAsync(sceneName);
    }
    /// <summary>
    /// ж�س����ص�
    /// </summary>
    /// <param name="scene"></param>
    private void OnSceneUnloaded(Scene scene)
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Debug.Log("OnSceneUnloaded: " + scene.name);
        if (unloadSceneEvent != null)
        {
            unloadSceneEvent.Invoke();
            unloadSceneEvent = null;
        }
    }
}
