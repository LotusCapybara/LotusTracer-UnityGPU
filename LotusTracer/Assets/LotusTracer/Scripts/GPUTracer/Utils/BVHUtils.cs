using System;
using CapyTracerCore.Core;
using Unity.Mathematics;

public static class BVHUtils
{
    // Interleave bits by Binary Magic Numbers
    private static ulong Part1By2(ulong n)
    {
        n &= 0x1fffff; // we only use the first 21 bits
        n = (n | n << 32) & 0x1f00000000ffff;
        n = (n | n << 16) & 0x1f0000ff0000ff;
        n = (n | n << 8) & 0x100f00f00f00f00f;
        n = (n | n << 4) & 0x10c30c30c30c30c3;
        n = (n | n << 2) & 0x1249249249249249;
        return n;
    }

    private static ulong EncodeMorton3(uint x, uint y, uint z)
    {
        return Part1By2(z) | (Part1By2(y) << 1) | (Part1By2(x) << 2);
    }
    
    // this is how it would work without splitting the ulong morton code into 2 uint (read below)
    
    // private static uint3 DecodeMorton3(ulong morton)
    // {
    //     uint3 r = new uint3();
    //     
    //     r.x = Compact1By2(morton >> 2); // Shift right twice, compact bits for x
    //     r.y = Compact1By2(morton >> 1); // Shift right once, compact bits for y
    //     r.z = Compact1By2(morton);      // No shift needed, compact bits for z
    //
    //     return r;
    // }
    //
    // // Compact bits (opposite of Part1By2)
    // private static uint Compact1By2(ulong x)
    // {
    //     x &= 0x1249249249249249; // Mask: binary 1001001001001001001001001001001001001001001001001001
    //     x = (x ^ (x >> 2)) & 0x10C30C30C30C30C3; // binary: 1000011000011000011000011000011000011000011000011
    //     x = (x ^ (x >> 4)) & 0x100F00F00F00F00F; // binary: 1000000001111000000111100000011111000000111100000001111
    //     x = (x ^ (x >> 8)) & 0x1F0000FF0000FF;   // binary: 11111000000000000011111111000000000011111111
    //     x = (x ^ (x >> 16)) & 0x1F00000000FFFF;  // binary: 111110000000000000000000000000000111111111111111
    //     x = (x ^ (x >> 32)) & 0x1FFFFF;          // binary: 111111111111111111111
    //     return (uint)x;
    // }
    
    // --- gpu versions of Morton3D decode
    // the thing is that in hlsl I dont have ulong or uint64 so I have to split the morton code
    // into 2 uint, but to be able to test this properly I'm also putting the code here in c#
    // so I can debug it
    
    // Compact bits (opposite of Part1By2)
    private static uint Compact1By2(uint x) 
    {
        x &= 0x09249249; // Mask: binary 1001001001001001001001001
        x = (x ^ (x >> 2)) & 0x030C30C3; // binary: 11000011000011000011
        x = (x ^ (x >> 4)) & 0x0300F00F; // binary: 11000000111100000011
        x = (x ^ (x >> 8)) & 0x030000FF; // binary: 11000000000000000011111111
        x = (x ^ (x >> 16)) & 0x00003FFF; // Adjust the mask for 14 bits per dimension if needed
        return x;
    }

    private static uint3 DecodeMorton3(uint2 morton) {
        uint3 result;
        // Combine bits from low and high parts for each coordinate
        result.x = Compact1By2(morton.x);  // Extract x from low bits
        result.y = Compact1By2(morton.x >> 1);  // Extract y from low bits
        result.z = Compact1By2(morton.x >> 2);  // Extract z from low bits

        // If your encoding scheme puts parts of coordinates into high, decode those as well:
        result.x |= Compact1By2(morton.y) << 32;  // Shift by 10 if low part covers 10 bits, same for others
        result.y |= Compact1By2(morton.y >> 1) << 32;
        result.z |= Compact1By2(morton.y >> 2) << 32;

        return result;
    }
    
    
    public static StackBVH4Node Compress(in BoundsBox[] childBounds, in BoundsBox parentBoundsBox)
    {
        float3 parentMin = parentBoundsBox.min;
        float3 parentSize = parentBoundsBox.GetSize();
        
        StackBVH4Node stackNode = new StackBVH4Node();
        stackNode.boundsMin = parentMin;
        stackNode.extends = parentSize;
        
        uint4[] compressedBounds = new uint4[8];

        uint mask32 = 0xFFFFFFFF;
        
        for (int i = 0; i < 8; i++)
        {
            float3 minRatio = ((childBounds[i].min - parentMin) / parentSize)  * 1023f;
            uint3 qMin = (uint3)  (minRatio);
            
            float3 maxRatio = ((childBounds[i].max - parentMin) / parentSize) * 1023f;
            uint3 qMax = (uint3)  ( maxRatio);
            
            ulong min = EncodeMorton3(qMin.x, qMin.y, qMin.z);
            ulong max = EncodeMorton3(qMax.x, qMax.y, qMax.z);

            uint4 sMinMax = new uint4();
            sMinMax.x = (uint) (min & mask32);
            sMinMax.y = (uint) ((min & mask32) >> 32);
            sMinMax.z = (uint) (max & mask32);
            sMinMax.w = (uint) ((max & mask32) >> 32);

            compressedBounds[i] = sMinMax;
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

        uint2[] mortonMins = new uint2[8];
        uint2[] mortonMaxs = new uint2[8];
        
        uint mask32 = 0xFFFFFFFF;
        mortonMins[0] = node.bb0.xy;
        mortonMaxs[0] = node.bb0.zw;

        mortonMins[1] = node.bb1.xy;
        mortonMaxs[1] = node.bb1.zw;
        
        mortonMins[2] = node.bb2.xy;
        mortonMaxs[2] = node.bb2.zw;
        
        mortonMins[3] = node.bb3.xy;
        mortonMaxs[3] = node.bb3.zw;
        
        mortonMins[4] = node.bb4.xy;
        mortonMaxs[4] = node.bb4.zw;
        
        mortonMins[5] = node.bb5.xy;
        mortonMaxs[5] = node.bb5.zw;
        
        mortonMins[6] = node.bb6.xy;
        mortonMaxs[6] = node.bb6.zw;
        
        mortonMins[7] = node.bb7.xy;
        mortonMaxs[7] = node.bb7.zw;

        for (int i = 0; i < 8; i++)
        {
            uint3 qMin = DecodeMorton3(mortonMins[i]);
            uint3 qMax = DecodeMorton3(mortonMaxs[i]);
        
            bounds[i] = new BoundsBox( 
                ((float3) qMin / 1023f ) * node.extends + node.boundsMin,
                ((float3) qMax / 1023f ) * node.extends + node.boundsMin);
        }
        
        return bounds;
    }
    
}