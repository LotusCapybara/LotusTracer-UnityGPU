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
        
       public uint4 bb01;
       public uint4 bb23;
       public uint4 bb45;
       public uint4 bb67;        
    }
}