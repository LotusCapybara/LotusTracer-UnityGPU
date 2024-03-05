using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public class SerializedScene_Data
    {
        public SerializedCamera camera;
        public int qtyMaterials;
        public SerializedMaterial[] materials;
        
        public int qtyLights;
        public RenderLight[] lights;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SerializedScene_Geometry
    {
        public float3 boundMin;
        public float3 boundMax;
        public int qtyTriangles;
        public RenderTriangle_Vertices[] triangleVertices;
        public RenderTriangle_Data[] triangleDatas;
        public int qtyBVHNodes;
        public StackBVH4Node[] bvhNodes;
    }
}