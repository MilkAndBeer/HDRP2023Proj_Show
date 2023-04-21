struct Attributes
{
   float4 positionOS 	: POSITION;
	float4 uv 			: TEXCOORD0;	// Z is Random, W is Lifetime
	float4 uv1			: TEXCOORD1;	// X is Pan Offset, Y is UV Warp Strength, Z is Gravity
	float4 normalOS 	: NORMAL;
	float4 tangentOS 	: TANGENT;
	float4 color 		: COLOR0;
	
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 uv : TEXCOORD0;
    float4 positionCS : SV_POSITION;

	//wPos.x in w-component
	float4 normalWS 	: NORMAL;
	#if _NORMALMAP
	//wPos.y in w-component
	float4 tangentWS 		: TANGENT;
	//wPos.z in w-component
	float4 bitangentWS 	: TEXCOORD1;
	#else
	float3 positionWS 	: TEXCOORD1;
	#endif

	float4 screenPos 	: TEXCOORD2;

	float4 color 		: COLOR0;

	 UNITY_VERTEX_OUTPUT_STEREO
};

struct SurfaceData
{
    uint materialFeatures;
    real3 baseColor;
    real specularOcclusion;
    float3 normalWS;
    real perceptualSmoothness;
    real ambientOcclusion;
    real metallic;
    real coatMask;
    real3 specularColor;
    uint diffusionProfileHash;
    real subsurfaceMask;
    real transmissionMask;
    real thickness;
    float3 tangentWS;
    real anisotropy;
    real iridescenceThickness;
    real iridescenceMask;
    real3 geomNormalWS;
    real ior;
    real3 transmittanceColor;
    real atDistance;
    real transmittanceMask;
};