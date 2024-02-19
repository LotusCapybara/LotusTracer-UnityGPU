using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class F3
    {
        public static float3 ONE = new float3(1f, 1f, 1f);
        public static float3 INFINITY = ONE * math.INFINITY;
        public static float3 INFINITY_INV = ONE * -math.INFINITY;

    }
}