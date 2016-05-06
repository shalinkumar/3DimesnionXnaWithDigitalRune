//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file GBufferAlphaTestEmissive.fx
/// Combines the material of a model (e.g. textures) with the light buffer data.
/// Supports:
/// - Diffuse color/texture
/// - Specular color/texture
/// - Emissive parts (mask stored in alpha channel of specular texture)
/// - Alpha test (mask stored in alpha channel of diffuse texture)
//
//-----------------------------------------------------------------------------

#define EMISSIVE 1
#define ALPHA_TEST 1
#include "Material.fx"
