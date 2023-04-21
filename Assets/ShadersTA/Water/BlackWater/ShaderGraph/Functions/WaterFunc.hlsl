void GetSourceUV_float(float2 sourceUV, float2 time, float speed, float riverOn, out float4 outputUV) 
{
	float notRiverOn = riverOn ? 0 : 1;
	time.x *= notRiverOn;
	float2 uv1 = sourceUV.xy + (time.xy * speed);

	float2 uv2River = (sourceUV.xy * 0.5) + ((1 - time.xy) * speed * 0.5);
	float2 uv2Land = (sourceUV.xy * 0.5) + (time.xy * speed);;

	float2 uv2 = riverOn ? uv2River : uv2Land;

	outputUV = float4(uv1.xy, uv2.xy);
}

void GetSourceUV_half(float2 sourceUV, float2 time, float speed, float riverOn, out float4 outputUV) 
{
	float notRiverOn = riverOn ? 0 : 1;
	time.x *= notRiverOn;
	float2 uv1 = sourceUV.xy + (time.xy * speed);

	float2 uv2River = (sourceUV.xy * 0.5) + ((1 - time.xy) * speed * 0.5);
	float2 uv2Land = (sourceUV.xy * 0.5) + (time.xy * speed);;

	float2 uv2 = riverOn ? uv2River : uv2Land;

	outputUV = float4(uv1.xy, uv2.xy);
}

void GetPerturbNormal_Tangent_float(float3 surf_pos, float3 surf_tangent, float3 surf_bitTangent, float3 surf_norm, float height, float scale, out float3 outNormal)
{
	// "Bump Mapping Unparametrized Surfaces on the GPU" by Morten S. Mikkelsen
	float3 vSigmaS = ddx( surf_pos );
	float3 vSigmaT = ddy( surf_pos );
	float3 vN = surf_norm;
	float3 vR1 = cross( vSigmaT , vN );
	float3 vR2 = cross( vN , vSigmaS );
	float fDet = dot( vSigmaS , vR1 );
	float dBs = ddx( height );
	float dBt = ddy( height );
	float3 vSurfGrad = scale * 0.05 * sign( fDet ) * ( dBs * vR1 + dBt * vR2 );
	outNormal = normalize( abs( fDet ) * vN - vSurfGrad );
	float3x3 ase_worldToTangent = float3x3(surf_tangent,surf_bitTangent,surf_norm);
	outNormal = mul( ase_worldToTangent, outNormal);
}
void GetPerturbNormal_Tangent_half(float3 surf_pos, float3 surf_tangent, float3 surf_bitTangent, float3 surf_norm, float height, float scale, out float3 outNormal)
{
	// "Bump Mapping Unparametrized Surfaces on the GPU" by Morten S. Mikkelsen
	float3 vSigmaS = ddx( surf_pos );
	float3 vSigmaT = ddy( surf_pos );
	float3 vN = surf_norm;
	float3 vR1 = cross( vSigmaT , vN );
	float3 vR2 = cross( vN , vSigmaS );
	float fDet = dot( vSigmaS , vR1 );
	float dBs = ddx( height );
	float dBt = ddy( height );
	float3 vSurfGrad = scale * 0.05 * sign( fDet ) * ( dBs * vR1 + dBt * vR2 );
	outNormal = normalize( abs( fDet ) * vN - vSurfGrad );
	float3x3 ase_worldToTangent = float3x3(surf_tangent,surf_bitTangent,surf_norm);
	outNormal = mul( ase_worldToTangent, outNormal);
}