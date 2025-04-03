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
float4x4 RotWorld;
float4x4 View;
float4x4 Projection;

float tint;
float blocklightTint;
float3 colorTint;

float3 sunDirection;
float3 skyColor;
float3 skyBandColor;
float renderDistance;
float3 cameraPosition;

int ChunkSize = 512;

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Normal : COLOR1;
    float4 Color : COLOR0;
    float3 WorldPos : TEXCOORD2;
    float3 Coord : TEXCOORD0;
    float3 Depth : TEXCOORD1;
};

VSOutput MainVS(float4 position : POSITION, float4 normal : NORMAL0, float2 texcoord : TEXCOORD0, float3 color : COLOR0)
{
    VSOutput output = (VSOutput) 0;
    
    output.Normal = normalize(mul(normal,RotWorld));
    output.Position = mul(mul(mul(position, World), View), Projection); // Apply standard transformations
    output.WorldPos = mul(position, World).xyz + cameraPosition;
    output.Coord = float3(texcoord, 0);
    output.Color = float4(color.xyz, 1);
    output.Depth = float3(distance(output.WorldPos, cameraPosition), abs(normal.x) * 0.1f + abs(normal.y) * 0.5f + abs(normal.z) * 0.8f, output.Position.z / output.Position.w);
    
    
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
    output.Color0.xyz *= float3(tint, tint, tint) + tex2D(lightmapSampler, float2(pow(blocklightTint, 0.8f), 0)).xyz;
    output.Color0.xyz *= (dot(input.Normal.xyz, float3(0.1, 0.9, 0)) * 0.5 + 0.5) * input.Color.xyz * colorTint;
    
    float2 depth = input.Depth.xy;
    
    float fog = max(depth.x - ChunkSize * 0.5f, 0) / ((renderDistance + 1) * ChunkSize * 0.5f);
    float fogColor = pow(saturate(normalize(input.WorldPos - cameraPosition).y), 1.25f);
    output.Color0 = float4(lerp(output.Color0.xyz, lerp(skyBandColor, skyColor, fogColor), pow(saturate(fog), 1.2f)), output.Color0.a);
    
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