using System.Runtime.InteropServices;

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
        public BoundsBox bounds0;
        public BoundsBox bounds1;
        public BoundsBox bounds2;
        public BoundsBox bounds3;
        public BoundsBox bounds4;
        public BoundsBox bounds5;
        public BoundsBox bounds6;
        public BoundsBox bounds7;
    }
}