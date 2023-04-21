//AsteriaTrans_Func
//lxf

//=========================
void FaceSDF_float(float sdf_bias , float4 _HeadRight, float4 _HeadForward, float shadow1 , float shadow2 , float3 lightDir, out float finalAtten)
{

	//float shadow1 = SAMPLE_TEXTURE2D(_FaceShadowTexture, sampler_FaceShadowTexture, TRANSFORM_TEX(uv, _FaceShadowTexture)).r;
	//float shadow2 = SAMPLE_TEXTURE2D(_FaceShadowTexture, sampler_FaceShadowTexture, TRANSFORM_TEX(float2(-uv.x,uv.y), _FaceShadowTexture)).r;
	float rampSmooth = 0;

	float3 temp = cross(_HeadRight.xyz,_HeadForward.xyz);

	float2 Right = _HeadRight.xz;
	float2 Front = _HeadForward.xz;

    float2 sdf_lightDir = lightDir.xz;

	//float FrontdL = dot(float3(normalize(Front).x, normalize(Front).y, 0) , float3(normalize(lightDir).x, normalize(lightDir).z, 0));
	float FrontdL = dot(normalize(Front),normalize(sdf_lightDir));
	//float FrontdL_linear = acos(FrontdL) / 3.14;
	//float RightdL = dot(float3(normalize(Right).x, normalize(Right).y, 0) , float3(normalize(lightDir).x, normalize(lightDir).z, 0));
	float RightdL = dot(normalize(Right),normalize(sdf_lightDir));
	//float RightdL_linear = acos(RightdL) / 3.14;

    float FrontdLwrapped = (FrontdL * 0.5 + 0.5) + sdf_bias;
	float sdfAdjustment = 0.25;
    //float FrontdLwrappedReverse1 = FrontdL * 0.5 + 0.5 - sdf_bias;
    //float FrontdLwrappedReverse2 = FrontdL * 0.5 + 0.5 + sdf_bias;
    //float shadow = ShadowDirB.g;
    float shadowAtten1 = smoothstep(shadow1 - rampSmooth, shadow1 + rampSmooth, FrontdLwrapped);
    float shadowAtten2 = smoothstep(shadow2 - rampSmooth, shadow2 + rampSmooth, FrontdLwrapped);
    //float finalAtten =  (FrontdL < 0 ? shadowAtten1 : shadowAtten2);
	finalAtten =  RightdL < 0 ? shadowAtten1 : shadowAtten2;

	//finalAtten = RightdL;
	//return finalAtten;
	
}
