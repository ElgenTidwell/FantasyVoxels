﻿#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4x4 view_projection;
float noiseOffset;
texture ScreenTexture;
texture NormalTexture;
texture NoiseTexture;
texture AOTexture;
texture waterOverlay;
texture waterDUDV;
sampler TextureSampler : register(s0) = sampler_state
{
    Texture = <ScreenTexture>;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;

    AddressU = CLAMP;
    AddressV = CLAMP;
};
sampler NormalSampler : register(s1) = sampler_state
{
    Texture = <NormalTexture>;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;

    AddressU = CLAMP;
    AddressV = CLAMP;
};
sampler NoiseSampler : register(s2) = sampler_state
{
    Texture = <NoiseTexture>;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;

    AddressU = WRAP;
    AddressV = WRAP;
};
sampler AOSampler : register(s3) = sampler_state
{
    Texture = <AOTexture>;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;

    AddressU = CLAMP;
    AddressV = CLAMP;
};
sampler waterOverlaySampler : register(s4) = sampler_state
{
    Texture = <waterOverlay>;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;

    AddressU = WRAP;
    AddressV = WRAP;
};
sampler waterDUDVSampler : register(s5) = sampler_state
{
    Texture = <waterDUDV>;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;

    AddressU = WRAP;
    AddressV = WRAP;
};



float2 screenSize;
float3 cameraRotation;

bool underwater;

float2 AOSampleOffsets[8];

struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
};
struct PixelInput
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
};

PixelInput VShader(VertexInput v)
{
    PixelInput output;

    output.Position = mul(v.Position, view_projection);
    output.Color = v.Color;
    output.TexCoord = v.TexCoord;
    return output;
}

float CalcAO(float2 tex,float4 reference)
{
    float AO = 0;
    float Highlight = 0;
    for (int i = -8; i < 8; i++)
    {
        float2 coordOffset = (((tex2D(NoiseSampler, tex * i + float2(noiseOffset, noiseOffset)).rg - float2(1, 1))) * i*1) / screenSize;
        
        float3 testSample = tex2Dlod(NormalSampler, float4(tex+coordOffset*2, 0, 0));
        
        if (abs(testSample.r - reference.r) > 5)
            continue;
        
        AO += (testSample.r < reference.r && abs(testSample.g - reference.g) > 0.01f ? 1 : 0);
    }
    AO /= 16;
    Highlight /= 16;
    return 1 - (AO * 0.6) + Highlight * 0.8;
}

float4 applyVignette(float4 color, float2 pos)
{
    float2 position = pos - 0.5;
    float dist = length(position);
    
    float vignette = smoothstep(1, 1 - 0.6, dist);

    color.rgb = color.rgb * (vignette * 0.6 + 0.4);

    return color;
}
float4 PShader(PixelInput p) : COLOR0
{
    float2 watertexcoords = (p.TexCoord.xy - float2(cameraRotation.y, cameraRotation.x)) * (screenSize / 1024);
    
    float2 dudv = tex2D(waterDUDVSampler, watertexcoords);
    float4 overlay = tex2D(waterOverlaySampler, watertexcoords);
    
    if (underwater)
    {
        p.TexCoord.xy += ((dudv * 2 - float2(1, 1)) * 4) / screenSize;
    }
    
    float4 diffuse = tex2D(TextureSampler, p.TexCoord.xy);
    
    if (underwater)
    {
        diffuse.xyz *= (overlay.xyz * 0.8f + 0.2f);
    }
    
    diffuse = applyVignette(diffuse, p.TexCoord);
    
    float4 AO = tex2D(AOSampler,p.TexCoord.xy);
    
    return diffuse * p.Color * AO;
}
float4 AOShader(PixelInput p) : COLOR0
{
    float4 reference = tex2Dlod(NormalSampler, float4(p.TexCoord.xy, 0, 0));
    
    
    float4 AO = lerp(CalcAO(p.TexCoord.xy, reference), float4(1, 1, 1, 1), saturate((reference.r / 50)));
    
    return AO;
}

technique SpriteBatch
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL VShader();
        PixelShader = compile PS_SHADERMODEL PShader();
    }
}
technique AO
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL VShader();
        PixelShader = compile PS_SHADERMODEL AOShader();
    }
}