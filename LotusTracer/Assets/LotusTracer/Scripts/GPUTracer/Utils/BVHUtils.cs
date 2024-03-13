using CapyTracerCore.Core;
using Unity.Mathematics;

public static class BVHUtils
{
    public static StackBVH4Node Compress(in BoundsBox[] childBounds, in BoundsBox parentBoundsBox)
    {
        uint3[] quantizedMins = new uint3[8];
        uint3[] quantizedMaxs = new uint3[8];

        float3 parentMin = parentBoundsBox.min;
        float3 parentSize = parentBoundsBox.GetSize();
        
        StackBVH4Node stackNode = new StackBVH4Node();
        stackNode.boundsMin = parentMin;
        stackNode.extends = parentSize;
        
        for (int i = 0; i < 8; i++)
        {
            float3 minRatio = ((childBounds[i].min - parentMin) / parentSize)  * 255f;
            quantizedMins[i] = (uint3)  (minRatio);
            
            float3 maxRatio = ((childBounds[i].max - parentMin) / parentSize) * 255f;
            quantizedMaxs[i] = (uint3)  ( maxRatio);
        }

        uint2 xMins = new uint2(0, 0);
        uint2 xMaxs = new uint2(0, 0);
        uint2 yMins = new uint2(0, 0);
        uint2 yMaxs = new uint2(0, 0);
        uint2 zMins = new uint2(0, 0);
        uint2 zMaxs = new uint2(0, 0);
        
        for (int i = 0; i < 4; i++)
        {
            // x min max
            xMins.x |= ((quantizedMins[i].x & 0xFF) << (8 * i));
            xMins.y |= ((quantizedMins[i+ 4].x & 0xFF) << (8 * i));
            
            xMaxs.x |= ((quantizedMaxs[i].x & 0xFF) << (8 * i));
            xMaxs.y |= ((quantizedMaxs[i+ 4].x & 0xFF) << (8 * i));
            
            // y min max
            yMins.x |= ((quantizedMins[i].y & 0xFF) << (8 * i));
            yMins.y |= ((quantizedMins[i+ 4].y & 0xFF) << (8 * i));
            
            yMaxs.x |= ((quantizedMaxs[i].y & 0xFF) << (8 * i));
            yMaxs.y |= ((quantizedMaxs[i+ 4].y & 0xFF) << (8 * i));
            
            // z min max
            zMins.x |= ((quantizedMins[i].z & 0xFF) << (8 * i));
            zMins.y |= ((quantizedMins[i+ 4].z & 0xFF) << (8 * i));
            
            zMaxs.x |= ((quantizedMaxs[i].z & 0xFF) << (8 * i));
            zMaxs.y |= ((quantizedMaxs[i+ 4].z & 0xFF) << (8 * i));
        }
        

        stackNode.xMins = xMins;
        stackNode.xMaxs = xMaxs;
        stackNode.yMins = yMins;
        stackNode.yMaxs = yMaxs;
        stackNode.zMins = zMins;
        stackNode.zMaxs = zMaxs;
        
        return stackNode;
    }
    
    public static BoundsBox[] Decompress(in StackBVH4Node node)
    {
        BoundsBox[] bounds = new BoundsBox[8];

        uint3[] quantizedMins = new uint3[8];
        uint3[] quantizedMaxs = new uint3[8];

        for (int i = 0; i < 4; i++)
        {
                quantizedMins[i] = new uint3((node.xMins.x >> (8 * i) )& 0xFF, (node.yMins.x >> (8 * i) )& 0xFF, (node.zMins.x >> (8 * i) )& 0xFF);
                quantizedMaxs[i] = new uint3((node.xMaxs.x >> (8 * i) )& 0xFF, (node.yMaxs.x >> (8 * i) )& 0xFF, (node.zMaxs.x >> (8 * i) )& 0xFF);
                quantizedMins[i + 4] = new uint3((node.xMins.y >> (8 * i) )& 0xFF, (node.yMins.y >> (8 * i) )& 0xFF, (node.zMins.y >> (8 * i) )& 0xFF);
                quantizedMaxs[i + 4] = new uint3((node.xMaxs.y >> (8 * i) )& 0xFF, (node.yMaxs.y >> (8 * i) )& 0xFF, (node.zMaxs.y >> (8 * i) )& 0xFF);
        }
        
        for (int i = 0; i < 8; i++)
        {
            bounds[i] = new BoundsBox( 
                ((float3) quantizedMins[i] / 255f ) * node.extends + node.boundsMin,
                ((float3) quantizedMaxs[i] / 255f ) * node.extends + node.boundsMin);
        }
        
        return bounds;
    }
    
}