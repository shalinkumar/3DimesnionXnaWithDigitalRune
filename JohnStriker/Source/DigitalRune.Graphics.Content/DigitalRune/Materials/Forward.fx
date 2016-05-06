//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file Forward.fx
/// Renders a model in a single pass ("forward rendering").
/// Supports:
/// - Diffuse color/texture
/// - Specular color/texture
/// - Specular power
/// - Alpha blending
//
// ----- Defines
// This effect can be configured using various defines (see section "Defines").
// Default settings: 1 ambient + 1 directional light (without shadow) + fog
//
// ----- Alpha transparency
// Alpha values can be specified in
// - Diffuse texture (alpha channel)
// - Effect parameter "Alpha"
// 
// Variant #1: "Alpha for transparent materials"
// When rendering transparent materials, such as glass, alpha should only be
// applied to the diffuse component.
//
// Variant #2: "Fade in/out using alpha"
// When using alpha to fade an object in/out, alpha needs to be applied to all
// components (diffuse, specular, emissive).
//
// The current implementation applies alpha to the entire object (Variant #2)!
//
// ----- Morphing (morph target animation)
// When morphing is enabled the binormal is excluded from the vertex attributes
// to stay within the max number of vertex attributes. The binormal is derived
// from normal and tangent in the vertex shader.
// References:
// - C. Beeson & K. Bjorke: Curtis Beeson: Skin in the "Dawn" Demo,
//   http://http.developer.nvidia.com/GPUGems/gpugems_ch03.html
// - C. Beeson, NVIDIA: Animation in the "Dawn" Demo,
//   http://http.developer.nvidia.com/GPUGems/gpugems_ch04.html
//
//-----------------------------------------------------------------------------

#include "../Common.fxh"
#include "../Lighting.fxh"
#include "../Material.fxh"
#include "../Fog.fxh"

#define CLAMP_TEXCOORDS_TO_SHADOW_MAP_BOUNDS 1
#include "../ShadowMap.fxh"

#if DEPTH_TEST || ENVIRONMENT_MAP
#include "../Encoding.fxh"
#endif

#if DEPTH_TEST
#include "../Deferred.fxh"
#endif


//--------------------------------------------------------
// Defines
//--------------------------------------------------------

// Possible defines
//#define DEPTH_TEST 1    // Perform manual depth test using G-buffer.
//#define TWO_SIDED 1
//#define PREMULTIPLIED_ALPHA 1   // Diffuse texture uses premultiplied alpha.
//#define EMISSIVE 1
//#define NORMAL_MAP 1
//#define MORPHING 1
//#define SKINNING 1
//#define AMBIENT_LIGHT_COUNT x
//#define DIRECTIONAL_LIGHT_COUNT x
//#define POINT_LIGHT_COUNT x
//#define SPOTLIGHT_COUNT x
//#define PROJECTOR_LIGHT_COUNT 1
//#define FOG 1
//#define ENVIRONMENT_MAP 1
//#define GAMMA_CORRECTION 1

// Use the following define to enable a texture for the first light of its kind:
//#define DIRECTIONAL_LIGHT_TEXTURE 1
//#define POINT_LIGHT_TEXTURE 1
//#define SPOTLIGHT_TEXTURE 1

// Use the following define to enable shadow maps for the first directional light:
//#define DIRECTIONAL_LIGHT_SHADOW 1

#ifndef AMBIENT_LIGHT_COUNT
#define AMBIENT_LIGHT_COUNT 1
#endif

#ifndef DIRECTIONAL_LIGHT_COUNT
#define DIRECTIONAL_LIGHT_COUNT 1
#endif

#ifndef POINT_LIGHT_COUNT
#define POINT_LIGHT_COUNT 0
#endif

#ifndef SPOTLIGHT_COUNT
#define SPOTLIGHT_COUNT 0
#endif

#ifndef PROJECTOR_LIGHT_COUNT
#define PROJECTOR_LIGHT_COUNT 0
#endif
#if PROJECTOR_LIGHT_COUNT > 1
#error "Forward.fxh supports max. 1 projector light."
#endif

#ifndef FOG
#define FOG 1
#endif


//--------------------------------------------------------
// Constants
//--------------------------------------------------------

float4x4 World : WORLD;
float4x4 View : VIEW;
float4x4 Projection : PROJECTION;
float3 CameraPosition : CAMERAPOSITION;

#if DEPTH_TEST
float2 ViewportSize : VIEWPORTSIZE;
DECLARE_UNIFORM_GBUFFER(GBuffer0, 0);
float CameraFar : CAMERAFAR;
#endif


//--------------------------------------------------------
// Material Parameters
//--------------------------------------------------------

float3 DiffuseColor : DIFFUSECOLOR;
float3 SpecularColor : SPECULARCOLOR;
float SpecularPower : SPECULARPOWER;
#if EMISSIVE
float3 EmissiveColor : EMISSIVECOLOR;
#endif
float Alpha : ALPHA = 1;
float BlendMode : BLENDMODE = 1;    // 0 = Additive alpha-blending, 1 = normal alpha-blending

DECLARE_UNIFORM_DIFFUSETEXTURE      // Diffuse (RGB) + Alpha (A)
DECLARE_UNIFORM_SPECULARTEXTURE     // Specular (RGB) + Emissive (A)
#if NORMAL_MAP
DECLARE_UNIFORM_NORMALTEXTURE
#endif

#if MORPHING
float MorphWeight0 : MORPHWEIGHT0;
float MorphWeight1 : MORPHWEIGHT1;
float MorphWeight2 : MORPHWEIGHT2;
float MorphWeight3 : MORPHWEIGHT3;
float MorphWeight4 : MORPHWEIGHT4;
#endif

#if SKINNING
float4x3 Bones[72] : BONES;
#endif


//--------------------------------------------------------
// Light Parameters
//--------------------------------------------------------

// ----- Ambient light
#if AMBIENT_LIGHT_COUNT > 0
float3 AmbientLight[AMBIENT_LIGHT_COUNT] : AMBIENTLIGHT;
float AmbientLightAttenuation[AMBIENT_LIGHT_COUNT] : AMBIENTLIGHTATTENUATION;
float3 AmbientLightUp[AMBIENT_LIGHT_COUNT] : AMBIENTLIGHTUP;
#endif

// ----- Directional lights
#if DIRECTIONAL_LIGHT_COUNT > 0
float3 DirectionalLightDiffuse[DIRECTIONAL_LIGHT_COUNT] : DIRECTIONALLIGHTDIFFUSE;
float3 DirectionalLightSpecular[DIRECTIONAL_LIGHT_COUNT] : DIRECTIONALLIGHTSPECULAR;
float3 DirectionalLightDirection[DIRECTIONAL_LIGHT_COUNT] : DIRECTIONALLIGHTDIRECTION;
#if DIRECTIONAL_LIGHT_TEXTURE
texture DirectionalLightTexture0 : DIRECTIONALLIGHTTEXTURE0;
sampler DirectionalLightTexture0Sampler = sampler_state
{
  Texture = (DirectionalLightTexture0);
  AddressU  = WRAP;
  AddressV  = WRAP;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  MipFilter = LINEAR;
};
float4x4 DirectionalLightTextureMatrix0 : DIRECTIONALLIGHTTEXTUREMATRIX0;
#endif
#if DIRECTIONAL_LIGHT_SHADOW
int DirectionalLightShadowNumberOfCascades : DIRECTIONALLIGHTSHADOWNUMBEROFCASCADES0;
float4 DirectionalLightShadowCascadeDistances : DIRECTIONALLIGHTSHADOWCASCADEDISTANCES0;
float4x4 DirectionalLightShadowViewProjections[4] : DIRECTIONALLIGHTSHADOWVIEWPROJECTIONS0;
float4 DirectionalLightShadowDepthBias : DIRECTIONALLIGHTSHADOWDEPTHBIAS0;
float4 DirectionalLightShadowNormalOffset : DIRECTIONALLIGHTSHADOWDNORMALOFFSET0;
float2 DirectionalLightShadowMapSize : DIRECTIONALLIGHTSHADOWMAPSIZE0;
float DirectionalLightShadowFilterRadius : DIRECTIONALLIGHTSHADOWFILTERRADIUS0;
float DirectionalLightShadowJitterResolution : DIRECTIONALLIGHTSHADOWJITTERRESOLUTION0;
float DirectionalLightShadowFadeOutRange : DIRECTIONALLIGHTSHADOWFADEOUTRANGE0;
float DirectionalLightShadowMaxDistance : DIRECTIONALLIGHTSHADOWMAXDISTANCE0;
float DirectionalLightShadowFog : DIRECTIONALLIGHTSHADOWFOG0;

DECLARE_UNIFORM_SHADOWMAP(DirectionalLightShadowMap, DIRECTIONALLIGHTSHADOWMAP0);
#endif
#endif

// ----- Point lights
#if POINT_LIGHT_COUNT > 0
float3 PointLightDiffuse[POINT_LIGHT_COUNT] : POINTLIGHTDIFFUSE;
float3 PointLightSpecular[POINT_LIGHT_COUNT] : POINTLIGHTSPECULAR;
float3 PointLightPosition[POINT_LIGHT_COUNT] : POINTLIGHTPOSITION;
float PointLightRange[POINT_LIGHT_COUNT] : POINTLIGHTRANGE;
float PointLightAttenuation[POINT_LIGHT_COUNT] : POINTLIGHTATTENUATION;
#if POINT_LIGHT_TEXTURE
DECLARE_UNIFORM_LIGHTTEXTURE(PointLightTexture0, POINT_LIGHT_TEXTURE0);
float4x4 PointLightTextureMatrix0 : POINTLIGHTTEXTUREMATRIX0;
#endif
#endif

// ----- Spotlights
#if SPOTLIGHT_COUNT > 0
float3 SpotlightDiffuse[SPOTLIGHT_COUNT] : SPOTLIGHTDIFFUSE;
float3 SpotlightSpecular[SPOTLIGHT_COUNT] : SPOTLIGHTSPECULAR;
float3 SpotlightPosition[SPOTLIGHT_COUNT] : SPOTLIGHTPOSITION;
float3 SpotlightDirection[SPOTLIGHT_COUNT] : SPOTLIGHTDIRECTION;
float SpotlightAttenuation[SPOTLIGHT_COUNT] : SPOTLIGHTATTENUATION;
float SpotlightRange[SPOTLIGHT_COUNT] : SPOTLIGHTRANGE;
float SpotlightFalloffAngle[SPOTLIGHT_COUNT] : SPOTLIGHTFALLOFFANGLE;
float SpotlightCutoffAngle[SPOTLIGHT_COUNT] : SPOTLIGHTCUTOFFANGLE;
#if SPOTLIGHT_TEXTURE
DECLARE_UNIFORM_LIGHTTEXTURE(SpotlightTexture0, SPOTLIGHT_TEXTURE0);
float4x4 SpotlightTextureMatrix0 : SPOTLIGHTTEXTUREMATRIX0;
#endif
#endif

// ----- Projector light
#if PROJECTOR_LIGHT_COUNT > 0
float3 ProjectorLightDiffuse : PROJECTORLIGHTDIFFUSE;
float3 ProjectorLightSpecular : PROJECTORLIGHTSPECULAR;
float3 ProjectorLightPosition : PROJECTORLIGHTPOSITION;
float ProjectorLightRange : PROJECTORLIGHTRANGE;
float ProjectorLightAttenuation : PROJECTORLIGHTATTENUATION;
DECLARE_UNIFORM_LIGHTTEXTURE(ProjectorLightTexture, PROJECTORLIGHTTEXTURE);
float4x4 ProjectorLightTextureMatrix : PROJECTORLIGHTTEXTUREMATRIX;
#endif

#if ENVIRONMENT_MAP
texture EnvironmentMap : ENVIRONMENTMAP;
samplerCUBE EnvironmentSampler = sampler_state
{
  Texture = <EnvironmentMap>;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  MipFilter = LINEAR;
  AddressU = CLAMP;
  AddressV = CLAMP;
};
float EnvironmentMapSize : ENVIRONMENTMAPSIZE;
float3 EnvironmentMapDiffuse : ENVIRONMENTMAPDIFFUSE;
float3 EnvironmentMapSpecular : ENVIRONMENTMAPSPECULAR;
float EnvironmentMapRgbmMax : ENVIRONMENTMAPRGBMMAX;
float4x4 EnvironmentMapMatrix : ENVIRONMENTMAPMATRIX;
#endif

//--------------------------------------------------------
// Fog
//--------------------------------------------------------

// Color of fog (RGBA). If alpha is 0, fog is disabled.
float4 FogColor : FOGCOLOR;

// Combined fog parameters.
float4 FogParameters : FOGPARAMETERS;  // (Start, End, Density, HeightFalloff)
#define FogStart FogParameters.x
#define FogEnd FogParameters.y
#define FogDensity FogParameters.z
#define FogHeightFalloff FogParameters.w


//-----------------------------------------------------------------------------
// Input, output
//-----------------------------------------------------------------------------

struct VSInput
{
  float4 Position : SV_POSITION;
  float2 TexCoord : TEXCOORD;
  float3 Normal : NORMAL;
#if NORMAL_MAP
  float3 Tangent : TANGENT;
#if !MORPHING   // Exclude binormal when morphing is enabled.
  float3 Binormal : BINORMAL;
#endif
#endif
#if MORPHING
  float3 MorphPosition0 : POSITION1;
  float3 MorphNormal0 : NORMAL1;
  float3 MorphPosition1 : POSITION2;
  float3 MorphNormal1 : NORMAL2;
  float3 MorphPosition2: POSITION3;
  float3 MorphNormal2: NORMAL3;
  float3 MorphPosition3 : POSITION4;
  float3 MorphNormal3: NORMAL4;
  float3 MorphPosition4 : POSITION5;
  float3 MorphNormal4 : NORMAL5;
#endif
#if SKINNING
  int4 BoneIndices : BLENDINDICES0;
  float4 BoneWeights : BLENDWEIGHT0;
#endif
};


struct VSOutput
{
  float2 TexCoord : TEXCOORD0;
  float4 PositionWorldAndFog : TEXCOORD1;  // W contains 1- Fog Intensity.
  float3 Normal : TEXCOORD2;
#if NORMAL_MAP
  float3 Tangent : TEXCOORD3;
  float3 Binormal : TEXCOORD4;
#endif
#if DEPTH_TEST
  float4 PositionProj : TEXCOORD5;
  float Depth : TEXCOORD6;
#endif
  float4 Position : SV_POSITION;
};


struct PSInput
{
  float2 TexCoord : TEXCOORD0;
  float4 PositionWorldAndFog : TEXCOORD1;  // W contains 1- Fog Intensity.
  float3 Normal : TEXCOORD2;
#if NORMAL_MAP
  float3 Tangent : TEXCOORD3;
  float3 Binormal : TEXCOORD4;
#endif
#if DEPTH_TEST
  float4 PositionProj : TEXCOORD5;
  float Depth : TEXCOORD6;
#endif
};


//-----------------------------------------------------------------------------
// Shaders
//-----------------------------------------------------------------------------

VSOutput VS(VSInput input, float4x4 world)
{
  float4 position = input.Position;
  float3 normal = input.Normal;
#if NORMAL_MAP
  float3 tangent = input.Tangent;
#if !MORPHING
  float3 binormal = input.Binormal;
#endif
#endif
  
#if MORPHING
  // ----- Apply morph targets.
  position.xyz += MorphWeight0 * input.MorphPosition0;
  position.xyz += MorphWeight1 * input.MorphPosition1;
  position.xyz += MorphWeight2 * input.MorphPosition2;
  position.xyz += MorphWeight3 * input.MorphPosition3;
  position.xyz += MorphWeight4 * input.MorphPosition4;
  
  normal += MorphWeight0 * input.MorphNormal0;
  normal += MorphWeight1 * input.MorphNormal1;
  normal += MorphWeight2 * input.MorphNormal2;
  normal += MorphWeight3 * input.MorphNormal3;
  normal += MorphWeight4 * input.MorphNormal4;
  normal = normalize(normal);
  
#if NORMAL_MAP
  // Orthonormalize the neutral tangent against the new normal. (Subtract the
  // collinear elements of the new normal from the neutral tangent and normalize.)
  tangent = tangent - dot(tangent, normal) * normal;
  //tangent = normalize(tangent); Tangent is normalized in pixel shader.
#endif
#endif
  
#if SKINNING
  // ----- Apply skinning matrix.
  float4x3 skinningMatrix = (float4x3)0;
  skinningMatrix += Bones[input.BoneIndices.x] * input.BoneWeights.x;
  skinningMatrix += Bones[input.BoneIndices.y] * input.BoneWeights.y;
  skinningMatrix += Bones[input.BoneIndices.z] * input.BoneWeights.z;
  skinningMatrix += Bones[input.BoneIndices.w] * input.BoneWeights.w;
  position.xyz = mul(position, skinningMatrix);
  normal = mul(normal, (float3x3)skinningMatrix);
#if NORMAL_MAP
  tangent = mul(tangent, (float3x3)skinningMatrix);
#if !MORPHING
  binormal = mul(binormal, (float3x3)skinningMatrix);
#endif
#endif
#endif
  
  // ----- Apply world, view, projection transformation.
  float4 positionWorld = mul(position, world);
  float4 positionView = mul(positionWorld, View);
  float4 positionProj = mul(positionView, Projection);
#if DEPTH_TEST
  float normalizedDepth = -positionView.z / CameraFar;
#endif
  float3 normalWorld = mul(normal, (float3x3)world);
#if NORMAL_MAP
  float3 tangentWorld = mul(tangent, (float3x3)world);
#if !MORPHING
  float3 binormalWorld = mul(binormal, (float3x3)world);
#else
  // Derive binormal from normal and tangent.
  float3 binormalWorld = cross(normalWorld, tangentWorld);
  //binormalWorld = normalize(binormalWorld); Binormal is normalized in pixel shader.
#endif
#endif
  
#if FOG
  // ----- Fog
  float fog = 0;
  if (FogColor.a > 0)
  {
    float3 cameraToPositionVector = positionWorld.xyz - CameraPosition;
    float cameraDistance = length(cameraToPositionVector);
    
    // Smoothstep distance fog
    float smoothRamp = ComputeSmoothFogIntensity(cameraDistance, FogStart, FogEnd);
    
    // Exponential height-based fog
    float opticalLength = GetOpticalLengthInHeightFog(cameraDistance, FogDensity, cameraToPositionVector, FogHeightFalloff);
    float exponentialFog = ComputeExponentialFogIntensity(opticalLength, 1);  // fogDensity is included in opticalLength!
    
    // We use this fog parameter to scale the alpha in the pixel shader. The correct
    // way would be to apply a fog color, but scaling the alpha looks ok and works
    // with non-uniform/view-dependent fog colors.
    fog = smoothRamp * exponentialFog * FogColor.a;
  }
#else
  float fog = 0;
#endif
  
  // ----- Output
  VSOutput output = (VSOutput)0;
  output.Position = positionProj;
  output.TexCoord = input.TexCoord;
  output.PositionWorldAndFog = float4(positionWorld.xyz, (1 - fog));
  output.Normal =  normalWorld;
#if NORMAL_MAP
  output.Tangent = tangentWorld;
  output.Binormal = binormalWorld;
#endif
#if DEPTH_TEST
  output.PositionProj = positionProj;
  output.Depth = normalizedDepth;
#endif
  return output;
}


VSOutput VSNoInstancing(VSInput input)
{
  return VS(input, World);
}


VSOutput VSInstancing(VSInput input,
                      float4 worldColumn0 : BLENDWEIGHT0,
                      float4 worldColumn1 : BLENDWEIGHT1,
                      float4 worldColumn2 : BLENDWEIGHT2)
{
  float4x4 worldTransposed =
  {
    worldColumn0,
    worldColumn1,
    worldColumn2,
    float4(0, 0, 0, 1)
  };
  float4x4 world = transpose(worldTransposed);
    
  return VS(input, world);
}


float4 PS(PSInput input) : COLOR
{
#if DEPTH_TEST
  // Get the screen space texture coordinate for this position.
  float2 texCoordScreen = ProjectionToScreen(input.PositionProj, ViewportSize);
  float4 gBuffer0Sample = tex2Dlod(GBuffer0Sampler, float4(texCoordScreen, 0, 0));
  clip(GetGBufferDepth(gBuffer0Sample) - input.Depth);
#endif
  
  // Diffuse map: diffuse color + alpha
  float4 diffuseMap = tex2D(DiffuseSampler, input.TexCoord);
  float alpha = diffuseMap.a;
  clip(abs(alpha * Alpha) - 0.001f);
  
#if PREMULTIPLIED_ALPHA
  // Diffuse color uses premultiplied alpha.
  // Undo premultiplication.
  diffuseMap.rgb /= diffuseMap.a;
#endif
  
  // Convert color from sRGB to linear space.
  float3 diffuse = FromGamma(diffuseMap.rgb);
  
  // Specular map: non-premultiplied specular color + emissive
  float4 specularMap = tex2D(SpecularSampler, input.TexCoord);
  float3 specular = FromGamma(specularMap.rgb);
#if EMISSIVE
  float emissive = specularMap.a;
#endif
  
  // Normalize tangent space vectors.
  float3 normal = normalize(input.Normal);
#if NORMAL_MAP
  float3 tangent = normalize(input.Tangent);
  float3 binormal = normalize(input.Binormal);
  
  // Normals maps are encoded using DXT5nm.
  float3 normalMapSample = GetNormalDxt5nm(NormalSampler, input.TexCoord); // optional: multiply with "Bumpiness" factor.
  normal = normal * normalMapSample.z + tangent * normalMapSample.x - binormal * normalMapSample.y;
#endif
  
  float3 position = input.PositionWorldAndFog.xyz;
  float3 cameraToPositionVector = position - CameraPosition;
  float cameraDistance = length(cameraToPositionVector);
  float3 viewDirection = cameraToPositionVector / cameraDistance;
  
  float3 diffuseLightAccumulated = 0;
  float3 specularLightAccumulated = 0;
  
  int i;
  // ----- Ambient light
#if AMBIENT_LIGHT_COUNT > 0
  [unroll]
  for (i = 0; i < AMBIENT_LIGHT_COUNT; i++)
    diffuseLightAccumulated += ComputeAmbientLight(AmbientLight[i], AmbientLightAttenuation[i], AmbientLightUp[i], normal);
#endif
  
  // ----- Directional lights
#if DIRECTIONAL_LIGHT_COUNT > 0
  [unroll]
  for (i = 0; i < DIRECTIONAL_LIGHT_COUNT; i++)
  {
    float3 lightDiffuse, lightSpecular;
    ComputeDirectionalLight(DirectionalLightDiffuse[i],
                            DirectionalLightSpecular[i],
                            DirectionalLightDirection[i],
                            viewDirection,
                            normal,
                            SpecularPower,
                            lightDiffuse,
                            lightSpecular);
    
    // Optional texture for the first directional light.
#if DIRECTIONAL_LIGHT_TEXTURE
    if (i == 0)
    {
      float4 lightTexCoord = mul(float4(position, 1), DirectionalLightTextureMatrix0);
      float3 textureColor = FromGamma(tex2Dproj(DirectionalLightTexture0Sampler, lightTexCoord));
      
      lightDiffuse *= textureColor;
      lightSpecular *= textureColor;
    }
#endif
#if DIRECTIONAL_LIGHT_SHADOW
    if (i == 0)
    {
      // Compute the shadow cascade index and the texture coords.
      int cascade;
      float3 shadowTexCoord;
      float planarCameraDistance = -dot(cameraToPositionVector, View._m02_m12_m22); // dot with "forward"
      ComputeCsmCascadeFast(float4(position, 1), planarCameraDistance, DirectionalLightShadowCascadeDistances, DirectionalLightShadowViewProjections, cascade, shadowTexCoord);
      
      float shadow = DirectionalLightShadowFog;
      if (IsInRange(shadowTexCoord, 0, 1))
      {
        // Apply normal offset (using geometry normal, not perturbed normal).
        float3 offsetPosition = ApplyNormalOffset(position, normal, DirectionalLightDirection[0], DirectionalLightShadowNormalOffset[cascade]);
        shadowTexCoord.xy = GetShadowTexCoord(float4(offsetPosition, 1), DirectionalLightShadowViewProjections[cascade]).xy;
        
        // Transform the texture coords to valid texture atlas coords.
        float2 atlasTexCoord = ConvertToTextureAtlas(shadowTexCoord.xy, cascade, DirectionalLightShadowNumberOfCascades);
        
        // Shadow map bounds (left, top, right, bottom) inside texture atlas.
        float4 shadowMapBounds = GetShadowMapBounds(cascade, DirectionalLightShadowNumberOfCascades, DirectionalLightShadowMapSize);
        
        // Since this shadow uses an orthographic projection, the pixel depth
        // is the z value of the shadow projection space.
        // Apply bias against surface acne.
        float ourDepth = shadowTexCoord.z + DirectionalLightShadowDepthBias[cascade];
        
        // Compute the shadow factor (0 = no shadow, 1 = shadow).
        shadow = GetShadow(ourDepth, atlasTexCoord, DirectionalLightShadowMapSampler, DirectionalLightShadowMapSize, shadowMapBounds);
        
        // Fade out the shadow in the distance.
        shadow = ApplyShadowFog(
          cascade >= DirectionalLightShadowNumberOfCascades - 1,
          shadow,
          shadowTexCoord,
          cameraDistance,
          DirectionalLightShadowMaxDistance,
          DirectionalLightShadowFadeOutRange,
          DirectionalLightShadowFog);
      }
      
      // The shadow mask stores the inverse.
      float shadowTerm = 1 - shadow;
      lightDiffuse *= shadowTerm.xxx;
      lightSpecular *= shadowTerm.xxx;
    }
#endif
    
    diffuseLightAccumulated += lightDiffuse;
    specularLightAccumulated += lightSpecular;
  }
#endif
  
  // ----- Point lights
#if POINT_LIGHT_COUNT > 0
  [unroll]
  for (i = 0; i < POINT_LIGHT_COUNT; i++)
  {
    float3 lightDirection = position - PointLightPosition[i];
    float lightDistance = length(lightDirection);
    lightDirection = lightDirection / lightDistance;
    
    float3 lightDiffuse, lightSpecular;
    ComputePointLight(PointLightDiffuse[i],
                      PointLightSpecular[i],
                      PointLightRange[i],
                      PointLightAttenuation[i],
                      lightDirection,
                      lightDistance,
                      viewDirection,
                      normal,
                      SpecularPower,
                      lightDiffuse,
                      lightSpecular);
    
    // Optional texture for the first directional light.
#if POINT_LIGHT_TEXTURE
    if (i == 0)
    {
      float3 textureColor = texCUBE(PointLightTexture0Sampler, mul(lightDirection, PointLightTextureMatrix0));
      textureColor = FromGamma(textureColor);
      
      lightDiffuse *= textureColor;
      lightSpecular *= textureColor;
    }
#endif
    
    diffuseLightAccumulated += lightDiffuse;
    specularLightAccumulated += lightSpecular;
  }
#endif
  
  // ----- Spotlights
#if SPOTLIGHT_COUNT > 0
  [unroll]
  for (i = 0; i < SPOTLIGHT_COUNT; i++)
  {
    float3 lightDirection = position - SpotlightPosition[i];
    float lightDistance = length(lightDirection);
    lightDirection = lightDirection / lightDistance;
    
    float3 lightDiffuse, lightSpecular;
    ComputeSpotlight(SpotlightDiffuse[i], SpotlightSpecular[i],
                     SpotlightRange[i], SpotlightAttenuation[i],
                     SpotlightFalloffAngle[i], SpotlightCutoffAngle[i],
                     SpotlightDirection[i],
                     lightDirection, lightDistance,
                     viewDirection, normal, SpecularPower,
                     lightDiffuse, lightSpecular);
    
    // Optional texture for the first directional light.
#if SPOTLIGHT_TEXTURE
    if (i == 0)
    {
      float4 lightTexCoord = mul(float4(position, 1), SpotlightTextureMatrix0);
      float3 textureColor = FromGamma(tex2Dproj(SpotlightTexture0Sampler, lightTexCoord));
      
      lightDiffuse *= textureColor;
      lightSpecular *= textureColor;
    }
#endif
    
    diffuseLightAccumulated += lightDiffuse;
    specularLightAccumulated += lightSpecular;
  }
#endif
  
  
  // ----- Projector light
#if PROJECTOR_LIGHT_COUNT > 0
  {
    float3 lightDirection = position - ProjectorLightPosition;
    float lightDistance = length(lightDirection);
    lightDirection = lightDirection / lightDistance;
    
    float3 lightDiffuse, lightSpecular;
    ComputeProjectorLight(ProjectorLightDiffuse, ProjectorLightSpecular,
                          ProjectorLightTextureSampler, ProjectorLightTextureMatrix,
                          ProjectorLightRange, ProjectorLightAttenuation,
                          lightDirection, lightDistance,
                          viewDirection, position, normal,
                          SpecularPower,
                          lightDiffuse, lightSpecular);
    
    diffuseLightAccumulated += lightDiffuse;
    specularLightAccumulated += lightSpecular;
  }
#endif
  
  // ----- Light Probe (Environment Map)
#if ENVIRONMENT_MAP
  // Diffuse:
  // Currently we use a hardcoded mipmap level for the diffuse textue lookup.
  // In DirectX 11 we could compute the mipmap level and choose the 1x1 or 2x2 mip level.
  const float maxMipLevel = 8;
  
  float4 environmentMapDiffuse = texCUBElod(
    EnvironmentSampler, 
    float4(mul(normal, (float3x3)EnvironmentMapMatrix), maxMipLevel));
  
  diffuseLightAccumulated += EnvironmentMapDiffuse * FromGamma(DecodeRgbm(environmentMapDiffuse, EnvironmentMapRgbmMax));
  
  // Specular:
  // Select mipmap level for specular reflections based on surface roughness (SpecularPower).
  // Note: The term log2(size * sqrt(3)) can be precomputed.
  float mipLevel = max(0, log2(EnvironmentMapSize * sqrt(3)) - 0.5 * log2(SpecularPower + 1));
  
  float3 viewDirectionReflected = reflect(viewDirection, normal);
  
  float4 environmentMapSpecular = texCUBElod(
    EnvironmentSampler, 
    float4(mul(viewDirectionReflected, (float3x3)EnvironmentMapMatrix), mipLevel));
  
  specularLightAccumulated += EnvironmentMapSpecular * FromGamma(DecodeRgbm(environmentMapSpecular, EnvironmentMapRgbmMax));
#endif
  
  // Combine material colors with lights.
  
  // Diffuse
  float3 result = DiffuseColor * diffuse * diffuseLightAccumulated;
  
  // Specular
  result += SpecularColor * specular * specularLightAccumulated;
  
#if EMISSIVE
  // Emissive
  result += EmissiveColor * diffuse * emissive;
#endif
  
  // Fog needs to be applied to the alpha value.
  float fog = input.PositionWorldAndFog.w;
  alpha = abs(alpha * Alpha) * fog;
  
#if GAMMA_CORRECTION
  result = ToGamma(result);
#endif
  
  // Premultiply alpha.
  result *= alpha;
  
  return float4(result, alpha * BlendMode);
}


//-----------------------------------------------------------------------------
// Techniques
//-----------------------------------------------------------------------------

#if !SM4
#define VSTARGET vs_3_0
#define PSTARGET ps_3_0
#else
#define VSTARGET vs_4_0
#define PSTARGET ps_4_0
#endif


#if !SM4 && !SKINNING && !MORPHING && !POINT_LIGHT_COUNT && !SPOTLIGHT_COUNT && !PROJECTOR_LIGHT_COUNT && !ENVIRONMENT_MAP && !MGFX 
#define SUPPORTS_INSTANCING 1
#endif

technique Default
#if SUPPORTS_INSTANCING
< string InstancingTechnique = "DefaultInstancing"; >
#endif

{
#if TWO_SIDED
  pass BackSides
  {
    CullMode = CW;
    VertexShader = compile VSTARGET VSNoInstancing();
    PixelShader = compile PSTARGET PS();
  }
#endif
  
  pass FrontSides
  {
    CullMode = CCW;
    VertexShader = compile VSTARGET VSNoInstancing();
    PixelShader = compile PSTARGET PS();
  }
}


#if SUPPORTS_INSTANCING
technique DefaultInstancing
{
#if TWO_SIDED
  pass BackSides
  {
    CullMode = CW;
    VertexShader = compile VSTARGET VSInstancing();
    PixelShader = compile PSTARGET PS();
  }
#endif
  
  pass FrontSides
  {
    CullMode = CCW;
    VertexShader = compile VSTARGET VSInstancing();
    PixelShader = compile PSTARGET PS();
  }
}
#endif
