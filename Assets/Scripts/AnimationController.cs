using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour {
    public bool isPlaying = false;

	// Use this for initialization
	void Start () {
		
	}

    public void NextFrame(float dt)
    {
        Debug.Log(dt);
    }
	
	// Update is called once per frame
	protected void Update () {
	    if (isPlaying)
        {
            NextFrame(Time.deltaTime);
        }	
	}
}
