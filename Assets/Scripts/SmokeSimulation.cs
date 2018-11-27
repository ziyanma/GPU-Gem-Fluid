using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeSimulation : AnimationController {
    //consts
	const int READ = 0;
	const int WRITE = 1;
	const int NUMTHREADS = 8; // Make sure to change numthread.cginc as well
	const float TIME_STEP = 0.1f;

	//material & shader
	public Material mat;
	public ComputeShader computeObstacle;
	
	public int width = 64;
	public int height = 64;
	public int depth = 64;

	//privates
	RenderTexture mObstacle;
	RenderTexture [] mDensity = new RenderTexture[2];
	RenderTexture [] mTemperature  = new RenderTexture[2];
	RenderTexture [] mPressure  = new RenderTexture[2];
	RenderTexture [] mVelocity  = new RenderTexture[2];
	RenderTexture [] mDivergence = new RenderTexture[2];

	Vector3Int texRes; // TODO: Make it flexible


	// Use this for initialization
	void Start () {
        texRes = new Vector3Int(width, height, depth);
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
    protected override void Update () {
        base.Update();
        // Set up shader
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
        mat.SetTexture("_Obstacle", mObstacle);
    }

    public override void NextFrame(float dt)
    {
        ApplyVelocity(dt);

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
		rTex [READ] = new RenderTexture (texRes [0], texRes [1], texRes [2], format);
		rTex [WRITE] = new RenderTexture (texRes [0], texRes [1], texRes [2], format);
	}
    
    void InitializeObstacle() {
        Vector4 size = new Vector4(texRes.x, texRes.y, texRes.z, 0.0f);
        computeObstacle.SetVector("_GridSize", size);
        computeObstacle.SetTexture(computeObstacle.FindKernel("CSMain"), "Result", mObstacle);
        computeObstacle.Dispatch(computeObstacle.FindKernel("CSMain"), 
                                texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
    }

    void ApplyVelocity(float dt)
    {

    }

	void SwapBuffer (RenderTexture [] swap) {
		RenderTexture temp = swap[0];
		swap[0] = swap[1];
		swap[1] = temp;
	}
}
