//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file GBufferMorphNormalSkinned.fx
/// Renders the model into the G-buffer.
/// Supports:
/// - Specular power
/// - Normal map
/// - Morphing (up to 5 morph targets)
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define NORMAL_MAP 1
#define MORPHING 1
#define SKINNING 1
#include "GBuffer.fx"
