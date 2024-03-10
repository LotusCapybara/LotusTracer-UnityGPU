using CapyTracerCore.Core;
using Unity.Mathematics;

public static class BVHUtils
{
    public static uint3 Compress(in BoundsBox bounds, in BoundsBox sceneBounds, float3 sceneExtends)
    {
        float twoPow = 65535f;
        float3 bias = sceneBounds.min;
        float3 scale = twoPow / sceneExtends;


        uint3 min = new uint3(); 
        min.x = (uint) math.clamp( (bounds.min.x - bias.x) * scale.x, 0, twoPow);
        min.y = (uint) math.clamp( (bounds.min.y - bias.y) * scale.y, 0, twoPow);
        min.z = (uint) math.clamp( (bounds.min.z - bias.z) * scale.z, 0, twoPow);

        uint3 max = new uint3(); 
        max.x = (uint) math.clamp( (bounds.max.x - bias.x) * scale.x, 0, twoPow);
        max.y = (uint) math.clamp( (bounds.max.y - bias.y) * scale.y, 0, twoPow);
        max.z = (uint) math.clamp( (bounds.max.z - bias.z) * scale.z, 0, twoPow);

        uint3 result = new uint3(0, 0, 0);
        
        result.x = ( (max.x & 0xFFFF) << 16 ) | (min.x & 0xFFFF);
        result.y = ( (max.y & 0xFFFF) << 16 ) | (min.y & 0xFFFF);
        result.z = ( (max.z & 0xFFFF) << 16 ) | (min.z & 0xFFFF);

        return result;
    }
    
    public static BoundsBox Decompress(in uint3 compressedCoords, in BoundsBox sceneBounds, float3 sceneExtends)
    {
        float twoPow = 65535f;
        float3 bias = sceneBounds.min;
        float3 scale = twoPow / sceneExtends;
        
        uint3 minCompressed = new uint3(
            compressedCoords.x & 0xFFFF,
            compressedCoords.y & 0xFFFF,
            compressedCoords.z & 0xFFFF
        );
        uint3 maxCompressed = new uint3(
            (compressedCoords.x >> 16) & 0xFFFF,
            (compressedCoords.y >> 16) & 0xFFFF,
            (compressedCoords.z >> 16) & 0xFFFF
        );
        
        
        float3 min = float3.zero;
        min.x = (minCompressed.x / scale.x) + bias.x;
        min.y = (minCompressed.y / scale.y) + bias.y;
        min.z = (minCompressed.z / scale.z) + bias.z;
        
        float3 max = float3.zero;
        max.x = (maxCompressed.x / scale.x) + bias.x;
        max.y = (maxCompressed.y / scale.y) + bias.y;
        max.z = (maxCompressed.z / scale.z) + bias.z;
        

        return new BoundsBox(min, max);
    }
    
}