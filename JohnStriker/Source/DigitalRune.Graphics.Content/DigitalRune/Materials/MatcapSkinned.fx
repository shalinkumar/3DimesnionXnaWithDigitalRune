//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file MatcapSkinned.fx
/// Renders an object by sampling a surface material ("material capture") using
/// the normal vector.
/// Supports:
/// - Matcap texture
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define SKINNING 1
#include "Matcap.fx"
