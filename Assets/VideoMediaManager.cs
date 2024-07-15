using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class VideoMediaManager : MonoBehaviour
{

  public GameObject videoPlayerObject;
  public string videoPath;
  // Start is called before the first frame update
  void Start()
  {
    videoPlayerObject = GameObject.Find("Video Player");
  }

  // Update is called once per frame
  void Update()
  {
    Play();
  }

  void Play()
  {

    var videoPlayer = videoPlayerObject.GetComponent<VideoPlayer>();
    if (videoPlayer && videoPlayer.enabled == true && videoPlayer.clip)
    {
      videoPlayer.clip = null;
      videoPlayer.url = videoPath;
      videoPlayer.Prepare();
      videoPlayer.Play();
      Destroy(this.gameObject);
    }
  }
}
