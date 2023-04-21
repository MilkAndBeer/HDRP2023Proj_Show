#ifndef GETEDGEDETECTTPLAYERMONSTERDEPTH
#define GETEDGEDETECTTPLAYERMONSTERDEPTH

TEXTURE2D_ARRAY(_OnlyPlayerColorTex);
TEXTURE2D_ARRAY(_OnlyMonsterColorTex);

TEXTURE2D_ARRAY(_OnlyPlayerDepthTex);
TEXTURE2D_ARRAY(_OnlyMonsterDepthTex);

void GetPlayerMonsterDepth_float(float2 uv, out float depth)
{
    float playerAlpha = SAMPLE_TEXTURE2D_ARRAY_LOD(_OnlyPlayerColorTex, s_linear_clamp_sampler, uv, unity_StereoEyeIndex, 0).a;
    float monsterAlpha = SAMPLE_TEXTURE2D_ARRAY_LOD(_OnlyMonsterColorTex, s_linear_clamp_sampler, uv, unity_StereoEyeIndex, 0).a;


    float playerDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(_OnlyPlayerDepthTex, s_linear_clamp_sampler, uv, unity_StereoEyeIndex, 0).r;
    float monsterDepth = SAMPLE_TEXTURE2D_ARRAY_LOD(_OnlyMonsterDepthTex, s_linear_clamp_sampler, uv, unity_StereoEyeIndex, 0).r;
    depth = max(playerDepth, monsterDepth);

    depth = Linear01Depth(depth, _ZBufferParams);
}
#endif 