//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file ForwardMorphSkinned.fx
/// Renders a model in a single pass ("forward rendering").
/// Supports:
/// - Morphing (up to 5 morph targets)
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define MORPHING 1
#define SKINNING 1
#include "Forward.fx"
