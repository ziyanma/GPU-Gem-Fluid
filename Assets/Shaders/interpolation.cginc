
SamplerState vel_Linear_Clamp_Sampler;

float3 cellIndex2TexCoord(float3 index)
{
	float w, h, d;
	_Obstacle.GetDimensions(w,h,d);
	// Convert a value in the range [0,gridSize] to one in the range [0,1].
	return float3(index.x / w,
                index.y / h,
                index.z / d);
}

float4 SampleBilinearRGBA(float3 cellCoord, Texture3D<float4> tex) {
	float3 texCoord = cellIndex2TexCoord(cellCoord);
	return tex.SampleLevel(vel_Linear_Clamp_Sampler, texCoord, 0);
}

float SampleBilinearFloat(float3 cellCoord, Texture3D<float> tex) {
	float3 texCoord = cellIndex2TexCoord(cellCoord);
	return tex.SampleLevel(vel_Linear_Clamp_Sampler, texCoord, 0);
}