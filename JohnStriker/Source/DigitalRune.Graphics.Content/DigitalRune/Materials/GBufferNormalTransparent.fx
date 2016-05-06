//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file GBufferNormalTransparent.fx
/// Renders the model into the G-buffer.
/// Supports:
/// - Specular power
/// - Normal map
/// - Screen-door transparency
// 
//-----------------------------------------------------------------------------

#define TRANSPARENT 1
#define NORMAL_MAP 1
#include "GBuffer.fx"
