using System.Runtime.InteropServices;
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
        
        // bounds of all the children
        public uint3 bounds1;
        public uint3 bounds2;
        public uint3 bounds3;
        public uint3 bounds4;
        public uint3 bounds5;
        public uint3 bounds6;
        public uint3 bounds7;
        public uint3 bounds8;
    }
}