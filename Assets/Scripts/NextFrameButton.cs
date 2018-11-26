using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NextFrameButton : MonoBehaviour {
    public AnimationController ac;
	
    // Use this for initialization
	void Start () {
        GetComponent<Button>().onClick.AddListener(() => ac.NextFrame(0.1f));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
