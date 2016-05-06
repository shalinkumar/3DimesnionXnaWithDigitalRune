//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file ForwardTwoSided.fx
/// Renders a model in a single pass ("forward rendering").
/// Supports:
/// - Diffuse color/texture
/// - Specular color/texture
/// - Specular power
/// - Environment map
/// - Alpha blending
//
//-----------------------------------------------------------------------------

#define PREMULTIPLIED_ALPHA 1   // Diffuse texture uses premultiplied alpha.
#define ENVIRONMENT_MAP 1
#include "Forward.fx"
