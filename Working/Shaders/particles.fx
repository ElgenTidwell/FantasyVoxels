#if OPENGL
#define SV_POSITION POSITION
#endif
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0

texture mainTexture;
sampler mainSampler : register(s0) = sampler_state
{
    Texture = <mainTexture>;
};
texture lightmap;
sampler lightmapSampler : register(s1) = sampler_state
{
    Texture = <lightmap>;
};

float4x4 World;
float4x4 View;
float4x4 Projection;

float tint;
float blocklightTint;

float uvScale;
float2 uvOffset;
int texIndex;
int atlasSize;

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

VSOutput MainVS(float4 position : POSITION, float2 texcoord : TEXCOORD0)
{
    VSOutput output = (VSOutput) 0;
    
    output.Position = mul(mul(mul(position, World), View), Projection); // Apply standard transformations
    output.Coord = float3((texcoord * uvScale) + uvOffset + float2(texIndex % atlasSize, floor(texIndex / atlasSize)), 0) / atlasSize;
    
    return output;
}

struct PSOut
{
    float4 Color0 : SV_Target0;
    float4 Color1 : SV_Target1;
};

PSOut MainPS(VSOutput input)
{
    PSOut output = (PSOut) 0;
    
    output.Color0 = tex2D(mainSampler, input.Coord.xy);
    
    clip(output.Color0.a - 0.1f);
    
    output.Color0.a = 1;
    output.Color0.xyz *= float3(tint, tint, tint) + tex2D(lightmapSampler, float2(pow(blocklightTint, 0.8f), 0)).xyz;
    
    return output;
}

technique Sky
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};