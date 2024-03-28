using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StackBVH4Node
    {
        // 32 bits of data
        // bit 0: is leaf
        // bit 1-8: are children navigable? 
        // they are not navigable if they are leaf with 0 triangleIndices
        // bits 9-12: bits for integer [0, 15] = amount of children (either tris or nodes)
        
        // bits 13-20: are children leaves? 
        
        
        public uint data;
        
        // leaf: triangle  inner: inner nodes
        public int firstElementIndex;

        public float precisionLoss;
        public float3 boundsMin;
        public float3 extends;
        
        public uint2 bb0;
        public uint2 bb1;
        public uint2 bb2;
        public uint2 bb3;
        public uint2 bb4;
        public uint2 bb5;
        public uint2 bb6;
        public uint2 bb7;
    }
}