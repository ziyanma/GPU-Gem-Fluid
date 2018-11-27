﻿using System.Collections;
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
	public int width = 64;
	public int height = 64;
	public int depth = 64;

	//privates
	RenderTexture mObstacle;
	RenderTexture mDivergence;
	RenderTexture [] mPressure = new RenderTexture[2];
	RenderTexture [] mVelocity = new RenderTexture[2];

	Vector3Int texRes; // TODO: Make it flexible

	// Use this for initialization
	void Start()
	{
        texRes = new Vector3Int(width, height, depth);
		initialize3DTexture(mVelocity, RenderTextureFormat.ARGB32);
		initialize3DTexture(mPressure, RenderTextureFormat.ARGB32);
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
		RenderTexture newTex  = new RenderTexture(texRes[0], texRes[1], 0, format); // To Alpha 8
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

    public override void NextFrame(float dt)
    {
        ApplyVelocity(dt);
		ComputeDivergence();
		ComputePressure();
	}
	
	// Update is called once per frame
	protected override void Update()
	{
        base.Update();
        // Set up shader
        mat.SetVector("_Scale", transform.localScale);
        mat.SetVector("_Translate", transform.position);
        mat.SetTexture("_Obstacle", mObstacle);
	}
	
    void OnDestroy()
	{
		mVelocity[READ].Release();
		mVelocity[WRITE].Release();
		mPressure[READ].Release();
		mPressure[WRITE].Release();
		mDivergence.Release();
		mObstacle.Release();
	}
}