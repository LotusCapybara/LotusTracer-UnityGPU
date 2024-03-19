using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    struct BufferSizes
    {
        int qtyRays;
        int qtyBounceHits;
        int qtyValidSamples;
    };
    
    [StructLayout(LayoutKind.Sequential)]
    struct TriangleHitInfo
    {
        bool isTriangle; // either triangle or sdf (for point lights, etc)
        bool isFrontFace;
        float distance;
        float3 normal;
        float3 tangent;
        float3 biTangent;
        float3 position;
        float3 vertexColor;
        
        // inverse direction of the ray that made this hit
        float3 backRayDirection;

        uint triangleIndex;
        int materialIndex;
        float2 textureUV;
    };
    
    
    [StructLayout(LayoutKind.Sequential)]
    struct BounceHitInfo
    {
        int pixelIndex;
        TriangleHitInfo hitInfo;
    };
    
    [StructLayout(LayoutKind.Sequential)]
    struct BounceSample
    {
        int isValid;
        float3 throughput;
        float pdf;
        float3 sampledDir;
        float3 sampledPos;
    };
}

