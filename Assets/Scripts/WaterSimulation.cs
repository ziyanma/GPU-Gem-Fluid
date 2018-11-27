using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSimulation : AnimationController {
	//DONT CHANGE THESE
	const int READ = 0;
	const int WRITE = 1;
	const int NUMTHREADS = 8; // Make sure to change numthread.cginc as well
	const int PHI_N_HAT = 0;
	const int PHI_N_1_HAT = 1;

	//material & shader
	public Material mat;
	public ComputeShader computeObstacle;
	public ComputeShader computeAdvection;
	public ComputeShader computeJacobi;
    public ComputeShader computeBuoyancy;
    public ComputeShader computeDivergence;
	public ComputeShader computeProjection;
	public ComputeShader computeImpulse;
	public int width = 128;
	public int height = 128;
	public int depth = 128;

	//privates
	RenderTexture mObstacle;
	RenderTexture mDivergence;
	RenderTexture [] mPressure = new RenderTexture[2];
	RenderTexture [] mVelocity = new RenderTexture[2];
	RenderTexture [] mDensity = new RenderTexture[2];

	Vector3Int texRes; // TODO: Make it flexible

	// Use this for initialization
	void Start()
	{
        texRes = new Vector3Int(width, height, depth);
        initialize3DTexture(mDensity, RenderTextureFormat.RFloat);
		initialize3DTexture(mPressure, RenderTextureFormat.RFloat);
		initialize3DTexture(mVelocity, RenderTextureFormat.ARGBHalf);
		mDivergence = initializeRenderTexture(RenderTextureFormat.RFloat);
        mObstacle = initializeRenderTexture(RenderTextureFormat.RFloat);
        InitializeObstacle();
        mat.SetTexture("_Obstacle", mObstacle);
	}

	void initialize3DTexture(RenderTexture[] rTex, RenderTextureFormat format) {
		rTex [READ] = initializeRenderTexture(format);
		rTex [WRITE] = initializeRenderTexture(format);
	}

	RenderTexture initializeRenderTexture(RenderTextureFormat format) {
		RenderTexture newTex  = new RenderTexture(texRes[0], texRes[1], 24, format); // To Alpha 8
        newTex.enableRandomWrite = true;
        newTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        newTex.volumeDepth = texRes[2];
        newTex.isPowerOfTwo = true;
        newTex.Create();
		return newTex;
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

	void SwapBuffer (RenderTexture [] swap) {
		RenderTexture temp = swap[0];
		swap[0] = swap[1];
		swap[1] = temp;
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

    void WaterVelocity(float dt)
    {
		int kernel = computeAdvection.FindKernel("WaterVelocity");
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

	void ComputeDivergence()
	{
		int kernel = computeDivergence.FindKernel("CSMain");
		computeDivergence.SetTexture(kernel, "_Velocity", mVelocity[READ]);
        computeDivergence.SetTexture(kernel, "_WRITE", mDivergence);
        computeDivergence.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                			texRes.y / NUMTHREADS, 
                              			 	texRes.z / NUMTHREADS);
	}

	void ComputePressure() 
	{
		int kernel = computeJacobi.FindKernel("CSMain");
		computeJacobi.SetTexture(kernel, "_Pressure", mPressure[READ]);
		computeJacobi.SetTexture(kernel, "_Divergence", mDivergence);
        computeJacobi.SetTexture(kernel, "_WRITE", mPressure[WRITE]);
        computeJacobi.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                		texRes.y / NUMTHREADS, 
                                		texRes.z / NUMTHREADS);
		SwapBuffer(mPressure);
	}

	void Project()
	{
		int kernel = computeProjection.FindKernel("CSMain");
		computeProjection.SetTexture(kernel, "_Obstacle", mObstacle);
		computeProjection.SetTexture(kernel, "_Pressure", mPressure[READ]);
		computeProjection.SetTexture(kernel, "_Velocity", mVelocity[READ]);
		computeProjection.SetTexture(kernel, "_WRITE", mVelocity[WRITE]);
		computeProjection.Dispatch(kernel, texRes.x / NUMTHREADS, 
										texRes.y / NUMTHREADS, 
										texRes.z / NUMTHREADS);
		SwapBuffer(mVelocity);
	}

	void ApplyImpulse(float dt, RenderTexture [] texture, float amount)
	{
		int kernel = computeImpulse.FindKernel("CSMain");
		computeImpulse.SetFloat("_DeltaTime", dt);
		computeImpulse.SetFloat("_Amount", amount);		
		computeImpulse.SetTexture(kernel, "_READ", texture[READ]);
		computeImpulse.SetTexture(kernel, "_WRITE", texture[WRITE]);

		computeImpulse.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
        SwapBuffer(texture);
	}

	void WaterImpulse(float dt, RenderTexture [] texture, float amount)
	{
		int kernel = computeImpulse.FindKernel("WaterImpulse");
		computeImpulse.SetFloat("_DeltaTime", dt);
		computeImpulse.SetFloat("_Amount", amount);		
		computeImpulse.SetTexture(kernel, "_READ", texture[READ]);
		computeImpulse.SetTexture(kernel, "_WRITE", texture[WRITE]);

		computeImpulse.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
        SwapBuffer(texture);
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

	void WaterBuoyancy(float dt)
    {
		int kernel = computeBuoyancy.FindKernel("WaterBuoyancy");
		computeBuoyancy.SetFloat("_DeltaTime", dt);
		computeBuoyancy.SetFloat("_Mass", 0.0125f);
		computeBuoyancy.SetVector("_Up", Vector3.down);
        computeBuoyancy.SetTexture(kernel,	"_ReadVelocity", mVelocity[READ]);
		computeBuoyancy.SetTexture(kernel,	"_Density", mDensity[READ]);
		computeBuoyancy.SetTexture(kernel,	"_Pressure", mPressure[READ]);
		computeBuoyancy.SetTexture(kernel,	"_WriteVelocity", mVelocity[WRITE]);
        computeBuoyancy.Dispatch(kernel, texRes.x / NUMTHREADS, 
                                texRes.y / NUMTHREADS, 
                                texRes.z / NUMTHREADS);
		SwapBuffer(mVelocity);
    }

    public override void NextFrame(float dt)
    {
		ApplyAdvection(dt, mDensity);
        // ApplyVelocity(dt);
        WaterVelocity(dt);
		WaterBuoyancy(dt);
		// ApplyImpulse(dt, mDensity, 1.0f);
		WaterImpulse(dt, mDensity, 1.0f);
		ComputeDivergence();
		ComputePressure();
		Project();
	}
	
	// Update is called once per frame
	protected override void Update()
	{
        base.Update();
        // Set up shader
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
        mat.SetTexture("_Obstacle", mObstacle);
		mat.SetTexture("_Density", mDensity[READ]);
	}
	
    void OnDestroy()
	{
		mVelocity[READ].Release();
		mVelocity[WRITE].Release();
		mPressure[READ].Release();
		mPressure[WRITE].Release();
		mDensity[READ].Release();	
		mDensity[WRITE].Release();
		mDivergence.Release();
		mObstacle.Release();
	}
}
