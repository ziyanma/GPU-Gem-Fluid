using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSimulation : AnimationController {
    public Material mat;

    public RenderTexture [] mTexture;
    public RenderTexture mObstacle;

    public Vector3Int texRes = new Vector3Int(64, 64, 64); // TODO: Make it flexible
    public int NUMTHREADS = 8; // Make sure to change numthread.cginc as well

    public ComputeShader computeObstacle;
    
    // public Vector3 mSize;

	// Use this for initialization
	void Start () {
        mTexture = new RenderTexture[2];
        mTexture[0] = new RenderTexture(texRes[0], texRes[1], texRes[2], RenderTextureFormat.RFloat); // To Alpha 8
        mTexture[1] = new RenderTexture(texRes[0], texRes[1], texRes[2], RenderTextureFormat.RFloat);
        mObstacle = new RenderTexture(texRes[0], texRes[1], texRes[2], RenderTextureFormat.RFloat); // To Alpha 8
        //mObstacle.isVolume = true;
        mObstacle.enableRandomWrite = true;
        mObstacle.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        mObstacle.volumeDepth = texRes[2];
        mObstacle.isPowerOfTwo = true;
        mObstacle.Create();

        InitializeObstacle();
        
        // mat.SetTexture("_Obstacle", mObstacle);
        // foreach (var c in mObstacle.GetPixels())
        // {
        //     Debug.Log(c);
        // }
    }

    void SwapBuffer (Texture3D [] swap) {
        Texture3D texture = swap[0];
        swap[0] = swap[1];
        swap[1] = texture;
    }

    // Update is called once per frame
    protected void Update () {
        base.Update();
        // Set up shader
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);

        mat.SetTexture("_Obstacle", mObstacle);
    }

    void OnPreRender() {
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
    }
    
    void InitializeObstacle() {
        Vector4 size = new Vector4(texRes.x, texRes.y, texRes.z, 0.0f);
        Debug.Log(computeObstacle.FindKernel("CSMain"));
        computeObstacle.SetVector("_GridSize", size);
        computeObstacle.SetTexture(computeObstacle.FindKernel("CSMain"), "Result", mObstacle);
        computeObstacle.Dispatch(computeObstacle.FindKernel("CSMain"), 
                                texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
    }
}
