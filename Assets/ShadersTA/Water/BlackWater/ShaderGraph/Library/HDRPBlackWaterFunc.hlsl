#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"

#define TIME_FRAG_INPUT _Time.y
#define TIME_VERTEX_OUTPUT _Time.y
#define TIME ((TIME_FRAG_INPUT * _AnimationParams.z) * _AnimationParams.xy)
#define TIME_VERTEX ((TIME_VERTEX_OUTPUT * _AnimationParams.z) * _AnimationParams.xy)
#define UP_VECTOR float3(0,1,0)

#define FAR_CLIP _ProjectionParams.z
#define NEAR_CLIP _ProjectionParams.y
//Scale linear values to the clipping planes for orthographic projection (unity_OrthoParams.w = 1 = orthographic)
#define DEPTH_SCALAR lerp(1.0, FAR_CLIP - NEAR_CLIP, unity_OrthoParams.w)

struct SceneDepth
{
	float raw;
	float linear01;
	float eye;
};

float2 GetSourceUV(float2 uv, float2 wPos, float state) 
{
	float2 output =  lerp(uv, wPos, state);
	//output.x = (int)((output.x / 0.5) + 0.5) * 0.5;
	//output.y = (int)((output.y / 0.5) + 0.5) * 0.5;

	#ifdef _RIVER
	//World-space tiling is useless in this case
	return uv;
	#endif
	
	return output;
}

float4 GetVertexColor(float4 inputColor, float4 mask)
{
	return inputColor * mask;
}

float4 PackedUV(float2 sourceUV, float2 time, float speed)
{
	#if _RIVER
	time.x = 0; //Only move in forward direction
	#endif
	
	float2 uv1 = sourceUV.xy + (time.xy * speed);
	#ifndef _RIVER
	//Second UV, 2x larger, twice as slow, in opposite direction
	float2 uv2 = (sourceUV.xy * 0.5) + ((1 - time.xy) * speed * 0.5);
	#else
	//2x larger, same direction/speed
	float2 uv2 = (sourceUV.xy * 0.5) + (time.xy * speed);
	#endif

	return float4(uv1.xy, uv2.xy);
}

float3 BlendTangentNormals(float3 a, float3 b)
{
	return BlendNormalRNM(a, b);
}
float DepthDistance(float3 wPos, float3 viewPos, float3 normal)
{
	return length((wPos - viewPos) * normal);
}

// Linear depth buffer value between [0, 1] or [1, 0] to eye depth value between [near, far]
real LinearDepthToEyeDepth(real rawDepth)
{
    #if UNITY_REVERSED_Z
    return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
    #else
    return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
    #endif
}
//Linear depth difference between scene and current (transparent) geometry pixel
float SurfaceDepth(SceneDepth depth, float4 positionCS)
{
	const float sceneDepth = (unity_OrthoParams.w == 0) ? depth.eye : LinearDepthToEyeDepth(depth.raw);
	const float clipSpaceDepth = (unity_OrthoParams.w == 0) ? LinearEyeDepth(positionCS.z, _ZBufferParams) : LinearEyeDepth(positionCS.z / positionCS.w, _ZBufferParams);

	return sceneDepth - clipSpaceDepth;
}
//Return depth based on the used technique (buffer, vertex color, baked texture)
SceneDepth SampleDepth(float4 screenPos)
{
	SceneDepth depth = (SceneDepth)0;
	
#ifndef _DISABLE_DEPTH_TEX
	screenPos.xyz /= screenPos.w;

	depth.raw = LoadCameraDepth(screenPos.xy);
	depth.eye = LinearEyeDepth(depth.raw, _ZBufferParams);
	depth.linear01 = Linear01Depth(depth.raw, _ZBufferParams) * DEPTH_SCALAR;
#else
	depth.raw = 1.0;
	depth.eye = 1.0;
	depth.linear01 = 1.0;
#endif

	return depth;
}

//Reconstruct view-space position from depth.
float3 ReconstructViewPos(float4 screenPos, float3 viewDir, SceneDepth sceneDepth)
{
	#if UNITY_REVERSED_Z
	real rawDepth = sceneDepth.raw;
	#else
	// Adjust z to match NDC for OpenGL
	real rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneDepth.raw);
	#endif
	
	#if defined(ORTHOGRAPHIC_SUPPORT)
	//View to world position
	float4 viewPos = float4((screenPos.xy/screenPos.w) * 2.0 - 1.0, rawDepth, 1.0);
	float4x4 viewToWorld = UNITY_MATRIX_I_VP;
	#if UNITY_REVERSED_Z //Wrecked since 7.3.1 "fix" and causes warping, invert second row https://issuetracker.unity3d.com/issues/shadergraph-inverse-view-projection-transformation-matrix-is-not-the-inverse-of-view-projection-transformation-matrix
	//Commit https://github.com/Unity-Technologies/Graphics/pull/374/files
	viewToWorld._12_22_32_42 = -viewToWorld._12_22_32_42;              
	#endif
	float4 viewWorld = mul(viewToWorld, viewPos);
	float3 viewWorldPos = viewWorld.xyz / viewWorld.w;
	#endif

	//Projection to world position
	float3 camPos = GetCurrentViewPosition().xyz;
	float3 worldPos = sceneDepth.eye * (viewDir/screenPos.w) - camPos;
	float3 perspWorldPos = -worldPos;

	#if defined(ORTHOGRAPHIC_SUPPORT)
	return lerp(perspWorldPos, viewWorldPos, unity_OrthoParams.w);
	#else
	return perspWorldPos;
	#endif

}

#define AIR_RI 1.000293
float ReflectionFresnel(float3 worldNormal, float3 viewDir, float exponent)
{
	float cosTheta = saturate(dot(worldNormal, viewDir));	
	return pow(max(0.0, AIR_RI - cosTheta), exponent);
}

#define TRANSMISSION_WRAP_ANGLE (PI/12)
#define TRANSMISSION_WRAP_LIGHT cos(PI/2 - TRANSMISSION_WRAP_ANGLE)


//Specular Blinn-phong reflection in world-space
float3 DirectionalSpecularReflection(DirectionalLightData light, float3 viewDirectionWS, float3 normalWS, float perturbation, float size, float intensity, float3 positionWS)
{
	float3 upVector = float3(0, 1, 0);
	float3 offset = 0;
	
	#if _RIVER
	//Can't assume the surface is flat. Perturb the normal vector instead
	upVector = lerp(float3(0, 1, 0), normalWS, perturbation);
	#else
	//Perturb the light view vector
	offset = normalWS * perturbation;
	#endif
	
	float3 lightDir = -light.forward;
	const float3 halfVec = SafeNormalize(lightDir + viewDirectionWS + offset);
	half NdotH = saturate(dot(upVector, halfVec));

	half specSize = lerp(8196, 64, size);
	float specular = pow(NdotH, specSize);

	//Attenuation includes shadows, if available
	const float3 attenuatedLightColor = light.color;
	
	float3 specColor = attenuatedLightColor * specular * intensity;
	
	#if UNITY_COLORSPACE_GAMMA
	specColor = LinearToSRGB(specColor);
	#endif

	return specColor;
}

float3 PointSpecularReflection(LightData light, float3 viewDirectionWS, float3 normalWS, float3 positionWS)
{
	float3 upVector = float3(0, 1, 0);
	
	const float3 halfVec = SafeNormalize(-light.forward + viewDirectionWS);
	half NdotH = saturate(dot(normalWS, halfVec));

	float3 specular = (float3)NdotH;

	half modifier = pow(NdotH, _PointSpotLightReflectionExp * 100 * (0.01f + _ReflectionDistortion) * .5);

	#if _CARTOONWAVE
		float minStep = _WaveLightStep - _WaveLightFeather * .5;
		float maxStep = _WaveLightStep + _WaveLightFeather * .5;
		modifier = smoothstep(minStep * NdotH, maxStep, modifier);
	#endif

	float3 lightColor = light.color * GetCurrentExposureMultiplier();

	float4 distances;
	float3 lightToSample = positionWS - light.positionRWS;
	distances.w = dot(lightToSample, light.forward);
	if (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
	{
		distances.xyz = float3(1.0,1.0,1.0); 
	}
	else
	{
		float3 unL     = -lightToSample;
		float  distSq  = dot(unL, unL);
		float  distRcp = rsqrt(distSq);
		float  dist    = distSq * distRcp;
		distances.xyz = float3(dist, distSq, distRcp);
	}

	half atten = PunctualLightAttenuation(distances, light.rangeAttenuationScale , light.rangeAttenuationBias, light.angleScale, light.angleOffset);

	//Attenuation includes shadows, if available
	const float3 attenuatedLightColor = lightColor * atten;

	specular *= attenuatedLightColor;

	 half3 specularReflection = specular * modifier * _PointSpotLightStrength;
	
	#if UNITY_COLORSPACE_GAMMA
	specularReflection = LinearToSRGB(specularReflection);
	#endif

	return specularReflection;
}


float4 SampleBlackWaterEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod, float rangeCompressionFactorCompensation, float2 positionNDC, float2 pixelOffset, int sliceIdx = 0)
{
    // 31 bit index, 1 bit cache type
    uint cacheType = IsEnvIndexCubemap(index) ? ENVCACHETYPE_CUBEMAP : ENVCACHETYPE_TEXTURE2D;
    // Index start at 1, because -0 == 0, so we can't known which cache to sample for that index. Thus it is invalid.
    index = abs(index) - 1;

    float4 color = float4(0.0, 0.0, 0.0, 1.0);

    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        if (cacheType == ENVCACHETYPE_TEXTURE2D)
        {
            //_ReflAtlasPlanarCaptureVP is in capture space
            float3 ndc = ComputeNormalizedDeviceCoordinatesWithZ(texCoord, PLANAR_CAPTURE_VP[index]);
            float2 atlasCoords = GetReflectionAtlasCoordsPlanar(PLANAR_SCALE_OFFSET[index], ndc.xy, lod) + pixelOffset.xy;

            color.rgb = SAMPLE_TEXTURE2D_ARRAY_LOD(_ReflectionAtlas, s_trilinear_clamp_sampler, atlasCoords, sliceIdx, lod).rgb;
#if UNITY_REVERSED_Z
            // We check that the sample was capture by the probe according to its frustum planes, except the far plane.
            //   When using oblique projection, the far plane is so distorded that it is not reliable for this check.
            //   and most of the time, what we want, is the clipping from the oblique near plane.
            color.a = any(ndc.xy < 0) || any(ndc.xyz > 1) ? 0.0 : 1.0;
#else
            color.a = any(ndc.xyz < 0) || any(ndc.xy > 1) ? 0.0 : 1.0;
#endif
            float3 capturedForwardWS = PLANAR_CAPTURE_FORWARD[index].xyz;
            if (dot(capturedForwardWS, texCoord) < 0.0)
                color.a = 0.0;
            else
            {
                // Controls the blending on the edges of the screen
                const float amplitude = 100.0;

                float2 rcoords = abs(saturate(ndc.xy) * 2.0 - 1.0);

                // When the object normal is not aligned with the reflection plane, the reflected ray might deviate too much and go out
                // of the reflection frustum. So we apply blending when the reflection sample coords are on the edges of the texture
                // These "edges" depend on the screen space coordinates of the pixel, because it is expected that a pixel on the
                // edge of the screen will sample on the edge of the texture

                // Blending factors taking the above into account
                bool2 blend = (positionNDC < ndc.xy) ^ (ndc.xy < 0.5);
                float2 alphas = saturate(amplitude * abs(ndc.xy - positionNDC));
                alphas = float2(Smoothstep01(alphas.x), Smoothstep01(alphas.y));

                float2 weights = lerp(1.0, saturate(2.0 - 2.0 * rcoords), blend * alphas);
                color.a *= weights.x * weights.y;
            }
        }
        else if (cacheType == ENVCACHETYPE_CUBEMAP)
        {
            float2 atlasCoords = GetReflectionAtlasCoordsCube(CUBE_SCALE_OFFSET[index], texCoord, lod) + pixelOffset.xy;

            color.rgb = SAMPLE_TEXTURE2D_ARRAY_LOD(_ReflectionAtlas, s_trilinear_clamp_sampler, atlasCoords, sliceIdx, lod).rgb;
		}

        // Planar and Reflection Probes aren't pre-expose, so best to clamp to max16 here in case of inf
        color.rgb = ClampToFloat16Max(color.rgb);

        color.rgb *= rangeCompressionFactorCompensation;
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        color.rgb = SampleSkyTexture(texCoord, lod, sliceIdx).rgb;
        // Sky isn't pre-expose, so best to clamp to max16 here in case of inf
        color.rgb = ClampToFloat16Max(color.rgb);

#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
        if (_FogEnabled)
        {
            float4 fogAttenuation = EvaluateFogForSkyReflections(lightLoopContext.positionWS, texCoord);
            color.rgb = color.rgb * fogAttenuation.a + fogAttenuation.rgb;
        }
#endif
    }

    return color;
}

TEXTURE2D(_PlanarReflectionLeft);
SAMPLER(sampler_PlanarReflectionLeft);

float3 SampleReflections(PositionInputs posInput, float3 reflectionVector, float smoothness, float mask, float4 screenPos, float3 wPos, float3 normal, float3 viewDir, float2 pixelOffset)
{
	LightLoopContext context;
	context.shadowContext  = InitShadowContext();
	context.shadowValue = 1;			
	context.sampleReflection = 0;
	context.splineVisibility = -1;

	#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
		context.positionWS = posInput.positionWS;
	#endif
	context.contactShadowFade = 0.0;
	context.contactShadow = 0;


	InitContactShadow(posInput, context);

	context.sampleReflection = 0;

	float weight = 1.0;
	context.sampleReflection = SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES;
	float3 probe = 0;

	//screenPos /= screenPos.w;
	//screenPos.xy += pixelOffset.xy * lerp(1.0, 0.1, unity_OrthoParams.w);
		
	float4 ssrLighting = LOAD_TEXTURE2D_X(_SsrLightingTexture, posInput.positionSS);
	InversePreExposeSsrLighting(ssrLighting);

	ApplyScreenSpaceReflectionWeight(ssrLighting);

	uint envLightStart , envLightCount;
	envLightCount = _PunctualLightCount;
	#ifdef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
		GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, i, envLightCount);
	#endif
	uint v_envLightListOffset = 0;
	uint v_envLightIdx = envLightStart;
	uint renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;

	probe = SAMPLE_TEXTURE2D_ARRAY_LOD(_ReflectionAtlas, s_trilinear_clamp_sampler, screenPos.xy, 1, 0).rgb;

	if (v_envLightListOffset < envLightCount)
	{
		v_envLightIdx = FetchIndex(envLightStart, v_envLightListOffset);
		#if SCALARIZE_LIGHT_LOOP
			uint s_envLightIdx = ScalarizeElementIndex(v_envLightIdx, fastPath);
		#else
			uint s_envLightIdx = v_envLightIdx;
		#endif
		if (s_envLightIdx == -1)
			break;

		EnvLightData ELD = FetchEnvLight(s_envLightIdx);
		float reflectionHierarchyWeight = 0.0; 
		if (s_envLightIdx >= v_envLightIdx)
		{
			v_envLightListOffset++;
			
			if (reflectionHierarchyWeight < 1.0 && IsMatchingLightLayer(ELD.lightLayers, renderingLayers) )
			{
				float weight = 1;
				float intersectionDistance = EvaluateLight_EnvIntersection(posInput.positionWS, normal, ELD, ELD.influenceShapeType, reflectionVector, weight);
				probe = SampleBlackWaterEnv(context, ELD.envIndex, reflectionVector, (11.0 - (smoothness * 11.0)), ELD.rangeCompressionFactorCompensation, posInput.positionNDC, pixelOffset.xy);
				//probe = SampleEnv(context, ELD.envIndex, reflectionVector, 0.0, ELD.rangeCompressionFactorCompensation, posInput.positionNDC);
				UpdateLightingHierarchyWeights(reflectionHierarchyWeight, weight);
				
				if (_EnableRecursiveRayTracing)
				{ }
				else
				{
					if (_EnableRayTracedReflections)
					{
	
						#ifdef N_F_ESSR_ON

							#if (!defined(N_F_TRANS_ON) || defined(N_F_CO_ON)) && !defined(N_F_FR_ON)

								if(smoothness < 0.9)
								{
									probe += (probe * weight) * ELD.multiplier;
								}

							#else

									probe += (probe * weight) * ELD.multiplier;

							#endif
						#else

							probe += (probe * weight) * ELD.multiplier;
						#endif

					}
					else
					{
						probe += (probe * weight) * ELD.multiplier;
						
					}

				}
			}
		}
	}

	#if !_RIVER //Planar reflections are pointless on curve surfaces, skip
		
		float4 planarLeft = SAMPLE_TEXTURE2D(_PlanarReflectionLeft, sampler_PlanarReflectionLeft, screenPos.xy);
		//Terrain add-pass can output negative alpha values. Clamp as a safeguard against this
		planarLeft.a = saturate(planarLeft.a);
	
		return lerp(probe, planarLeft.rgb, planarLeft.a * mask);
	#else
		return probe;
	#endif
}
