// Renders the velocity into a velocity buffer.
//
// The velocity buffer contains the position change of each pixel:
// current position - position of last frame (using texture space coordinates).
//

float4x4 WorldViewProjection;
float4x4 LastWorldViewProjection;


void VS(out   float4 positionProj     : TEXCOORD0,
        out   float4 lastPositionProj : TEXCOORD1,
        inout float4 position         : SV_POSITION)
{
  positionProj = mul(position, WorldViewProjection); 
  lastPositionProj = mul(position, LastWorldViewProjection);
  position = positionProj;
}


float4 PS(float4 positionProj     : TEXCOORD0,
          float4 lastPositionProj : TEXCOORD1) : COLOR0
{
  // Homogenous divide:
  positionProj /= positionProj.w;
  lastPositionProj /= lastPositionProj.w;

  // Position change relative to the camera clip space.
  float2 delta = positionProj.xy - lastPositionProj.xy;
  
  // Convert from clip space to texture space:
  // * 0.5 because clip space is in the range [-1, 1] and texture space in the range [0, 1].
  delta *= 0.5f;
  // Clip space y must be inverted for textures space.
  delta.y *= -1;                                   

  return float4(delta.xy, 0, 1);
}


technique 
{
  pass 
  {
#if !SM4
    VertexShader = compile vs_2_0 VS();
    PixelShader = compile ps_2_0 PS();
#else
    VertexShader = compile vs_4_0_level_9_1 VS();
    PixelShader = compile ps_4_0_level_9_1 PS();
#endif
  }
}
