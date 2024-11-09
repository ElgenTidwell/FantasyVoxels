#define GroupSizeXY 16

struct Ray
{
    float3 origin;
    float3 dir;
};
struct HitInfo
{
    bool hit;
    float3 color;
    float3 pos;
};

RWTexture2D<float4> Texture;

float3 cameraPosition;
float4x4 cameraViewMatrix;

bool RayBoundingBox(Ray ray, float3 boxMin, float3 boxMax)
{
    float3 invDir = 1 / ray.dir;
    float3 tMin = (boxMin - ray.origin) * invDir;
    float3 tMax = (boxMax - ray.origin) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);
    return tNear <= tFar;
};

HitInfo DDA(Ray ray,float3 startpos, int maxDist )
{
    HitInfo inf = (HitInfo) 0;
    
    //Main DDA loop
    float3 curpos = startpos;
    float3 tpos = floor(curpos);
    int3 step = int3(ray.dir.x > 0 ? 1 : -1, ray.dir.y > 0 ? 1 : -1, ray.dir.z > 0 ? 1 : -1);
    
    const float3 deltaDist = float3(
        ray.dir.x == 0 ? 0.001f : abs(1.f / ray.dir.x),
        ray.dir.y == 0 ? 0.001f : abs(1.f / ray.dir.y),
        ray.dir.z == 0 ? 0.001f : abs(1.f / ray.dir.z)
    );
    
    float3 sideDist = (step > 0 ? (tpos - curpos + 1) : (curpos - tpos)) * deltaDist;
    
    int steps = 0;
    int distance = 0;
    while (steps < maxDist)
    {
        float3 tile = floor(curpos);
        float3 itile = abs(tile)%64;
        
        float stepErrorAllowance = steps>800?2:1;
            
        if (tile.y > -10 && tile.y < 110 && tile.x > -10 && tile.x < 110 && tile.z > 10 && tile.z < 130)
        {
            int c = (int) itile.x ^ (int) itile.y ^ (int) itile.z;
            
            inf.color = float3(c,c,c)/64;
            inf.hit = true;
            inf.pos = curpos;
            return inf;
        }
        steps++;

        // Move to the next tile in the direction of the smallest side distance
        if (sideDist.x < sideDist.y)
        {
            if (sideDist.x < sideDist.z)
            {
                sideDist.x += deltaDist.x * stepErrorAllowance;
                curpos.x += step.x * stepErrorAllowance;
            }
            else
            {
                sideDist.z += deltaDist.z * stepErrorAllowance;
                curpos.z += step.z * stepErrorAllowance;
            }
        }
        else
        {
            if (sideDist.y < sideDist.z)
            {
                sideDist.y += deltaDist.y * stepErrorAllowance;
                curpos.y += step.y * stepErrorAllowance;
            }
            else
            {
                sideDist.z += deltaDist.z * stepErrorAllowance;
                curpos.z += step.z * stepErrorAllowance;
            }
        }
    }
    return inf;
}

[numthreads(GroupSizeXY, GroupSizeXY, 1)]
void CS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint  localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
    float2 dim;
    Texture.GetDimensions(dim.x,dim.y);
    
    //Get UV coord. Used for Ray Direction
    float2 uv = globalID.xy/dim;
    
    uv.y = 1 - uv.y;
    
    uv -= float2(0.5, 0.5);
    uv.x *= dim.x / dim.y;
    
    Ray ray;
    ray.origin = float3(0,0,0.6);
    ray.dir = float3(uv.x, uv.y, 0) - ray.origin;
    
    ray.dir = mul(ray.dir, cameraViewMatrix);
    ray.dir = normalize(ray.dir);
    
    ray.origin = cameraPosition;
    
    float4 pixel = float4(0,0,0, 1);
    
    HitInfo info = DDA(ray, ray.origin+ray.dir*20, 1024);
    
    if (info.hit)
    {
        pixel = float4(info.color, 1);
    }
    
    Texture[globalID.xy] = pixel;
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 CS();
    }
}