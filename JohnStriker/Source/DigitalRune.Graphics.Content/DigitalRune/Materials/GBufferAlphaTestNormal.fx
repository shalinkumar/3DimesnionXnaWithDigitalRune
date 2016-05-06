//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file GBufferAlphaTestNormal.fx
/// Renders the model into the G-buffer.
/// Supports:
/// - Specular power
/// - Alpha test (mask stored in alpha channel of diffuse texture)
/// - Normal map
//
//-----------------------------------------------------------------------------

#define ALPHA_TEST 1
#define NORMAL_MAP 1
#include "GBuffer.fx"
