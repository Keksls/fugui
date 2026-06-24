#ifndef FUGUI_WORLD_COMMON_INCLUDED
#define FUGUI_WORLD_COMMON_INCLUDED

// Vertex layout used by FuguiWorldRenderFeature.
struct FuguiWorldVert
{
    float3 vertex       : POSITION;
    float2 uv           : TEXCOORD0;
    uint   color        : TEXCOORD1;
    float2 clipPosition : TEXCOORD2;
};

// Data interpolated into the fragment stage.
struct FuguiWorldVaryings
{
    float4 vertex       : SV_POSITION;
    float2 uv           : TEXCOORD0;
    half4  color        : COLOR;
    float2 clipPosition : TEXCOORD1;
};

CBUFFER_START(UnityPerMaterial)
    float4 _Texture_ST;
    float _TextureIsAlpha;
    float4 _ClipRect;
CBUFFER_END

TEXTURE2D(_Texture);
SAMPLER(sampler_Texture);

// Converts ImGui packed RGBA colors to Unity shader colors.
half4 FuguiWorldUnpackColor(uint c)
{
    half4 color = half4((c) & 0xff, (c >> 8) & 0xff, (c >> 16) & 0xff, (c >> 24) & 0xff) / 255.0h;
    #ifndef UNITY_COLORSPACE_GAMMA
        color.rgb = FastSRGBToLinear(color.rgb);
    #endif
    return color;
}

// Vertex stage for Fugui world-space meshes.
FuguiWorldVaryings FuguiWorldPassVertex(FuguiWorldVert input)
{
    FuguiWorldVaryings output = (FuguiWorldVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    output.vertex = TransformObjectToHClip(input.vertex);
    output.uv = float2(input.uv.x, 1.0 - input.uv.y);
    output.color = FuguiWorldUnpackColor(input.color);
    output.clipPosition = input.clipPosition;
    return output;
}

// Fragment stage for Fugui world-space meshes.
half4 FuguiWorldPassFrag(FuguiWorldVaryings input) : SV_Target
{
    clip(input.clipPosition.x - _ClipRect.x);
    clip(input.clipPosition.y - _ClipRect.y);
    clip(_ClipRect.z - input.clipPosition.x);
    clip(_ClipRect.w - input.clipPosition.y);

    half4 tex = SAMPLE_TEXTURE2D(_Texture, sampler_Texture, input.uv);
    if (_TextureIsAlpha > 0.5h)
    {
        tex = half4(1.0h, 1.0h, 1.0h, tex.a);
    }

    return input.color * tex;
}

#endif
