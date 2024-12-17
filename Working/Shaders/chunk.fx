#if OPENGL
#define SV_POSITION POSITION
#endif
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 InvProjection;
float4x4 LightViewProj;

float3 sunColor;
float3 sunDir;

float FOV;
float renderDistance;
float minSafeDistance;
float3 cameraPosition;
float3 skyColor;
float3 skyBandColor;
float4x4 cameraViewMatrix;
float2 dim;
float time;

int ChunkSize = 512;

texture normal;
sampler2D normalMapSampler : register(s0) = sampler_state
{
    Texture = <normal>;
};
texture colors;
sampler2D colorsSampler : register(s1) = sampler_state
{
    Texture = <colors>;
};
texture shadowmap;
sampler2D shadowmapSampler : register(s2) = sampler_state
{
    Texture = <shadowmap>;
};
texture lightmap;
sampler2D lightmapSampler : register(s3) = sampler_state
{
    Texture = <lightmap>;
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
    float3 WorldPos : COLOR1;
    float4 Normal : NORMAL0;
    float3 NormalPS : TEXCOORD3;
    float4 Color : COLOR0;
    float3 Depth : TEXCOORD1;
    float2 Coord : TEXCOORD2;
    float4 ShadowCoord : TEXCOORD4;
};

VSOutput MainVS(half4 position : POSITION, nointerpolation float4 color : COLOR0, float4 normal : NORMAL0, float2 texcoord : TEXCOORD0)
{
    VSOutput output = (VSOutput) 0;
    output.WorldPos = mul(position, World).xyz + cameraPosition;
    output.Normal = normal;
    
    //float2 coord = float2(0, 0);
    
    //if (normal.x != 0)
    //{
    //    coord = position.zy;
    //}
    //else if (normal.y != 0)
    //{
    //    coord = position.xz;
    //}
    //else
    //{
    //    coord = position.xy;
    //}
    
    //coord.x %= 1;
    //coord.y %= 1;
    
    //coord.x += color.a*9;
    
    //coord.x /= 9;
    
    //output.AOCoord = coord;
    
    output.NormalPS = normal.xyz;
    
    output.Coord = texcoord;
    
    float3 tile = round(output.WorldPos);
    
    float3 vox = floor(color.xyz * 255);
    
    float effect = vox.r;
    float voxShadeEffect = 1;
    float voxAlphaEffect = 1;
    
    float4 finPos = position;
            
    [branch]
    if (effect == 1) //waving water
    {
        float timeOffset = radians(((tile.x / ChunkSize)) * 180);
        float timeOffset2 = radians(((tile.z / ChunkSize)) * 180);
        float sin1 = (sin(time * 0.6 + timeOffset)+1)*0.5f;
        float sin2 = (sin(time * 0.2 + timeOffset2)+1)*0.5f;
        
        //Water
        voxShadeEffect = lerp(1.8f, 0.1f, abs(sin(time * 0.6 + radians(((tile.x / ChunkSize)) * 180))) * 0.3f);
        voxAlphaEffect = abs(sin1) * 0.1f + 1;
        
        finPos.y += (sin1 * sin2)*0.2f;
    }
    else if (effect == 2)
    {
        float sin1 = (sin(time * 0.4 + (tile.x + tile.z - tile.y)*0.5f) + 1) * 0.5f;
        float sin2 = (sin(time * 0.4 + (tile.x + tile.y + tile.z)*0.5f) + 1) * 0.5f;
        
        voxShadeEffect = 1 - (abs(sin(time * 1 + radians(((tile.z / ChunkSize) + (tile.x / ChunkSize)) * 2 * 180) + 1) / 2) * (abs(sin(time * 0.01 + radians(((tile.z / ChunkSize) + (tile.x / ChunkSize)) * 2 * 180)) + 1) / 2) * 0.1f);
    
        finPos.x += sin1 * 0.1f;
        finPos.z += sin2 * 0.1f;
    }
    
    output.ShadowCoord = float4(0,0,0, dot(-sunDir, normal.xyz));
    
    output.Color = float4(voxShadeEffect, color.g, color.b, voxAlphaEffect);
    
    output.Position = mul(mul(mul(finPos, World), View), Projection); // Apply standard transformations
    
    output.Depth = float3(distance(output.WorldPos, cameraPosition), abs(normal.x) * 0.1f + abs(normal.y) * 0.5f + abs(normal.z) * 0.8f, output.Position.z / output.Position.w);
    
    return output;
}
struct PSOut
{
    float4 Color0 : SV_Target0;
    float4 Color1 : SV_Target1;
};

// Calculates the shadow term using PCF with edge tap smoothing
float CalcShadowTermSoftPCF(float light_space_depth, float ndotl, float2 shadow_coord, int iSqrtSamples)
{
    float fShadowTerm = 0;

       //float2 v_lerps = frac(ShadowMapSize * shadow_coord);

    float variableBias = clamp(0.001 * tan(acos(ndotl)), 0.0012f, 0.003f);
    //safe to assume it's a square
    float shadowMapSize = 2048;
    	
    float fRadius = (iSqrtSamples-1); //mad(iSqrtSamples, 0.5, -0.5);//(iSqrtSamples - 1.0f) / 2;

    for (float y = -fRadius; y <= fRadius; y++)
    {
        for (float x = -fRadius; x <= fRadius; x++)
        {
            float2 vOffset = 0;
            vOffset = float2(x, y);
            vOffset /= shadowMapSize;
            //vOffset *= 2;
            //vOffset /= variableBias*200;
            float2 vSamplePoint = shadow_coord + vOffset;
            float fDepth = tex2D(shadowmapSampler, vSamplePoint).x;
            float fSample = (light_space_depth > fDepth + variableBias);
            
            // Edge tap smoothing
            float xWeight = 1;
            float yWeight = 1;
            
            if (x == -fRadius)
                xWeight = 1 - frac(shadow_coord.x * shadowMapSize);
            else if (x == fRadius)
                xWeight = frac(shadow_coord.x * shadowMapSize);
                
            if (y == -fRadius)
                yWeight = 1 - frac(shadow_coord.y * shadowMapSize);
            else if (y == fRadius)
                yWeight = frac(shadow_coord.y * shadowMapSize);
                
            fShadowTerm += fSample * xWeight * yWeight;
        }
    }
    
    fShadowTerm /= (fRadius * fRadius * 4);
    
    return fShadowTerm;
}

PSOut MainPS(VSOutput input)
{
    PSOut output = (PSOut) 0;
    
    float2 depth = input.Depth.xy;
    
    float fog = max(depth.x - ChunkSize * 0.5f, 0) / ((renderDistance+1) * ChunkSize * 0.5f);
    float fogColor = pow(saturate(normalize(input.WorldPos - cameraPosition).y),1.25f);
    
    float4 dat = input.Color;
    
    float4 color = tex2Dlod(colorsSampler, float4(input.Coord,0,0));
    clip(color.a - 0.02f);
    
    //float3 tangent;
    //float3 binormal;
    //float3 c1 = cross(input.NormalPS, float3(0.0, 0.0, 1.0));
    //float3 c2 = cross(input.NormalPS, float3(0.0, 1.0, 0.0));

    //if (length(c1) > length(c2))
    //{
    //    tangent = c1;
    //}
    //else
    //{
    //    tangent = c2;
    //}

    //tangent = normalize(tangent);

    //binormal = cross(input.NormalPS, tangent);
    //binormal = normalize(binormal);
    
    //// Calculate the normal, including the information in the bump map
    //float3 bump = 0.5f * (tex2D(normalMapSampler, input.Coord) - float3(0.5, 0.5, 0.5));
    //float3 normal = input.NormalPS + (bump.x * tangent + bump.y * binormal);
    //normal = normalize(normal);
    
    //float3 realBump = dot(normal, sunDir) - dot(input.NormalPS, sunDir);
    
    float3 normal = input.NormalPS;
    float realBump = 0;
    
    
    float4 lightingPosition = mul(float4(floor((input.WorldPos + float3(0.001f,0.001f,0.001f)) * 16.0f) / 16.0f, 1), LightViewProj);
    
    // Find the position in the shadow map for this pixel
    float2 ShadowTexCoord = mad(0.5f, lightingPosition.xy / lightingPosition.w, float2(0.5f, 0.5f));
    ShadowTexCoord.y = 1.0f - ShadowTexCoord.y;
    
	// Get the current depth stored in the shadow map
    float ourdepth = (lightingPosition.z / lightingPosition.w);
    
    float4 shadowCoords = input.ShadowCoord;
    float shadowContributioncasc1 = CalcShadowTermSoftPCF(ourdepth, shadowCoords.w, ShadowTexCoord, 2);
    
    float shadowdistance = pow(saturate(1 - (abs(depth.x) / (32 * ChunkSize))), 8);
    
    shadowContributioncasc1 *= shadowdistance;
    
    float3 lmp = tex2D(lightmapSampler, float2(pow(dat.b, 0.8f), (time%4) / 4));
    
    float3 desCol = color.xyz * (lmp + saturate(dat.g * (1 - shadowContributioncasc1 * 0.8f * saturate(dot(input.NormalPS, sunDir)) * (1-pow(dat.b, 0.8f))) - 0.025f) * (realBump + 1) * sunColor);

    output.Color0 = float4(lerp(desCol, lerp(skyBandColor, skyColor, fogColor), pow(saturate(fog),1.2f)), color.a * dat.a);
    
    if (color.a > 0.9f)
    {
        output.Color1 = float4(depth.xy, 0, 1);
    }
    else
    {
        output.Color1 = float4(0, 0, 0, 0);
    }
    
    return output;
}

float4 ShadowPS(VSOutput input) : SV_Target0
{
    float3 depth = input.Depth;
    
    float4 color = tex2Dlod(colorsSampler, float4(input.Coord, 0, 0));
    clip(color.a - 0.02f);
    
    return float4(depth.z, 0, 0, 1);
}

technique Terrain
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};

technique Shadow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL ShadowPS();
    }
};