Varyings BaseVert (Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv.xy = input.uv.xy;
    output.uv.z = _TimeParameters.x;
    output.uv.w = 0;

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	float3 offset = 0;

    output.normalWS.xyz = TransformObjectToWorldNormal(input.normalOS.xyz);
    #ifdef _NORMALMAP
        output.tangentWS.xyz = TransformObjectToWorldDir(input.tangentOS.xyz);
        real sign = real(input.tangentOS.w) * GetOddNegativeScale();
        output.bitangentWS.xyz = real3(cross(output.normalWS.xyz, float3(output.tangentWS.xyz))) * sign;
    #endif

    float4 vertexColor = GetVertexColor(input.color.rgba, _VertexColorMask.rgba);
    #if _WAVES
        float2 uv = GetSourceUV(input.uv.xy, positionWS.xz, _WorldSpaceUV);
        //Vertex animation
	    WaveInfo waves = GetWaveInfo(uv, TIME_VERTEX * _WaveSpeed, _WaveHeight, lerp(1, 0, vertexColor.b), _WaveFadeDistance.x, _WaveFadeDistance.y);
	    //Offset in direction of normals (only when using mesh uv)
	    if(_WorldSpaceUV == 0) waves.position *= output.normalWS.xyz;
	
	    offset += waves.position.xyz;
    #endif

    //Apply vertex displacements
	positionWS += offset;

    output.positionCS = TransformWorldToHClip(positionWS);
    output.screenPos = ComputeScreenPos(output.positionCS);

    #ifdef _NORMALMAP
        output.normalWS.w = positionWS.x;
        output.tangentWS.w = positionWS.y;
        output.bitangentWS.w = positionWS.z;
    #else
        output.positionWS = positionWS;
    #endif

    output.color = vertexColor;

    return output;
}