
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

float4 SampleBilinearRGBA(float3 cellCoord, Texture3D<float4> tex) {
	float3 texCoord = cellIndex2TexCoord(cellCoord);
	
	return tex.SampleLevel(vel_Linear_Clamp_Sampler, texCoord, 0);
}

// float SampleBilinearFloat(float3 id, Texture3D<float> tex) {
// 	float3 texCoord = cellIndex2TexCoord(id);
// 	uint3 L = uint3(floor(id.x)-1, id.y, id.z);
//     uint3 R = uint3(ceil(id.x+1), id.y, id.z);

//     uint3 B = uint3(id.x, floor(id.y-1), id.z);
//     uint3 T = uint3(id.x, ceil(id.y+1), id.z);

//     uint3 D = uint3(id.x, id.y, floor(id.z -1));
//     uint3 U = uint3(id.x, id.y, ceil(id.z +1));

// 	float texL = tex[L];
//     float texR = tex[R];
    
//     float texB = tex[B];
//     float texT = tex[T];
    
//     float texD = tex[D];
//     float texU = tex[U];

// 	return lerp(lerp(lerp(texL, texR, R-id.x), lerp(texB, texT, T-id.y), 0.5), lerp(texD, texU, U-id.z), 0.5);
// }