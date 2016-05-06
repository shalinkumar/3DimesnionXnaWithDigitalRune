//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file ShadowMapAlphaTestSkinned.fx
/// Renders the model into the shadow map.
/// Supports:
/// - Alpha test (mask stored in alpha channel of diffuse texture)
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define ALPHA_TEST 1
#define SKINNING 1
#include "ShadowMap.fx"
