#define GroupSize 64

struct Particle
{
    float2 pos;
    float2 vel;
};

StructuredBuffer<int> voxels;

[numthreads(GroupSize, GroupSize, GroupSize)]
void CS(uint3 localID : SV_GroupThreadID, uint3 groupID : SV_GroupID,
        uint localIndex : SV_GroupIndex, uint3 globalID : SV_DispatchThreadID)
{
}

technique Tech0
{
    pass Pass0
    {
        ComputeShader = compile cs_5_0 CS();
    }
}