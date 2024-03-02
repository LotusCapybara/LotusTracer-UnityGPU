using Unity.Mathematics;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public static class F3
    {
        public static float3 ONE = new float3(1f, 1f, 1f);
        public static float3 INFINITY = ONE * math.INFINITY;
        public static float3 INFINITY_INV = ONE * -math.INFINITY;

    }

    public static class V3Extensions
    {
        public static Vector4 toVector4(this float3 v3)
        {
            return new Vector4(v3.x, v3.y, v3.z, 0);
        }
    }
}