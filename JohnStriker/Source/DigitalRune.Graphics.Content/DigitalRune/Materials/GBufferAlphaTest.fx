//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file GBufferAlphaTest.fx
/// Renders the model into the G-buffer.
/// Supports:
/// - Specular power
/// - Alpha test (mask stored in alpha channel of diffuse texture)
//
//-----------------------------------------------------------------------------

#define ALPHA_TEST 1
#include "GBuffer.fx"
