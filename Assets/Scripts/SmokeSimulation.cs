using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSimulation : AnimationController {
    public Material mat;

    public Texture3D [] mTexture;
    public Texture3D mObstacle;

    public Vector3Int texRes = new Vector3Int(64, 64, 64); // TODO: Make it flexible
    public int NUMTHREADS = 8; // Make sure to change numthread.cginc as well

    public ComputeShader computeObstacle;
    
    // public Vector3 mSize;

	// Use this for initialization
	void Start () {
        mTexture = new Texture3D[2];
        mTexture[0] = new Texture3D(texRes[0], texRes[1], texRes[2], TextureFormat.Alpha8, true); // To Alpha 8
        mTexture[1] = new Texture3D(texRes[0], texRes[1], texRes[2], TextureFormat.Alpha8, true);

        mObstacle = new Texture3D(texRes[0], texRes[1], texRes[2], TextureFormat.Alpha8, true); // To Alpha 8

        InitializeObstacle();
    }

    void SwapBuffer (Texture3D [] swap) {
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

    void OnPreRender() {
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
    }
    
    void InitializeObstacle() {
        Vector4 size = new Vector4(texRes.x, texRes.y, texRes.z, 0.0f);

        computeObstacle.SetVector("_GridSize", size);
        computeObstacle.SetTexture(0, "Result", mObstacle);

        computeObstacle.Dispatch(0, (int)texRes.x / NUMTHREADS, (int)texRes.y / NUMTHREADS, (int)texRes.z / NUMTHREADS);
    }
}
