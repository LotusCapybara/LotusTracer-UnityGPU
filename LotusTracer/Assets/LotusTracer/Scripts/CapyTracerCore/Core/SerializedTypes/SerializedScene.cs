using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class SerializedScene
    {
        public float3 boundMin;
        public float3 boundMax;
        public SerializedCamera camera;
        public int qtyMaterials;
        public SerializedMaterial[] materials;
        public int qtyTriangles;
        public FastTriangle[] triangles;
        public int qtyLights;
        public RenderLight[] lights;
        public int qtyBVHNodes;
        public StackBVH4Node[] bvhNodes;
    }
}