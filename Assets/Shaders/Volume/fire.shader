// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Custom/fire" {
	Properties {
		_Scale ("Scale", Vector) = (1,1,1)
		_Translate ("Translate", Vector) = (1,1,1)
		// 0 Step size would absolutely crash Unity
		_StepSize ("StepSize", Range(0.05, 1)) = 0.1
		_Obstacle ("Obstacle", 3D) = "defaulttexture" {}
	}

	SubShader {
		Tags { "Queue"="Transparent" }
		LOD 200
	Pass {
		Cull front
    	Blend SrcAlpha OneMinusSrcAlpha
		CGPROGRAM
			#include "UnityCG.cginc"

			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			
			float3 _Scale;
			float3 _Translate;
			float _StepSize;

			Texture3D _Obstacle;

			struct v2f 
			{
    			float4 pos : SV_POSITION;
    			float3 worldPos : TEXCOORD0;
			};

			struct Ray
			{
				float3 origin;
				float3 dir;
			};

			struct BBox {
			    float3 Min;
			    float3 Max;
			};

			//find intersection points of a ray with a box
			bool intersectBox(Ray r, BBox aabb, out float t0, out float t1)
			{
			    float3 invR = 1.0 / r.dir;
			    float3 tbot = invR * (aabb.Min-r.origin);
			    float3 ttop = invR * (aabb.Max-r.origin);
			    float3 tmin = min(ttop, tbot);
			    float3 tmax = max(ttop, tbot);
			    float2 t = max(tmin.xx, tmin.yz);
			    t0 = max(t.x, t.y);
			    t = min(tmax.xx, tmax.yz);
			    t1 = min(t.x, t.y);
			    return t0 <= t1;
			}


			SamplerState samPointClamp
			{
				Filter = MIN_MAG_MIP_POINT;
				AddressU = Wrap;
				AddressV = Wrap;
				AddressW = Wrap;
			};

			float4 sampleColor(float3 pos) {
				// uint x,y,z;
				// _Obstacle.GetDimiensions(x,y,z);

				float3 translate = (pos - _Translate) / _Scale + float3(0.5, 0.5, 0.5);
				// int3 sampler = translate * int3(x,y,z);
				float sample = _Obstacle.Sample(samPointClamp, translate);
				// float sample = tex3D(_Obstacle, translate);
				if (sample > 0) {
					return float4(1.0, 0.0, 0.0, 1.0f);
				} 
				return float4 (0.0f, 0.0f, 0.0f, 0.0f);

				// if (length(pos - _Translate) < 0.5f) {
				// 	return float4(1.0, 0.0, 0.0, 0.1f);
				// } 

				// return float4(0.0, 0.0, 0.0, 0.0f);
			}


			// Vertex Shader
			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = UnityObjectToClipPos(v.vertex);
    			OUT.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    			return OUT;
			}
	
			// Fragment Shader
			float4 frag(v2f IN) : COLOR
			{
				float3 cameraPos = _WorldSpaceCameraPos;
				float3 direction = normalize(IN.worldPos - cameraPos);
				float stepSize =  _StepSize > 0 ? _StepSize : 0.1f;

				Ray ray;
				ray.origin = cameraPos;
				ray.dir = direction;

				float near, far;
				BBox bbox;
				bbox.Min = float3(-0.5,-0.5,-0.5)*_Scale + _Translate;
				bbox.Max = float3(0.5,0.5,0.5)*_Scale + _Translate;

				intersectBox(ray, bbox, near, far);

				// Ray marching
				float3 rayStart = ray.origin + ray.dir * near;
				float3 rayEnd = ray.origin + ray.dir * far;
				
				float dist = length(rayEnd - rayStart);
				float3 ds = normalize(rayEnd - rayStart) * stepSize;
				int numStep = dist / stepSize;
				if (numStep > 64) numStep = 64;

				float3 rayPos = rayStart;
				
				float4 FinalColor = float4(0.0, 0.0, 0.0, 0.0);
				// FinalColor.rgb += sampleColor.rgb * sampleColor.a 
				for (int i = 0; i < numStep; i++, rayPos += ds) {
					float4 sample = sampleColor(rayPos);
					FinalColor.xyz += sample.xyz * sample.a * (1 - FinalColor.a);
					FinalColor.a += sample.a * (1 - FinalColor.a);
				}

				return FinalColor;
			}
			
			ENDCG
	}	
	}
}
