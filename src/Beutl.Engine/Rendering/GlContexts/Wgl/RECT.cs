﻿using System.Runtime.InteropServices;

namespace Beutl.Rendering.GlContexts;

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}
