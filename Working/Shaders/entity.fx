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

float4x4 World;
float4x4 View;
float4x4 Projection;

float tint;

float3 sunDirection;
float3 skyColor;
float3 skyBandColor;

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Normal : NORMAL0;
    float4 Color : COLOR0;
    float3 Coord : TEXCOORD0;
};

VSOutput MainVS(float4 position : POSITION, float4 normal : NORMAL0, float2 texcoord : TEXCOORD0)
{
    VSOutput output = (VSOutput) 0;
    
    output.Normal = normalize(normal);
    output.Position = mul(mul(mul(position, World), View), Projection); // Apply standard transformations
    output.Coord = float3(texcoord, 0);
    float d = (dot(output.Normal.xyz, float3(0.2, 0.8, 0))+1)/2;
    output.Color = float4(d,0,0,1);
    
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
    
    output.Color0 = tex2D(mainSampler,input.Coord.xy);
    
    clip(output.Color0.a - 0.1f);
    
    output.Color0.a = 1;
    output.Color0.xyz *= tint;
    output.Color0.xyz *= input.Color.x;
    
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