// textures
texture VideoMemory;
texture Palette;
int Mode4Base, Mode5Base;

sampler VideoMemorySampler = sampler_state
{
    Texture   = (VideoMemory);
    MipFilter = NONE;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU  = CLAMP;        
    AddressV  = CLAMP;
};

sampler PaletteSampler = sampler_state
{
    Texture   = (Palette);
    MipFilter = NONE;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    AddressU  = CLAMP;        
    AddressV  = CLAMP;
};

float4 Mode0Renderer(
    float4 Diff : COLOR0,
    float2 Tex  : TEXCOORD0) : COLOR
{
    int x = Tex[0];
    int y = Tex[1];
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

float4 Mode1Renderer(
    float4 Diff : COLOR0,
    float2 Tex  : TEXCOORD0) : COLOR
{
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

float4 Mode2Renderer(
    float4 Diff : COLOR0,
    float2 Tex  : TEXCOORD0) : COLOR
{
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

float4 Mode3Renderer(
    float4 Diff : COLOR0,
    float2 Tex  : TEXCOORD0) : COLOR
{
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}

float4 Mode4Renderer(
    float4 Diff : COLOR0,
    float2 Tex  : TEXCOORD0) : COLOR
{
    int x = Tex[0];
    int y = Tex[1];
    int baseidx = Mode4Base + (y * 240) + x;
    float tx = baseidx % 512;
    float ty = baseidx / 512;
    float4 vref = tex2D(VideoMemorySampler, float2((tx + 0.5f) / 512.0f, (ty + 0.5f) / 256.0));
    return tex1D(PaletteSampler, vref[3]);
}

float4 Mode5Renderer(
    float4 Diff : COLOR0,
    float2 Tex  : TEXCOORD0) : COLOR
{
    int x = Tex[0];
    int y = Tex[1];
    if (x >= 0 && x < 160 && y >= 0 && y < 128) {
        int baseidx = Mode5Base + ((y * 160) + x) * 2;
        float tx = baseidx % 512;
        float ty = baseidx / 512;
        float4 vref = tex2D(VideoMemorySampler, float2((tx + 0.5f) / 512.0f, (ty + 0.5f) / 256.0f));
        float4 vref2 = tex2D(VideoMemorySampler, float2((tx + 1.5f) / 512.0f, (ty + 0.5f) / 256.0f));
        int i1 = (int)((vref[3] * 255.0f) + 0.5f);
        int i2 = (int)((vref2[3] * 255.0f) + 0.5f);
        int r = i1 % 0x20;
        int g = ((i2 % 0x4) * 8) + (i1 / 0x20);
        int b = i2 / 4;
        return float4(r / 32.0f, g / 32.0f, b / 32.0f, 1.0f);
    } else {
        return float4(0,0,0,1.0f);
    }
}

technique Mode0Renderer
{
    pass P0
    {
        CullMode = none; AlphaBlendEnable = false; ZEnable = false;
        PixelShader  = compile ps_3_0 Mode0Renderer();
    }
}
technique Mode1Renderer
{
    pass P0
    {
        CullMode = none; AlphaBlendEnable = false; ZEnable = false;
        PixelShader  = compile ps_3_0 Mode1Renderer();
    }
}
technique Mode2Renderer
{
    pass P0
    {
        CullMode = none; AlphaBlendEnable = false; ZEnable = false;
        PixelShader  = compile ps_3_0 Mode2Renderer();
    }
}
technique Mode3Renderer
{
    pass P0
    {
        CullMode = none; AlphaBlendEnable = false; ZEnable = false;
        PixelShader  = compile ps_3_0 Mode3Renderer();
    }
}
technique Mode4Renderer
{
    pass P0
    {
        CullMode = none; AlphaBlendEnable = false; ZEnable = false;
        PixelShader  = compile ps_3_0 Mode4Renderer();
    }
}
technique Mode5Renderer
{
    pass P0
    {
        CullMode = none; AlphaBlendEnable = false; ZEnable = false;
        PixelShader  = compile ps_3_0 Mode5Renderer();
    }
}