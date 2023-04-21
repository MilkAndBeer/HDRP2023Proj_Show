using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(Animator))]
public class SyncVideoAndAnimtor : MonoBehaviour
{
    VideoPlayer videoPlayer;
    Animator anim;

    bool firstUpdate = true;

    // Start is called before the first frame update
    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        anim = GetComponent<Animator>();

        anim.Play(0);
        videoPlayer.Play();

        // Force a sync of the animation each time the video player loops
        videoPlayer.loopPointReached += Loop;
    }

    void Loop(VideoPlayer vp)
    {
        anim.Play(0, 0, 0.0f);
    }

    // Force a sync of the animation at the first rendered frame
    private void Update()
    {
        if(firstUpdate)
        {
            var t = (float)videoPlayer.time;
            anim.Play(0, 0, t);
            firstUpdate = false;
        }
    }
}
