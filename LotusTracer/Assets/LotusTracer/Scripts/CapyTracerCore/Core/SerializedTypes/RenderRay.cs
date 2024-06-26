﻿using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderRay
    {
        int pixelIndex;
        public float3 origin;
        public float3 direction;
    }
}