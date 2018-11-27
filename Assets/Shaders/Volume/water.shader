Shader "Custom/water"
{
	Properties
	{
		_ReflectionTex ("Internal reflection", 2D) = "white" {}
		_DistortParams ("Distortions (Bump waves, Reflection, Fresnel power, Fresnel bias)", Vector) = (1.0 ,1.0, 2.0, 1.15)
		_InvFadeParemeter ("Auto blend parameter (Edge, Shore, Distance scale)", Vector) = (0.15 ,0.15, 0.5, 1.0)
		_BaseColor ("Base color", COLOR)  = ( .54, .95, .99, 0.5)
		_ReflectionColor ("Reflection color", COLOR)  = ( .54, .95, .99, 0.5)
		_SpecularColor ("Specular color", COLOR)  = ( .72, .72, .72, 1)
		_WorldLightDir ("Specular light direction", Vector) = (0.0, 0.1, -0.5, 0.0)

		_Scale ("Scale", Vector) = (1,1,1)
		_Translate ("Translate", Vector) = (0,0,0)
		_StepSize ("StepSize", Range(0.05, 1)) = 0.1
		_Obstacle ("Obstacle", 3D) = "defaulttexture" {}
        _Density ("Density", 3D) = "defaulttexture" {}
	}
	SubShader
	{
		Tags {"RenderType"="Transparent" "Queue"="Transparent"}
		LOD 200

		GrabPass { "_RefractionTex" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			float3 _Scale;
			float3 _Translate;
			float _StepSize;
            float4 _WaterColor;

			Texture3D _Obstacle;
            Texture3D<float> _Density;

			sampler2D _ReflectionTex;
			sampler2D _RefractionTex;
			sampler2D _ShoreTex;

			uniform float4 _RefrColorDepth;
			uniform float4 _SpecularColor;
			uniform float4 _BaseColor;
			uniform float4 _ReflectionColor;
			uniform float4 _InvFadeParemeter;
			uniform float _Shininess;
			uniform float4 _WorldLightDir;
			uniform float4 _DistortParams;

			// shortcuts
			#define PER_PIXEL_DISPLACE _DistortParams.x
			#define REALTIME_DISTORTION _DistortParams.y
			#define FRESNEL_POWER _DistortParams.z
			#define VERTEX_WORLD_NORMAL i.normalInterpolator.xyz
			#define FRESNEL_BIAS _DistortParams.w
			#define NORMAL_DISPLACEMENT_PER_VERTEX _InvFadeParemeter.z

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

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

			SamplerState vel_Linear_Clamp_Sampler;

			float4 sampleColor(float3 pos) {
				// uint x,y,z;
				// _Obstacle.GetDimiensions(x,y,z);

				float3 translate = (pos - _Translate) / _Scale + float3(0.5, 0.5, 0.5);
				// int3 sampler = translate * int3(x,y,z);
                float samp = _Density.Sample(vel_Linear_Clamp_Sampler, translate);
                return samp * _WaterColor;
			}

            float sampleAlpha(float3 pos) {
				// uint x,y,z;
				// _Obstacle.GetDimiensions(x,y,z);

				float3 translate = (pos - _Translate) / _Scale + float3(0.5, 0.5, 0.5);
				// int3 sampler = translate * int3(x,y,z);
                float samp = _Density.Sample(vel_Linear_Clamp_Sampler, translate);
                return samp;
			}
			
			float Fresnel(half3 viewVector, half3 worldNormal, half bias, half power)
			{
				half facing =  clamp(1.0-max(dot(-viewVector, worldNormal), 0.0), 0.0,1.0);	
				half refl2Refr = saturate(bias+(1.0-bias) * pow(facing,power));	
				return refl2Refr;	
			}

			// Vertex Shader
			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = UnityObjectToClipPos(v.vertex);
    			OUT.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    			return OUT;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 cameraPos = _WorldSpaceCameraPos;
				float3 direction = normalize(i.worldPos - cameraPos);
				float stepSize =  _StepSize > 0 ? _StepSize : 0.1f;
				half4 distortOffset = half4(float3(0.0f,1.0f,0.0f).xz * REALTIME_DISTORTION * 10.0, 0, 0);
				float4 grabPassPos;
				grabPassPos.xy = ( float2( i.pos.x, i.pos.y ) + i.pos.w ) * 0.5;
				grabPassPos.zw = i.pos.zw;
				half4 grabWithOffset = grabPassPos + distortOffset;
				float4 rtRefractions = tex2Dproj(_RefractionTex, UNITY_PROJ_COORD(grabWithOffset));

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
				
                float4 baseColor = _BaseColor;

				float refl2Refr = Fresnel(direction, float3(0.0f,1.0f,0.0f), FRESNEL_BIAS, FRESNEL_POWER);
		
				float3 reflectVector = normalize(reflect(direction, float3(0.0f,1.0f,0.0f)));
				float3 h = normalize ((_WorldLightDir.xyz) + direction.xyz);
				float nh = max (0, dot (float3(0.0f,1.0f,0.0f), -h));
				float spec = max(0.0,pow (nh, _Shininess));

				// base, depth & reflection colors
				float4 reflectionColor = _ReflectionColor;
				//baseColor = lerp (lerp (rtRefractions, baseColor, baseColor.a), reflectionColor, refl2Refr);
				baseColor = lerp (rtRefractions, baseColor, baseColor.a);
				baseColor = baseColor + spec * _SpecularColor;
				
				UNITY_APPLY_FOG(i.fogCoord, baseColor);
				return baseColor;
			}
			ENDCG
		}
	}
}
