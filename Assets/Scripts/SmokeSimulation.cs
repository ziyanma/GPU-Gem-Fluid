using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSimulation : AnimationController {
    public Material mat;

    public Texture3D [] mTexture;

    public Vector3Int texRes;

	// Use this for initialization
	void Start () {
        mTexture = new Texture3D[2];
        mTexture[0] = new Texture3D(texRes[0], texRes[0], texRes[0], TextureFormat.Alpha8, true); // To Alpha 8
        mTexture[1] = new Texture3D(texRes[0], texRes[0], texRes[0], TextureFormat.Alpha8, true);
    }

    void SwapBuffer (Texture3D [] swap)
    {
        Texture3D texture = swap[0];
        swap[0] = swap[1];
        swap[1] = texture;
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
