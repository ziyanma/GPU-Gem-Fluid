using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSimulation : AnimationController {
    //consts
	const int READ = 0;
	const int WRITE = 1;
	const int NUMTHREADS = 8; // Make sure to change numthread.cginc as well
	const float Time_step = 0.1f;

	//material & shader
	public Material mat;
	public ComputeShader computeObstacle;
	public int width = 64;
	public int height = 64;
	public int depth = 64;

	//privates
	private RenderTexture mObstacle;
	private RenderTexture [] mDensity, mTemperature, mPressure, mVelocity, mDivergence;
	private Vector3Int texRes = new Vector3Int(width, height, depth); // TODO: Make it flexible


	// Use this for initialization
	void Start () {
        //initialize 3D buffers
		initialize3DTexture(mDensity, RenderTextureFormat.RFloat);
		initialize3DTexture(mTemperature, RenderTextureFormat.RFloat);
		initialize3DTexture(mPressure, RenderTextureFormat.ARGB32);
		initialize3DTexture(mVelocity, RenderTextureFormat.ARGB32);
		initialize3DTexture(mDivergence, RenderTextureFormat.ARGB32);

        mObstacle = new RenderTexture(texRes[0], texRes[1], texRes[2], RenderTextureFormat.RFloat); // To Alpha 8
        mObstacle.enableRandomWrite = true;
        mObstacle.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        mObstacle.volumeDepth = texRes[2];
        mObstacle.isPowerOfTwo = true;
        mObstacle.Create();

        InitializeObstacle();
        
        mat.SetTexture("_Obstacle", mObstacle);
    }

    // Update is called once per frame
    protected void Update () {
        base.Update();
        // Set up shader
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
        mat.SetTexture("_Obstacle", mObstacle);
    }

	void OnDestroy()
	{
		mDensity[READ].Release();	
		mDensity[WRITE].Release();
		mTemperature[READ].Release();
		mTemperature[WRITE].Release();
		mPressure[READ].Release();
		mPressure[WRITE].Release();
		mVelocity[READ].Release();
		mVelocity[WRITE].Release();
		mDivergence[READ].Release();
		mDivergence[WRITE].Release();
	}

	//helpers

	void initialize3DTexture(RenderTexture[] rTex, RenderTextureFormat format) {
		rTex = new RenderTexture[2];
		rTex [READ] = new RenderTexture (texRes [0], texRes [1], texRes [2], format);
		rTex [WRITE] = new RenderTexture (texRes [0], texRes [1], texRes [2], format);
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

	void SwapBuffer (RenderTexture [] swap) {
		RenderTexture temp = swap[0];
		swap[0] = swap[1];
		swap[1] = temp;
	}
}
