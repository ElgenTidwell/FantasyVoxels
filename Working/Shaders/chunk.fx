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

bool orthoCamera;
float orthoWidth;
float orthoHeight;

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
};

VSOutput MainVS(float4 position : POSITION)
{
    VSOutput output;
    output.Position = mul(mul(mul(position, World), View), Projection); // Apply standard transformations
    return output;
}
struct PSInput
{
    float4 Position : SV_POSITION;
    float2 vPos : VPOS;
};

float nrand(float3 seed)
{
    // Combine the components of the seed using a hash-like function
    float3 combined = float3(
        sin(dot(seed, float3(12.9898, 78.233, 45.164))) * 43758.5453,
        sin(dot(seed, float3(39.346, 11.135, 66.534))) * 43758.5453,
        sin(dot(seed, float3(78.233, 12.9898, 39.346))) * 43758.5453
    );

    // Combine the results to generate a final random value
    float randomValue = frac(combined.x + combined.y + combined.z);
    return randomValue; // Returns a float between 0 and 1
}


bool RayBoundingBox(Ray ray, float3 boxMin, float3 boxMax, out float tNear)
{
    float3 invDir = 1 / ray.dir;
    float3 tMin = (boxMin - ray.origin) * invDir;
    float3 tMax = (boxMax - ray.origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return tNear <= tFar;
}

HitInfo DDA(Ray ray, float startDist, out float3 tpos, out float side)
{
    HitInfo inf = (HitInfo) 0;
    // Main DDA loop
    
    float scalar = 2;
    
    float3 curpos = cameraPosition + ray.dir * startDist;
    
    tpos = floor(curpos);
    
    float3 step = float3(ray.dir.x > 0 ? 1 : -1, ray.dir.y > 0 ? 1 : -1, ray.dir.z > 0 ? 1 : -1);
    
    float3 deltaDist = float3(
        ray.dir.x == 0 ? 0.001f : abs(1.f / ray.dir.x),
        ray.dir.y == 0 ? 0.001f : abs(1.f / ray.dir.y),
        ray.dir.z == 0 ? 0.001f : abs(1.f / ray.dir.z)
    );
    float3 sideDist = float3(
        (step.x > 0 ? (tpos.x - curpos.x + 1) : (curpos.x - tpos.x)) * deltaDist.x,
        (step.y > 0 ? (tpos.y - curpos.y + 1) : (curpos.y - tpos.y)) * deltaDist.y,
        (step.z > 0 ? (tpos.z - curpos.z + 1) : (curpos.z - tpos.z)) * deltaDist.z
    );
    
    float shade = 1.f;
    bool vertSide = false;
    side = 0;
    
    float2 coord = float2(0, 0);

    for (int i = 0; i < ChunkSize*ChunkSize;i++)
    {
        float3 tile = floor(tpos);
        
        if ((step.x > 0 ? tile.x >= ChunkSize : tile.x < 0) || (step.z > 0 ? tile.z >= ChunkSize : tile.z < 0) || (step.y > 0 ? tile.y >= ChunkSize : tile.y < 0))
        {
            return inf;
        }
            
        float3 vox = floor(tex3Dlod(voxelsSampler, float4(tile / float3(ChunkSize, ChunkSize, ChunkSize), 0)) * 255);
        
        if (tile.y >= 0 && tile.y < ChunkSize && tile.x >= 0 && tile.x < ChunkSize && tile.z >= 0 && tile.z < ChunkSize && vox.r > 0)
        {
            float effect = vox.b;
            
            float voxShadeEffect = (vox.g/255.f);
            
            [branch]
            if (effect == 1)
            {
                //Water
                voxShadeEffect = lerp((vox.g / 255.f), ((vox.g / 255.f)-0.5f)*2, abs(sin(time * 0.6 + radians(((tile.x / ChunkSize)) * 180))) * 0.3f);
            }else
            if (effect == 2)
            {
                //Double wave
                voxShadeEffect = (vox.g / 255.f) - (abs(sin(time * 1 + radians(((tile.z / ChunkSize) + (tile.x / ChunkSize)) * 2 * 180) + 1) / 2) * (abs(sin(time * 0.01 + radians(((tile.z / ChunkSize) + (tile.x / ChunkSize)) * 2 * 180)) + 1) / 2) * 0.1f);
            }
            float2 pixelColor = (abs(coord) % 16.f)/16.f;
            float4 baseColorIndex = float4(floor((vox.r - 1) % 16) + pixelColor.x, floor((vox.r - 1) / 16) + pixelColor.y, 0, 0) / 16;
            
            float3 voxelColor = tex2Dlod(colorsSampler, baseColorIndex).rgb;
            
            inf.color += float3(voxShadeEffect * shade * voxelColor);
            
            inf.hit = true;
            return inf;
        }
        // Move to the next tile
        if (sideDist.x < sideDist.y)
        {
            if (sideDist.x < sideDist.z)
            {
                sideDist.x += deltaDist.x;
                tpos.x += step.x;
                shade = step.x < 0 ? 0.9 : 0.7;
                side = 2;
                coord = (float2(tpos.z, tpos.y));
            }
            else
            {
                sideDist.z += deltaDist.z;
                tpos.z += step.z;
                shade = step.z < 0 ? 0.5 : 0.4;
                side = 1;
                coord = (float2(tpos.x, tpos.y));
            }
        }
        else
        {
            if (sideDist.y < sideDist.z)
            {
                sideDist.y += deltaDist.y;
                tpos.y += step.y;
                shade = step.y<0?1:0.3;
                side = 0;
                coord = (float2(tpos.z, tpos.x));
            }
            else
            {
                sideDist.z += deltaDist.z;
                tpos.z += step.z;
                shade = step.z < 0 ? 0.5 : 0.4;
                side = 1;
                coord = (float2(tpos.x, tpos.y));
            }
        }
        
    }
    return inf;
}

float3 GetRayDirection(float2 uv)
{
    // NDC to View space
    float4 viewPos = mul(float4(uv, 1.0f, 1.0f), InvProjection);

    // Normalize ray direction in view space
    float3 rayDirViewSpace = normalize(viewPos.xyz);

    // Optionally transform to world space by multiplying with inverse view matrix
    float3 rayDirWorldSpace = mul(float4(rayDirViewSpace, 0.0f), cameraViewMatrix).xyz;

    return rayDirWorldSpace;
}

float3 GetRayDirectionOrthographic(float2 uv)
{
    // Calculate the ray origin in view space (on the near plane)
    float3 rayOriginViewSpace;
    rayOriginViewSpace.x = uv.x * (orthoWidth / 2.0f); // Scale NDC by ortho width
    rayOriginViewSpace.y = uv.y * (orthoHeight / 2.0f); // Scale NDC by ortho height
    rayOriginViewSpace.z = 0.0f; // On the near plane

    // Transform the origin from view space to world space
    float3 rayOriginWorldSpace = mul(float4(rayOriginViewSpace, 1.0f), cameraViewMatrix).xyz;

    // In orthographic projection, the ray direction is constant (e.g., down the -Z axis)
    float3 rayDirViewSpace = float3(0.0f, 0.0f, -1.0f);

    // Optionally transform the direction into world space
    float3 rayDirWorldSpace = mul(float4(rayDirViewSpace, 0.0f), cameraViewMatrix).xyz;

    return rayDirWorldSpace;
}
struct PSOut
{
    float4 Color0 : SV_Target0;
    float4 Color1 : SV_Target1;
    float Depth0 : DEPTH0;
};


PSOut MainPS(PSInput input)
{
    PSOut output = (PSOut) 0;
    
    float depth;
    float3 tileHit;
    // Calculate ray direction using screen space coordinates
    float2 uv = input.vPos.xy / dim;
    
    uv.y = 1 - uv.y;
    
    uv -= float2(0.5, 0.5);
    uv *= 2;
    
    float zComponent = 1.0 / tan(FOV / 2.0);
    
    // Initialize ray
    Ray ray;
    
    ray.dir = orthoCamera ? GetRayDirectionOrthographic(uv) : GetRayDirection(uv);
    ray.dir = normalize(ray.dir);
    
    ray.origin = cameraPosition;

    float side;
    // Perform ray-box intersection
    HitInfo info = DDA(ray, minSafeDistance, tileHit, side);
    
    RayBoundingBox(ray, tileHit, tileHit + float3(1,1,1), depth);
    
    // Calculate the exact intersection point
    float3 intersectionPoint = ray.origin + ray.dir * depth;
    depth = distance(ray.origin, intersectionPoint);
    
    clip(info.hit?0:-1);
    
    float fog = depth / (renderDistance*ChunkSize*0.5f);
        
    output.Color0 = float4(lerp(info.color, skyColor, pow(saturate(fog),2)), 1);
    output.Color1 = float4(depth, side, 0, 255);
    output.Depth0 = depth;
    
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