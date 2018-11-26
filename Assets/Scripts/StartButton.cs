using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartButton : MonoBehaviour {
    public AnimationController ac;

    // Use this for initialization
    void Start () {
        GetComponent<Button>().onClick.AddListener(() => ac.isPlaying = !ac.isPlaying);

    }

    // Update is called once per frame
    void Update () {
		
	}
}
