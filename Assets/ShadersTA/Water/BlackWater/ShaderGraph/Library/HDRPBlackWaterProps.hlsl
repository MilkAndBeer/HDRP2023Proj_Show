float _WorldSpaceUV;
float4 _AnimationParams;
float _RiverMode;

float _ShadingMode;
float _WaveLightStep;
float _WaveLightFeather;
float _EnableCartoonNormal;
float _Receiveshadows;
float _ShadowStrength;
float _TranslucencyOn;
float4 _TranslucencyParams;

float4 _BaseColor;
float4 _ShallowColor;
float _TotalAlphaStrength;
float4 _VertexColorMask;
float _DepthVertical;
float _DepthHorizontal;
float _DepthExp;
float _EdgeFade;
float4 _HorizonColor;
float _VerticalDepthAlpha;
float _HorizonDistance;
float _WaveTint;

float _NormalMapOn;
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
float _NormalTiling;
float _NormalStrength;
float _NormalSpeed;
float _EnableDistanceNormals;
float4 _DistanceNormalParams;
TEXTURE2D(_BumpMapLarge);
SAMPLER(sampler_BumpMapLarge);

float _IntersectionStyle;
float _IntersectionSource;
TEXTURE2D(_IntersectionNoise);
SAMPLER(sampler_IntersectionNoise);
float4 _IntersectionColor;
float _IntersectionLength;
float _IntersectionClipping;
float _IntersectionFalloff;
float _IntersectionTiling;
float _IntersectionSpeed;
float _IntersectionRippleDist;
float _IntersectionRippleStrength;

float _LightRefractionOn;
float _SunReflectionStrength;
float _SunReflectionSize;
float _SunReflectionDistortion;
float _PointSpotLightReflectionExp;
float _PointSpotLightStrength;
float _EnvRefractionOn;
float _ReflectionStrength;
float _ReflectionLighting;
float _ReflectionFresnel;
float _ReflectionDistortion;
float _ReflectionBlur;

float _WavesOn;
float _WaveSpeed;
float _WaveHeight;
float _WaveCount;
float4 _WaveDirection;
float _WaveDistance;
float _WaveSteepness;
float _WaveNormalStr;
float4 _WaveFadeDistance;

float _StencilRef;
TEXTURE2D(_V5InteraveWaveResult);
SAMPLER(sampler_V5InteraveWaveResult);
float4 _V5InteraveWavePosition;
float _V5InteraveWaveHeight;
float _V5InteraveWaterPlaneWidth;
float _V5InteraveWaterWaveTexOn;