#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

float4x4 view_projection;
float4x4 uv_transform;

float2 screenAspect;
float2 screenSize;

int texIndex;

sampler TextureSampler : register(s0);

struct VertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};
struct PixelInput
{
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

PixelInput SpriteVertexShader(VertexInput v)
{
    PixelInput output;

    output.Position = mul(v.Position, view_projection);
    output.Color = v.Color;
    output.TexCoord = v.TexCoord / (screenSize * (screenAspect / (16*200)));
    return output;
}
float4 SpritePixelShader(PixelInput p) : SV_TARGET
{
    const float tIndexSize = (16 / 256.f);
    float2 coord = p.TexCoord.xy;
    coord %= tIndexSize;
    coord += float2(tIndexSize * (texIndex % 16), tIndexSize*floor(texIndex/16));
    
    float4 diffuse = tex2D(TextureSampler, coord);

    // if (p.TexCoord.x < 0 || p.TexCoord.x > 1) {
    //     discard;
    // }

    // if (p.TexCoord.y < 0 || p.TexCoord.y > 1) {
    //     discard;
    // }

    return diffuse * p.Color;
}

technique SpriteBatch
{
    pass
    {
        VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
        PixelShader = compile PS_SHADERMODEL SpritePixelShader();
    }
}