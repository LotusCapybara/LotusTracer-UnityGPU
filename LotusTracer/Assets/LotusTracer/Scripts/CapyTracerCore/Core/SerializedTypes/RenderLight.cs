using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public enum ELightType
    {
        Spot = 0,
        Directional = 1,
        Point = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderLight
    {
        public float4 color;
        public float3 position;
        public float3 forward;
        public float range;
        public float intensity;
        public float angle;
        public int type;
        public int castShadows;
    }
}