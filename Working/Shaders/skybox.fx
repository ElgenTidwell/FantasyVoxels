#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;

float3 cameraPosition;
float3 cameraForward;
float3 sunDirection;
float3 skyColor;
float3 skyBandColor;

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Normal : NORMAL0;
    float4 SBoxData : TEXCOORD0;
};

VSOutput MainVS(float4 position : POSITION, nointerpolation float4 color : COLOR0, float4 normal : NORMAL0, float2 texcoord : TEXCOORD0)
{
    VSOutput output = (VSOutput) 0;
    
    output.Normal = normal;
    output.Position = mul(mul(mul(position, World), View), Projection); // Apply standard transformations
    
    output.SBoxData.x = (saturate(normalize(position).y));
    
    output.SBoxData.yzw = position.xyz;
    
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
    
    output.Color0 = float4(lerp(skyColor, skyBandColor, (1 - (pow(input.SBoxData.x, 0.8f)))), 1);
    
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