float4x4 WorldView : WORLDVIEW;
float4x4 Projection : PROJECTION;
float CameraFar : CAMERAFAR;
float2 ViewportSize : VIEWPORTSIZE;

float3 DiffuseColor : DIFFUSECOLOR;
float3 SpecularColor : SPECULARCOLOR;
float Exposure;
texture EmissiveTexture : EMISSIVETEXTURE;
sampler EmissiveSampler = sampler_state
{
	Texture = <EmissiveTexture>;
	MAGFILTER = LINEAR;
	MINFILTER = LINEAR;
	MIPFILTER = LINEAR;
	AddressU = WRAP;
	AddressV = WRAP;
};

// Light buffer 0 stores the diffuse lighting.
texture LightBuffer0 : LIGHTBUFFER0;
sampler LightBuffer0Sampler = sampler_state
{
  Texture = <LightBuffer0>;
  AddressU  = CLAMP;
  AddressV  = CLAMP;
  MipFilter = POINT;
  MinFilter = POINT;
  MagFilter = POINT;  
};

// Light buffer 1 stores the specular lighting.
texture LightBuffer1 : LIGHTBUFFER1;
sampler LightBuffer1Sampler = sampler_state
{
  Texture = <LightBuffer1>;
  AddressU  = CLAMP;
  AddressV  = CLAMP;
  MipFilter = POINT;
  MinFilter = POINT;
  MagFilter = POINT;  
};


//-----------------------------------------------------------------------------
// Functions
//-----------------------------------------------------------------------------

void VS(inout float2 texCoord : TEXCOORD0,
        out float4 positionProj : TEXCOORD1,
        inout float4 position : SV_POSITION)
{ 
  float4 positionView = mul(position, WorldView);
  position = mul(positionView, Projection);
  positionProj = position;
}


float4 PS(float2 texCoord	: TEXCOORD0,
          float4 positionProj : TEXCOORD1) : COLOR0
{
  float3 emissiveMap = tex2D(EmissiveSampler, texCoord).rgb;
  
  // Convert from Gamma to linear space.
  emissiveMap = emissiveMap * emissiveMap;
  
  // Get the screen space texture coordinate for this position.
  float2 texCoordScreen = positionProj.xy / positionProj.w;
  texCoordScreen.xy = (float2(texCoordScreen.x, -texCoordScreen.y) + 1) * 0.5f;
  texCoordScreen.xy += 0.5f / ViewportSize;
  
  float3 diffuseLight = tex2D(LightBuffer0Sampler, texCoordScreen).rgb;
  float3 specularLight = tex2D(LightBuffer1Sampler, texCoordScreen).rgb;
  
  return float4(DiffuseColor * diffuseLight + SpecularColor * specularLight.rgb + emissiveMap * Exposure, 1);
}


//-----------------------------------------------------------------------------
// Techniques
//-----------------------------------------------------------------------------


#if !SM4
  #define VSTARGET vs_2_0
  #define PSTARGET ps_2_0
#else
  #define VSTARGET vs_4_0_level_9_1
  #define PSTARGET ps_4_0_level_9_1
#endif    

technique 
{
  pass 
  {
    VertexShader = compile VSTARGET VS();
    PixelShader = compile PSTARGET PS();
  }
}
