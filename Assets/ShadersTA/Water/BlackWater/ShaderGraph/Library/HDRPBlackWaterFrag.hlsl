struct WaterSurface
{
	float alpha;
    float3 positionWS;
    float3 viewDir;

    float3 vertexNormal;
	float3 waveNormal;	
	float3 offset;
	float3 tangentNormal;
	float3 tangentWorldNormal;
	half3x3 tangentToWorldMatrix;

    float4 albedo;
	float3 reflections;
	float3 specular;
	half reflectionMask;
    float fog;
    float intersection;
};

struct SceneData
{
    float4 positionSS;
    float viewDepth;
    float verticalDepth;
    float3 positionWS;
};

#define COLLAPSIBLE_GROUP 1

void PopulateSceneData(inout SceneData scene, Varyings input, WaterSurface water)
{
    #ifdef SCREEN_POS
        scene.positionSS = input.screenPos;
    #endif

    //Default for disabled depth texture
	scene.viewDepth = 1;
	scene.verticalDepth = 1;

    SceneDepth depth = SampleDepth(scene.positionSS);

	scene.positionWS = ReconstructViewPos(scene.positionSS, water.viewDir, depth);
    	//Invert normal when viewing backfaces
	float normalSign = ceil(dot(normalize(water.viewDir), water.waveNormal));
	normalSign = normalSign == 0 ? -1 : 1;

	//Z-distance to opaque surface
	scene.viewDepth = SurfaceDepth(depth, input.positionCS);
	//Distance to opaque geometry in normal direction
	scene.verticalDepth = DepthDistance(water.positionWS, scene.positionWS, water.waveNormal * normalSign);
    
    //TODO shadow

    #if !_RIVER
        half VdotN = 1.0 - saturate(dot(SafeNormalize(water.viewDir), water.waveNormal));
		float grazingTerm = saturate(pow(VdotN, 64));
	
		//Resort to z-depth at surface edges. Otherwise makes intersection/edge fade visible through the water surface
		scene.verticalDepth = lerp(scene.verticalDepth, scene.viewDepth, grazingTerm);
    #endif
}

float GetWaterDensity(SceneData scene, float mask)
{
	//Best default value, otherwise water just turns invisible (infinitely shallow)
	float density = 1.0;
	
	#if !_DISABLE_DEPTH_TEX

	float viewDepth = scene.viewDepth;
	float verticalDepth = scene.verticalDepth;

		#if defined(RESAMPLE_REFRACTION_DEPTH)
		viewDepth = scene.viewDepthRefracted;
		verticalDepth = scene.verticalDepthRefracted;
		#endif

	float depthAttenuation = 1.0 - exp(-viewDepth * _DepthVertical * lerp(0.1, 0.01, unity_OrthoParams.w));
	float heightAttenuation = saturate(lerp(verticalDepth * _DepthHorizontal, 1.0 - exp(-verticalDepth * _DepthHorizontal), _DepthExp));
	
	density = max(depthAttenuation, heightAttenuation);
	
	#endif

	#if !_RIVER
	//Use green vertex color channel to control density
	density *= saturate(density - mask);
	#endif

	return density;
}

half4 BaseFrag (Varyings input) : SV_Target
{
     UNITY_SETUP_INSTANCE_ID(input);
     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    //Initialize with null values. Anything that isn't assigned, shouldn't be used either
	WaterSurface water = (WaterSurface)0;
	SceneData scene = (SceneData)0;

	water.alpha = 1.0;
	int faceSign = 1;

    /* ========
	// GEOMETRY DATA
	=========== */
    #ifdef COLLAPSIBLE_GROUP
        float4 vertexColor = input.color; //Mask already applied in vertex shader
        //Vertex normal in world-space
	    float3 normalWS = normalize(input.normalWS.xyz);
        float3 positionWS = (float3)0;
        #ifdef _NORMALMAP
    	    float3 WorldTangent = input.tangentWS.xyz;
	        float3 WorldBiTangent = input.bitangentWS.xyz;
            positionWS = float3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
        #else
            positionWS = input.positionWS;
        #endif

        water.positionWS = positionWS;
        water.viewDir = GetCurrentViewPosition() - positionWS;
        float3 viewDirNorm = normalize(water.viewDir);
        half VdotN = 1.0 - saturate(dot(viewDirNorm * faceSign, normalWS));
		
        water.vertexNormal = normalWS;
    
        float2 uv = GetSourceUV(input.uv.xy, positionWS.xz, _WorldSpaceUV);
    #endif

    /* ========
	// WAVES
	=========== */
    #ifdef COLLAPSIBLE_GROUP
        water.waveNormal = normalWS;
    	
        #if _WAVES
            WaveInfo waves = GetWaveInfo(uv, TIME * _WaveSpeed, _WaveHeight,  lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);
            waves.normal = lerp(waves.normal, normalWS, lerp(0, 1, vertexColor.b));
            water.waveNormal = BlendNormalWorldspaceRNM(waves.normal, normalWS, water.vertexNormal);

            water.offset.y += waves.position.y;
            water.offset.xz += waves.position.xz * 0.5;

            if(_WorldSpaceUV == 1) uv = GetSourceUV(input.uv.xy, positionWS.xz + water.offset.xz, _WorldSpaceUV);
        #endif
    #endif

    /* ========
	// SHADOWS
	=========== */
    //TODO

    /* ========
	// NORMALS
	=========== */
    #if COLLAPSIBLE_GROUP
        water.tangentNormal = float3(0.5, 0.5, 1);
	    water.tangentWorldNormal = water.waveNormal;
        #if _NORMALMAP
            //Tangent-space
	        water.tangentNormal = SampleNormals(uv * _NormalTiling, positionWS, TIME, _NormalSpeed, 0, 1) * (_NormalStrength + 0.0001);
			water.tangentToWorldMatrix = half3x3(WorldTangent, WorldBiTangent, water.waveNormal);
        
			//World-space
			water.tangentWorldNormal = normalize(TransformTangentToWorld(water.tangentNormal, water.tangentToWorldMatrix));	
		#endif
	
    #endif
	
    //Normals can perturb the screen coordinates, so needs to be calculated first
	PopulateSceneData(scene, input, water);

    /* =========
	// COLOR + FOG
	============ */
    #if COLLAPSIBLE_GROUP
        water.fog = GetWaterDensity(scene, vertexColor.g);

        //Albedo
	    float4 baseColor = lerp(_ShallowColor, _BaseColor, water.fog);
        baseColor.rgb += _WaveTint * water.offset.y;
        water.fog *= baseColor.a;
	    water.alpha = baseColor.a;

        water.albedo.rgb = baseColor.rgb;	
    #endif

    /* ========
	// INTERSECTION FOAM
	=========== */
    #if COLLAPSIBLE_GROUP
        water.intersection = 0;
        #if _SHARP_INERSECTION || _SMOOTH_INTERSECTION

		float interSecGradient = 0;
	
		#if !_DISABLE_DEPTH_TEX
		interSecGradient = 1-saturate(exp(scene.verticalDepth) / _IntersectionLength);	
		#endif
	
		if (_IntersectionSource == 1) interSecGradient = vertexColor.r;
		if (_IntersectionSource == 2) interSecGradient = saturate(interSecGradient + vertexColor.r);

		water.intersection = SampleIntersection(uv.xy, interSecGradient, TIME * _IntersectionSpeed) * _IntersectionColor.a;

		#if _WAVES
		//Prevent from peering through waves when camera is at the water level
		if(positionWS.y < scene.positionWS.y) water.intersection = 0;
		#endif

		//water.density += water.intersection;
	
		//Flatten normals on intersection foam
		water.waveNormal = lerp(water.waveNormal, normalWS, water.intersection);
        #endif
    #endif

    	
	/* ========
	// EMISSION (Caustics + Specular)
	=========== */
    #if COLLAPSIBLE_GROUP
		input.screenPos.xy = _OffScreenRendering > 0 ? (uint2)round(input.screenPos.xy * _OffScreenDownsampleFactor) : input.screenPos.xy;
		uint2 tileIndex = uint2(input.screenPos.xy) / GetTileSize();
		PositionInputs posInput = GetPositionInput(input.screenPos.xy, _ScreenSize.zw, input.screenPos.z, input.screenPos.w, positionWS.xyz, tileIndex);

		#ifdef _SPECULARHIGHLIGHTS_ON
			float3 sunReflectionNormals = water.tangentWorldNormal;

			#if _FLAT_SHADING //Use face normals
			sunReflectionNormals = water.waveNormal;
			#endif
			uint renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;

			for (int i = 0; i < _DirectionalLightCount; ++i)
			{
				DirectionalLightData mainLight = _DirectionalLightDatas[i];
				if (IsMatchingLightLayer(mainLight.lightLayers, renderingLayers))
				{
					half3 sunSpec = DirectionalSpecularReflection(mainLight, viewDirNorm, sunReflectionNormals, _SunReflectionDistortion, _SunReflectionSize, _SunReflectionStrength, positionWS);
					//sunSpec.rgb *= saturate((1-water.intersection)); //Hide
	
					water.specular += sunSpec;
				}
			}

			//TODO Point SpecualrÐÞ¸´
			if (LIGHTFEATUREFLAGS_PUNCTUAL)
			{
				//-- Point Lights -----------
				int pointlightsCount = _PunctualLightCount;
				#ifdef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
					GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, i, pointlightsCount);
				#endif
				for (i = 0; i < pointlightsCount; ++i)
				{
					 LightData Plight = FetchLight(i);

					 if (IsMatchingLightLayer(Plight.lightLayers, renderingLayers))
					 {
						half3 pointSpec = PointSpecularReflection(Plight, viewDirNorm, water.tangentWorldNormal, positionWS);
						//pointSpec.rgb *= saturate((1-water.intersection));
						water.specular += pointSpec;
					 }
				}
			}
		#endif
		
		//Reflection probe/planar
		#ifdef _ENVIRONMENTREFLECTIONS_ON
			//Blend between smooth surface normal and normal map to control the reflection perturbation (probes only!)
			#if !_FLAT_SHADING 
				float3 refWorldTangentNormal = lerp(water.waveNormal, normalize(water.waveNormal + water.tangentWorldNormal), _ReflectionDistortion);
			#else //Skip, not a good fit
				float3 refWorldTangentNormal = water.waveNormal;
			#endif
	
			float3 reflectionVector = reflect(-viewDirNorm, refWorldTangentNormal);

			#if !_RIVER
				//Ensure only the top hemisphere of the reflection probe is used
				reflectionVector.y = max(0, reflectionVector.y);
			#endif
	
			//Pixel offset for planar reflection, sampled in screen-space
			float2 reflectionPerturbation = lerp(water.waveNormal.xz, water.tangentNormal.xy, _ReflectionDistortion).xy;
			water.reflections = SampleReflections(posInput, reflectionVector, _ReflectionBlur, 1, scene.positionSS.xyzw, positionWS, refWorldTangentNormal, viewDirNorm, reflectionPerturbation);
			
			half reflectionFresnel = ReflectionFresnel(refWorldTangentNormal, viewDirNorm, _ReflectionFresnel);
			water.reflectionMask = _ReflectionStrength * reflectionFresnel;
			//water.reflectionMask = saturate(water.reflectionMask - water.intersection) * _ReflectionStrength;
		#endif
	#endif
			
	#if  _SHARP_INERSECTION || _SMOOTH_INTERSECTION
		water.alpha = saturate(water.alpha + water.intersection);
	#endif

	water.albedo.rgb = lerp(water.albedo, lerp(water.albedo.rgb, water.reflections, water.reflectionMask), _ReflectionLighting);

	float fresnel = saturate(pow(VdotN, _HorizonDistance));
	water.albedo.rgb = lerp(water.albedo.rgb, _HorizonColor.rgb, fresnel * _HorizonColor.a);

	return half4((half3)water.albedo + water.specular, water.alpha);
}