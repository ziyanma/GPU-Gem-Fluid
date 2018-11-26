using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSimulation : MonoBehaviour {
    public Material mat;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        // Set up shader
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
    }

    void OnPreRender()
    {
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
    }
}
