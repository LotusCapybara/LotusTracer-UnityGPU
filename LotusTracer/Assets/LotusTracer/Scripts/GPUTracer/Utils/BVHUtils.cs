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
    
    
    public static uint4[] Compress(in BoundsBox[] childBounds, in BoundsBox parentBoundsBox)
    {
        float3 parentMin = parentBoundsBox.min;
        float3 parentSize = parentBoundsBox.GetSize();
        
        StackBVH4Node stackNode = new StackBVH4Node();
        stackNode.boundsMin = parentMin;
        stackNode.extends = parentSize;

        int chQty = childBounds.Length;

        uint2 bb0 = chQty > 0 ? CompressSingleBounds(childBounds[0], parentMin, parentSize) : new uint2();
        uint2 bb1 = chQty > 1 ? CompressSingleBounds(childBounds[1], parentMin, parentSize) : new uint2();
        uint2 bb2 = chQty > 2 ? CompressSingleBounds(childBounds[2], parentMin, parentSize) : new uint2();
        uint2 bb3 = chQty > 3 ? CompressSingleBounds(childBounds[3], parentMin, parentSize) : new uint2();
        uint2 bb4 = chQty > 4 ? CompressSingleBounds(childBounds[4], parentMin, parentSize) : new uint2();
        uint2 bb5 = chQty > 5 ? CompressSingleBounds(childBounds[5], parentMin, parentSize) : new uint2();
        uint2 bb6 = chQty > 6 ? CompressSingleBounds(childBounds[6], parentMin, parentSize) : new uint2();
        uint2 bb7 = chQty > 7 ? CompressSingleBounds(childBounds[7], parentMin, parentSize) : new uint2();
        
        uint4[] compressedBounds = new uint4[8];

        compressedBounds[0] = new uint4(bb0.x, bb0.y, bb1.x, bb1.y);
        compressedBounds[1] = new uint4(bb2.x, bb2.y, bb3.x, bb3.y);
        compressedBounds[2] = new uint4(bb4.x, bb4.y, bb5.x, bb5.y);
        compressedBounds[3] = new uint4(bb6.x, bb6.y, bb7.x, bb7.y);

        return compressedBounds;
    }

    private static uint2 CompressSingleBounds(in BoundsBox box, float3 parentMin, float3 parentSize)
    {
        uint bitMask = 0x3FF;
        
        float3 minRatio = ((box.min - parentMin) / parentSize)  * 1023f;
        uint3 qMin = (uint3)  (minRatio);
            
        float3 maxRatio = ((box.max - parentMin) / parentSize) * 1023f;
        uint3 qMax = (uint3)  ( maxRatio);
            
        uint min = qMin.x & bitMask | (( qMin.y & bitMask) << 10) | (( qMin.z & bitMask) << 20) ;
        uint max = qMax.x & bitMask | (( qMax.y & bitMask) << 10) | (( qMax.z & bitMask) << 20) ;
            
        return new uint2(min, max);
    }

    public static BoundsBox[] DecompressAll(in StackBVH4Node stackNode)
    {
        bool isLeaf = (stackNode.data & 0b1) == 1;
        uint qtyElements = (stackNode.data >> 9) & 0b1111;
        uint qtyChildren = isLeaf ? 0 : qtyElements;
        
        BoundsBox[] bbs = new BoundsBox[qtyChildren];

        if (qtyChildren > 0)
            bbs[0] = Decompress(stackNode.bb01.xy, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 1)
            bbs[1] = Decompress(stackNode.bb01.zw, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 2)
            bbs[2] = Decompress(stackNode.bb23.xy, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 3)
            bbs[3] = Decompress(stackNode.bb23.zw, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 4)
            bbs[4] = Decompress(stackNode.bb45.xy, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 5)
            bbs[5] = Decompress(stackNode.bb45.zw, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 6)
            bbs[6] = Decompress(stackNode.bb67.xy, stackNode.boundsMin, stackNode.extends, 0);
        if (qtyChildren > 7)
            bbs[7] = Decompress(stackNode.bb67.zw, stackNode.boundsMin, stackNode.extends, 0);

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