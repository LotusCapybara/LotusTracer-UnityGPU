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
    struct BounceHitInfo
    {
        int triangleIndex;
        int rayIndex;
    };
}

