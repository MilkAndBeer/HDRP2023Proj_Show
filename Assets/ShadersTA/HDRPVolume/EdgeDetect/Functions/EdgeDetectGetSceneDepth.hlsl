#ifndef GETEDGEDETECTSCENEDEPTH
#define GETEDGEDETECTSCENEDEPTH

TEXTURE2D_ARRAY(_OnlySceneDepthTex);

void GetOnlySceneDepth_float(float2 uv, out float depth)
{
    depth = SAMPLE_TEXTURE2D_ARRAY_LOD(_OnlySceneDepthTex, s_linear_clamp_sampler, uv, unity_StereoEyeIndex, 0).r;
    depth = Linear01Depth(depth, _ZBufferParams);
}
void GetOnlySceneDepth_half(float2 uv, out float depth)
{
    depth = SAMPLE_TEXTURE2D_ARRAY_LOD(_OnlySceneDepthTex, s_linear_clamp_sampler, uv, unity_StereoEyeIndex, 0).r;
    depth = Linear01Depth(depth, _ZBufferParams);
}
#endif 