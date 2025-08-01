#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

#ifndef SLZ_MODIFIED
#define SLZ_MODIFIED

float SampleSceneDepth(float2 uv)
{
// SLZ MODIFIED - Use LOD sample, normal sample does pointless derivative calculations which break branching
#ifdef SLZ_MODIFIED
    return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv), 0).r;
#else
    return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
#endif // SLZ_MODIFIED
// END SLZ MODIFIED
}

#undef SLZ_MODIFIED
#endif // SLZ_MODIFIED

float LoadSceneDepth(uint2 uv)
{
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, uv).r;
}
#endif
