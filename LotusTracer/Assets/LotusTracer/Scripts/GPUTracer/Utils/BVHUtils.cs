using CapyTracerCore.Core;
using Unity.Mathematics;

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
    
    
    public static StackBVH4Node Compress(in BoundsBox[] childBounds, in BoundsBox parentBoundsBox)
    {
        float3 parentMin = parentBoundsBox.min;
        float3 parentSize = parentBoundsBox.GetSize();
        
        StackBVH4Node stackNode = new StackBVH4Node();
        stackNode.boundsMin = parentMin;
        stackNode.extends = parentSize;
        
        uint2[] compressedBounds = new uint2[8];

        uint bitMask = 0x3FF;
        
        for (int i = 0; i < 8; i++)
        {
            float3 minRatio = ((childBounds[i].min - parentMin) / parentSize)  * 1023f;
            uint3 qMin = (uint3)  (minRatio);
            
            float3 maxRatio = ((childBounds[i].max - parentMin) / parentSize) * 1023f;
            uint3 qMax = (uint3)  ( maxRatio);
            
            uint min = qMin.x & bitMask | (( qMin.y & bitMask) << 10) | (( qMin.z & bitMask) << 20) ;
            uint max = qMax.x & bitMask | (( qMax.y & bitMask) << 10) | (( qMax.z & bitMask) << 20) ;
            
            compressedBounds[i] = new uint2(min, max);
        }

        stackNode.bb0 = compressedBounds[0];
        stackNode.bb1 = compressedBounds[1];
        stackNode.bb2 = compressedBounds[2];
        stackNode.bb3 = compressedBounds[3];
        stackNode.bb4 = compressedBounds[4];
        stackNode.bb5 = compressedBounds[5];
        stackNode.bb6 = compressedBounds[6];
        stackNode.bb7 = compressedBounds[7];
        
        return stackNode;
    }
    
    public static BoundsBox[] Decompress(in StackBVH4Node node)
    {
        BoundsBox[] bounds = new BoundsBox[8];

        uint2[] qMinMax = new uint2[8];

        qMinMax[0] = node.bb0;
        qMinMax[1] = node.bb1;
        qMinMax[2] = node.bb2;
        qMinMax[3] = node.bb3;
        qMinMax[4] = node.bb4;
        qMinMax[5] = node.bb5;
        qMinMax[6] = node.bb6;
        qMinMax[7] = node.bb7;

        uint bitMask = 0x3FF;
        
        for (int i = 0; i < 8; i++)
        {
            uint3 qMin = new uint3(
                qMinMax[i].x & bitMask, (qMinMax[i].x >> 10) & bitMask, (qMinMax[i].x >> 20) & bitMask   
            );
            
            uint3 qMax = new uint3(
                qMinMax[i].y & bitMask, (qMinMax[i].y >> 10) & bitMask, (qMinMax[i].y >> 20) & bitMask   
            );
            
            bounds[i] = new BoundsBox( 
                ((float3) qMin / 1023f ) * node.extends + node.boundsMin - (F3.ONE * node.precisionLoss),
                ((float3) qMax / 1023f ) * node.extends + node.boundsMin + (F3.ONE * node.precisionLoss));
        }
        
        return bounds;
    }
    
}