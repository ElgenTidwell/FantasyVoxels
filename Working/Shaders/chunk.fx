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
float4x4 InvProjection;

float FOV;
float renderDistance;
float minSafeDistance;
float3 cameraPosition;
float3 skyColor;
float4x4 cameraViewMatrix;
float2 dim;
float time;

int ChunkSize = 512;

texture voxels;
sampler3D voxelsSampler : register(s0) = sampler_state
{
    Texture = <voxels>;
};
texture colors;
sampler2D colorsSampler : register(s1) = sampler_state
{
    Texture = <colors>;
};


struct Ray
{
    float3 origin;
    float3 dir;
};
struct HitInfo
{
    bool hit;
    float3 color;
};
struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 WorldPos : NORMAL1;
    float4 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 Depth : TEXCOORD1;
    float2 Coord : TEXCOORD2;
};

VSOutput MainVS(float4 position : POSITION, nointerpolation float4 color : COLOR0, float4 normal : NORMAL0, float2 texcoord : TEXCOORD0)
{
    VSOutput output = (VSOutput) 0;
    output.WorldPos = mul(position, World) + cameraPosition;
    output.Normal = normal;
    
    output.Coord = texcoord;
    
    float3 tile = round(output.WorldPos);
    
    float3 vox = floor(color.xyz * 255);
    
    float effect = vox.b;
    float voxShadeEffect = (vox.g / 255.f);
    float voxAlphaEffect = 1;
    
    float4 finPos = position;
            
    [branch]
    if (effect == 1)
    {
        float sin1 = sin(time * 0.6 + radians(((tile.x / ChunkSize)) * 180));
        float sin2 = sin(time * 0.2 + radians(((tile.z / ChunkSize)) * 180));
        
        //Water
        voxShadeEffect = lerp((vox.g / 255.f), ((vox.g / 255.f) - 0.5f) * 2, abs(sin(time * 0.6 + radians(((tile.x / ChunkSize)) * 180))) * 0.3f);
        voxAlphaEffect = abs(sin1) * 0.1f + 1;
        
        finPos.y += sin1 * sin2;
    }
    else if (effect == 2)
    {
        //Double wave
        voxShadeEffect = (vox.g / 255.f) - (abs(sin(time * 1 + radians(((tile.z / ChunkSize) + (tile.x / ChunkSize)) * 2 * 180) + 1) / 2) * (abs(sin(time * 0.01 + radians(((tile.z / ChunkSize) + (tile.x / ChunkSize)) * 2 * 180)) + 1) / 2) * 0.1f);
    }
    output.Color = float4(voxShadeEffect, 0, 0, voxAlphaEffect);
    
    output.Position = mul(mul(mul(finPos, World), View), Projection); // Apply standard transformations
    
    output.Depth = float2(distance(output.WorldPos, cameraPosition), abs(normal.x) * 0.1f + abs(normal.y) * 0.5f + abs(normal.z) * 0.8f);
    
    return output;
}
struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldPos : NORMAL1;
    float4 Normal : NORMAL0;
    float2 Coord : TEXCOORD2;
    float2 Depth : TEXCOORD1;
    float4 Color : COLOR0;
};
struct PSOut
{
    float4 Color0 : SV_Target0;
    float4 Color1 : SV_Target1;
};


PSOut MainPS(PSInput input)
{
    PSOut output = (PSOut) 0;
    
    float2 depth = input.Depth;
    
    float fog = abs(depth.x) / (renderDistance*ChunkSize*0.5f);
    
    float4 dat = input.Color;
    
    float4 color = tex2D(colorsSampler,input.Coord);
    
    output.Color0 = float4(lerp(color.xyz * dat.r, skyColor, saturate(fog)), color.a * dat.a);
    
    if (color.a > 0.9f)
    {
        output.Color1 = float4(depth.xy, 0, 1);
    }
    else
    {
        output.Color1 = float4(0,0,0,0);
    }
    
    return output;
}


technique Terrain
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};