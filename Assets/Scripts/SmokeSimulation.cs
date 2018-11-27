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
	public ComputeShader computeAdvection;
	public ComputeShader computeJacobi;
    public ComputeShader computeBuoyancy;
	
	public int width = 64;
	public int height = 64;
	public int depth = 64;

	//privates
	RenderTexture mObstacle;
	RenderTexture mDivergence;
	RenderTexture mPressure;
	RenderTexture [] mDensity = new RenderTexture[2];
	RenderTexture [] mTemperature  = new RenderTexture[2];
	RenderTexture [] mVelocity  = new RenderTexture[2];
	

	Vector3Int texRes; // TODO: Make it flexible

	// Use this for initialization
	void Start () {
        texRes = new Vector3Int(width, height, depth);
        //initialize 3D buffers
        initialize3DTexture(mDensity, RenderTextureFormat.RFloat);
		initialize3DTexture(mTemperature, RenderTextureFormat.RFloat);
		
		initialize3DTexture(mVelocity, RenderTextureFormat.ARGB32);

		//no readwrite 
		mDivergence = initializeRenderTexture(RenderTextureFormat.RFloat);
		mPressure = initializeRenderTexture(RenderTextureFormat.RFloat);
        mObstacle = new RenderTexture(texRes[0], texRes[1], 0, RenderTextureFormat.RFloat); // To Alpha 8
        mObstacle.enableRandomWrite = true;
        mObstacle.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        mObstacle.isPowerOfTwo = true;
        mObstacle.volumeDepth = texRes[2];
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
		ApplyAdvection(dt, mTemperature);
		ApplyAdvection(dt, mDensity);
        ApplyVelocity(dt);
		
		ApplyBuoyancy(dt);
    }


    void OnDestroy()
	{
		mDensity[READ].Release();	
		mDensity[WRITE].Release();
		mTemperature[READ].Release();
		mTemperature[WRITE].Release();
		mVelocity[READ].Release();
		mVelocity[WRITE].Release();
		mDivergence.Release();
		mPressure.Release();
	}

	//helpers

	RenderTexture initializeRenderTexture(RenderTextureFormat format) {
		RenderTexture newTex  = new RenderTexture(texRes[0], texRes[1], 0, format); // To Alpha 8
        newTex.enableRandomWrite = true;
        newTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        newTex.volumeDepth = texRes[2];
        newTex.isPowerOfTwo = true;
        newTex.Create();
		return newTex;
	}

	void initialize3DTexture(RenderTexture[] rTex, RenderTextureFormat format) {
		rTex [READ] = initializeRenderTexture(format);
		rTex [WRITE] = initializeRenderTexture(format);
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
		int kernel = computeAdvection.FindKernel("AdvectVelocity");
		computeAdvection.SetFloat("_timeStep", dt);
        computeAdvection.SetTexture(kernel, "_ReadVelocity", mVelocity[READ]);
		computeAdvection.SetTexture(kernel, "_WriteVelocity", mVelocity[WRITE]);
		computeAdvection.SetTexture(kernel, "_Obstacle", mObstacle);
        computeAdvection.Dispatch(kernel, 
                                texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);

		SwapBuffer(mVelocity);
    }

    void ApplyAdvection(float dt, RenderTexture [] texture)
    {
		int kernel = computeAdvection.FindKernel("Advect");
		computeAdvection.SetFloat("_timeStep", dt);
        computeAdvection.SetTexture(kernel,	"_ReadVelocity", mVelocity[READ]);
		computeAdvection.SetTexture(kernel,	"_Obstacle", mObstacle);
		computeAdvection.SetTexture(kernel,	"_Read", texture[READ]);
		computeAdvection.SetTexture(kernel,	"_Write", texture[WRITE]);
        computeAdvection.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
		SwapBuffer(texture);
    }

	void ApplyBuoyancy(float dt)
    {
		int kernel = computeBuoyancy.FindKernel("CSMain");
		computeBuoyancy.SetFloat("_DeltaTime", dt);
		computeBuoyancy.SetFloat("_Mass", 0.0125f);
		computeBuoyancy.SetFloat("_AmbientTemperature", 0.0f);
		
		computeBuoyancy.SetVector("_Up", Vector3.up);
        computeBuoyancy.SetTexture(kernel,	"_ReadVelocity", mVelocity[READ]);
		computeBuoyancy.SetTexture(kernel,	"_Density", mDensity[READ]);
		computeBuoyancy.SetTexture(kernel,	"_Temperature", mTemperature[READ]);
		computeBuoyancy.SetTexture(kernel,	"_WriteVelocity", mVelocity[WRITE]);
        computeBuoyancy.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
		SwapBuffer(mVelocity);
    }

	void ComputeDivergence()
	{
		computeJacobi.SetTexture(computeJacobi.FindKernel("CSMain"), 
									"_Velocity",
									mVelocity[READ]);
        computeJacobi.Dispatch(computeObstacle.FindKernel("CSMain"), 
                                texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
	}

	void ComputePressure() 
	{
		
	}

	void SwapBuffer (RenderTexture [] swap) {
		RenderTexture temp = swap[0];
		swap[0] = swap[1];
		swap[1] = temp;
	}
}
