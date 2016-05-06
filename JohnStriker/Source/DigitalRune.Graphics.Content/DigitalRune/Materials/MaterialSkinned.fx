//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file MaterialSkinned.fx
/// Combines the material of a model (e.g. textures) with the light buffer data.
/// Supports:
/// - Diffuse color/texture
/// - Specular color/texture
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define SKINNING 1
#include "Material.fx"
