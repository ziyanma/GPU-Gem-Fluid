using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour {
    public bool isPlaying = false;

	// Use this for initialization
	void Start () {
		
	}

    public virtual void NextFrame(float dt)
    {
        Debug.Log(dt);
    }
	
	// Update is called once per frame
	protected virtual void Update () {
	    if (isPlaying)
        {
            NextFrame(0.1f);
        }
	}
}
