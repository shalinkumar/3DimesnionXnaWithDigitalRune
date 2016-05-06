//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file ShadowMapAlphaTest.fx
/// Renders the model into the shadow map.
/// Supports:
/// - Alpha test (mask stored in alpha channel of diffuse texture)
//
//-----------------------------------------------------------------------------

#define ALPHA_TEST 1
#include "ShadowMap.fx"
