//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file MaterialEmissiveSkinned.fx
/// Combines the material of a model (e.g. textures) with the light buffer data.
/// Supports:
/// - Diffuse color/texture
/// - Specular color/texture
/// - Emissive parts (mask stored in alpha channel of specular texture)
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define EMISSIVE 1
#define SKINNING 1
#include "Material.fx"
