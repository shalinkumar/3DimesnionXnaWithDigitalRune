//-----------------------------------------------------------------------------
// DigitalRune Graphics - http://www.digitalrune.com/
// Copyright (C) DigitalRune GmbH. All rights reserved.
//-----------------------------------------------------------------------------
//
/// \file MatcapNormalSkinned.fx
/// Renders an object by sampling a surface material ("material capture") using
/// the normal vector.
/// Supports:
/// - Matcap texture
/// - Normal map
/// - Mesh skinning (up to 72 bones)
//
//-----------------------------------------------------------------------------

#define NORMAL_MAP 1
#define SKINNING 1
#include "Matcap.fx"
