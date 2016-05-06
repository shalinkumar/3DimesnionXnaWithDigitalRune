//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file ShadowMapMorphSkinned.fx
/// Renders the model into the shadow map.
/// Supports:
/// - Morphing (up to 5 morph targets)
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define MORPHING 1
#define SKINNING 1
#include "ShadowMap.fx"
