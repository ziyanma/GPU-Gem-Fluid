using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour {
    public bool isPlaying = true;

	// Use this for initialization
	void Start () {
		
	}

    public void NextFrame()
    {

    }
	
	// Update is called once per frame
	void Update () {
	    if (isPlaying)
        {
            NextFrame();
        }	
	}
}
