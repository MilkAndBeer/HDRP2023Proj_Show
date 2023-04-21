#ifndef INCLUDE_CUSTOMEDGEOUTLINEOFFSET
#define INCLUDE_CUSTOMEDGEOUTLINEOFFSET

void EdgeOutLineUVOffset_float(float4 screenPos, float2 screenWH, float edgeSize,
        out float2 uv0, out float2 uv1, out float2 uv2, out float2 uv3,
        out float2 uv4, out float2 uv5, out float2 uv6, out float2 uv7, out float2 uv8)
{
        half2 uv[9];
        uv[0] = screenPos.xy + edgeSize * half2(-1, -1)/screenWH;
        uv[1] = screenPos.xy + edgeSize * half2(0, -1)/screenWH;
        uv[2] = screenPos.xy + edgeSize * half2(1, -1)/screenWH;
        uv[3] = screenPos.xy + edgeSize * half2(-1, 0)/screenWH;
        uv[4] = screenPos.xy;
        uv[5] = screenPos.xy + edgeSize * half2(1, 1)/screenWH;
        uv[6] = screenPos.xy + edgeSize * half2(0, 1)/screenWH;
        uv[7] = screenPos.xy + edgeSize * half2(-1, 1)/screenWH;
        uv[8] = screenPos.xy + edgeSize * half2(1, 0)/screenWH;

        uv0 = uv[0];
        uv1 = uv[1];
        uv2 = uv[2];
        uv3 = uv[3];
        uv4 = uv[4];
        uv5 = uv[5];
        uv6 = uv[6];
        uv7 = uv[7];
        uv8 = uv[8];
}

void DepthEdgeDectection_float(float depth0, float depth1, float depth2, float depth3, float depth4,
        float depth5, float depth6, float depth7, float depth8, out float outDepth)
{
    float tempDepth1 = (depth0 - depth4) + (depth1 - depth4) + (depth2 - depth4) + (depth3 - depth4) + 
    (depth5 - depth4) + (depth6 - depth4) + (depth7 - depth4) + (depth8 - depth4); 
    float tempDepth2 = max(max(max(max(depth0, depth1), depth2), depth3),max(max(max(depth5, depth6), depth7), depth8));

    outDepth = tempDepth1/tempDepth2;
}

#endif