
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
SamplerState vel_Linear_Clamp_Sampler;

float3 cellIndex2TexCoord(float3 index)
{
	float w, h, d;
	_Obstacle.GetDimensions(w,h,d);
	// Convert a value in the range [0,gridSize] to one in the range [0,1].
	return float3(index.x / w,
                index.y / h,
                (index.z + 0.5f) / d);
}

// float4 SampleBilinearRGBA(float3 cellCoord, Texture3D<float4> tex) {
// 	float3 texCoord = cellIndex2TexCoord(cellCoord);
	
// 	return tex.SampleLevel(vel_Linear_Clamp_Sampler, texCoord, 0);
// }

float4 SampleBilinearRGBA(float3 id, Texture3D<float4> tex) {
	uint3 neg = floor(id);
	uint3 pos = ceil(id);
	
	// get the corners
	float3 c000 = float3(neg.x,neg.y,neg.z);
	float3 c001 = float3(neg.x,neg.y,pos.z);
	float3 c010 = float3(neg.x,pos.y,neg.z);
	float3 c011 = float3(neg.x,pos.y,pos.z);
	float3 c100 = float3(pos.x,neg.y,neg.z);
	float3 c101 = float3(pos.x,neg.y,pos.z);
	float3 c110 = float3(pos.x,pos.y,neg.z);
	float3 c111 = float3(pos.x,pos.y,pos.z);
	
	float3 weights = pos - id;
	
	float trilinearTaps[] = {
		tex[c000],
		tex[c001],
		tex[c010],
		tex[c011],
		tex[c100],
		tex[c101],
		tex[c110],
		tex[c111]
	};

	float4 result =	lerp(
						lerp(
							lerp(trilinearTaps[0],trilinearTaps[1],weights.z),
							lerp(trilinearTaps[2],trilinearTaps[3],weights.z),
							weights.y
							),
						lerp(
							lerp(trilinearTaps[4],trilinearTaps[5],weights.z),
							lerp(trilinearTaps[6],trilinearTaps[7],weights.z),
							weights.y
							),
						weights.x
					);

	return result;
}

// float SampleBilinearFloat(float3 cellCoord, Texture3D<float> tex) {
// 	float3 texCoord = cellIndex2TexCoord(cellCoord);
	
// 	return tex.SampleLevel(vel_Linear_Clamp_Sampler, texCoord, 0);
// }

float SampleBilinearFloat(float3 id, Texture3D<float> tex) {

	// find closest actual voxels (g_vDim is the size of the volume, delta is 1/g_vDim)
	uint3 neg = floor(id);
	uint3 pos = ceil(id);
	
	// get the corners
	float3 c000 = float3(neg.x,neg.y,neg.z);
	float3 c001 = float3(neg.x,neg.y,pos.z);
	float3 c010 = float3(neg.x,pos.y,neg.z);
	float3 c011 = float3(neg.x,pos.y,pos.z);
	float3 c100 = float3(pos.x,neg.y,neg.z);
	float3 c101 = float3(pos.x,neg.y,pos.z);
	float3 c110 = float3(pos.x,pos.y,neg.z);
	float3 c111 = float3(pos.x,pos.y,pos.z);
	
	float3 weights = pos - id;
	
	float trilinearTaps[] = {
		tex[c000],
		tex[c001],
		tex[c010],
		tex[c011],
		tex[c100],
		tex[c101],
		tex[c110],
		tex[c111]
	};

	float result =	lerp(
						lerp(
							lerp(trilinearTaps[0],trilinearTaps[1],weights.z),
							lerp(trilinearTaps[2],trilinearTaps[3],weights.z),
							weights.y
							),
						lerp(
							lerp(trilinearTaps[4],trilinearTaps[5],weights.z),
							lerp(trilinearTaps[6],trilinearTaps[7],weights.z),
							weights.y
							),
						weights.x
					);

	return result;
}