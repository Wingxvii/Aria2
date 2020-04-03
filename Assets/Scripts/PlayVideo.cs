using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PlayVideo : MonoBehaviour
{
    public VideoPlayer video;

    private void Start()
    {
        video.loopPointReached += Ended;
        video = this.GetComponent<VideoPlayer>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            video.Stop();
            GameSceneController.Instance.SwapScene(2);
        }

    }

    void Ended(UnityEngine.Video.VideoPlayer vp) {
        video.Stop();
        GameSceneController.Instance.SwapScene(2);
    }
}
