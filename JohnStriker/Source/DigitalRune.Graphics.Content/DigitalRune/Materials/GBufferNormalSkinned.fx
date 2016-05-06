//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file GBufferNormalSkinned.fx
/// Renders the model into the G-buffer.
/// Supports:
/// - Specular power
/// - Normal map
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define NORMAL_MAP 1
#define SKINNING 1
#include "GBuffer.fx"
