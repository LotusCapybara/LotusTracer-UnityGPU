﻿using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StackBVH4Node
    {
        // 32 bits of data
        // bit 1: is leaf
        // bit 2,3,4,5: are children navigable? 
        // they are not navigable if they are leaf with 0 triangleIndices
        
        public uint data;
        public int startIndex;
        public int qtyTriangles;

        public float3 boundsMin;
        public float3 extends;
        
        public uint2 xMins;
        public uint2 xMaxs;

        public uint2 yMins;
        public uint2 yMaxs;
        
        public uint2 zMins;
        public uint2 zMaxs;
    }
}