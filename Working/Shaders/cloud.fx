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

float2 texOffset;
float2 uvScale;

float4x4 World;
float4x4 View;
float4x4 Projection;

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Normal : NORMAL0;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
};

VSOutput MainVS(float4 position : POSITION, float4 normal : NORMAL0, float2 texcoord : TEXCOORD0)
{
    VSOutput output = (VSOutput) 0;
    
    output.Normal = normalize(normal);
    output.Position = mul(mul(mul(position, World), View), Projection); // Apply standard transformations
    output.Coord = float3(texcoord * uvScale + texOffset, 0);
    output.WorldPos = position;
    
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
    
    output.Color0 *= 1-(distance(input.WorldPos, float3(0.5, 0.5, 0))*2);
    
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