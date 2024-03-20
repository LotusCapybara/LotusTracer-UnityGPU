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
        public float3 extends;

        public uint4 bb0;
        public uint4 bb1;
        public uint4 bb2;
        public uint4 bb3;
        public uint4 bb4;
        public uint4 bb5;
        public uint4 bb6;
        public uint4 bb7;
      
    }
}