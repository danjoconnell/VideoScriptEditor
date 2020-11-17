// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// add headers that you want to pre-compile here

#include "UnmanagedLibImports.h"

// A simple macro emulating the C# nameof expression. (See https://stackoverflow.com/questions/10513604/how-can-one-get-a-property-name-as-a-string-in-managed-c/10691144#10691144).
#define NAMEOF(named_entity) #named_entity

#include "Internal.h"
#include "PreviewRendererException.h"

#endif //PCH_H
