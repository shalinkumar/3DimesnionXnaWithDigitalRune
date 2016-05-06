//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file MaterialMorphSkinned.fx
/// Combines the material of a model (e.g. textures) with the light buffer data.
/// Supports:
/// - Diffuse color/texture
/// - Specular color/texture
/// - Morphing (up to 5 morph targets)
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define MORPHING 1
#define SKINNING 1
#include "Material.fx"
