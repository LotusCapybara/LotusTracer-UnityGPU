using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public static class BVHUtils
{
    private static uint Part1by2(uint n)
    {
        n &= 0x000003ff;
        n = (n ^ (n << 16)) & 0xff0000ff;
        n = (n ^ (n << 8)) & 0x0300f00f;
        n = (n ^ (n << 4)) & 0x030c30c3;
        n = (n ^ (n << 2)) & 0x09249249;
        return n;
    }


    private static uint Unpart1by2(uint n)
    {
        n &= 0x09249249;
        n = (n ^ (n >> 2)) & 0x030c30c3;
        n = (n ^ (n >> 4)) & 0x0300f00f;
        n = (n ^ (n >> 8)) & 0xff0000ff;
        n = (n ^ (n >> 16)) & 0x000003ff;
        return n;
    }


    private static uint MortonEncode3(uint3 coords)
    {
        return Part1by2(coords.x) | (Part1by2(coords.y) << 1) | (Part1by2(coords.z) << 2);
    }


    private static uint3 MortonDecode3(in uint n)
    {
        uint3 v = new uint3();
        
        v.x = Unpart1by2(n);
        v.y = Unpart1by2(n >> 1);
        v.z = Unpart1by2(n >> 2);

        return v;
    }
    
    
    public static uint2[] Compress(in BoundsBox[] childBounds, in BoundsBox parentBoundsBox)
    {
        float3 parentMin = parentBoundsBox.min;
        float3 parentSize = parentBoundsBox.GetSize();
        
        StackBVH4Node stackNode = new StackBVH4Node();
        stackNode.boundsMin = parentMin;
        stackNode.extends = parentSize;
        
        uint2[] compressedBounds = new uint2[childBounds.Length];

        uint bitMask = 0x3FF;
        
        for (int i = 0; i < childBounds.Length; i++)
        {
            float3 minRatio = ((childBounds[i].min - parentMin) / parentSize)  * 1023f;
            uint3 qMin = (uint3)  (minRatio);
            
            float3 maxRatio = ((childBounds[i].max - parentMin) / parentSize) * 1023f;
            uint3 qMax = (uint3)  ( maxRatio);
            
            uint min = qMin.x & bitMask | (( qMin.y & bitMask) << 10) | (( qMin.z & bitMask) << 20) ;
            uint max = qMax.x & bitMask | (( qMax.y & bitMask) << 10) | (( qMax.z & bitMask) << 20) ;
            
            compressedBounds[i] = new uint2(min, max);
        }

        return compressedBounds;
    }

    public static BoundsBox[] DecompressAll(in StackBVH4Node stackNode)
    {
        bool isLeaf = (stackNode.data & 0b1) == 1;
        uint qtyElements = (stackNode.data >> 9) & 0b1111;
        uint qtyChildren = isLeaf ? 0 : qtyElements;
        
        BoundsBox[] bbs = new BoundsBox[qtyChildren];

        if (qtyChildren > 0)
            bbs[0] = Decompress(stackNode.bb0, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 1)
            bbs[1] = Decompress(stackNode.bb1, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 2)
            bbs[2] = Decompress(stackNode.bb2, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 3)
            bbs[3] = Decompress(stackNode.bb3, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 4)
            bbs[4] = Decompress(stackNode.bb4, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 5)
            bbs[5] = Decompress(stackNode.bb5, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 6)
            bbs[6] = Decompress(stackNode.bb6, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 7)
            bbs[7] = Decompress(stackNode.bb7, stackNode.boundsMin, stackNode.extends, 0);

        return bbs;
    }

    public static BoundsBox Decompress(in uint2 qMinMax, in float3 parentMin, in float3 parentExtends, float precisionLoss)
    {
        uint bitMask = 0x3FF;
        
        uint3 qMin = new uint3(
            qMinMax.x & bitMask, (qMinMax.x >> 10) & bitMask, (qMinMax.x >> 20) & bitMask   
        );
            
        uint3 qMax = new uint3(
            qMinMax.y & bitMask, (qMinMax.y >> 10) & bitMask, (qMinMax.y >> 20) & bitMask   
        );
            
        return new BoundsBox( 
            ((float3) qMin / 1023f ) * parentExtends + parentMin - (F3.ONE * precisionLoss),
            ((float3) qMax / 1023f ) * parentExtends + parentMin + (F3.ONE * precisionLoss));
    }
    
}