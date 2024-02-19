using System.IO;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class BinaryWriteExtensions
    {
        public static void WriteBinary(this float2 f2, BinaryWriter writer)
        {
            writer.Write(f2.x);
            writer.Write(f2.y);
        }
        
        public static void WriteBinary(this float3 f3, BinaryWriter writer)
        {
            writer.Write(f3.x);
            writer.Write(f3.y);
            writer.Write(f3.z);
        }
        
        public static void WriteBinary(this float4 f4, BinaryWriter writer)
        {
            writer.Write(f4.x);
            writer.Write(f4.y);
            writer.Write(f4.z);
            writer.Write(f4.w);
        }
    
        public static void WriteBinary(this SerializedMaterial mat, BinaryWriter writer)
        {
            writer.Write(mat.emissiveIntensity);
            mat.color.WriteBinary(writer);
            writer.Write(mat.transmissionPower);
            writer.Write(mat.mediumDensity);
            writer.Write(mat.scatteringDirection);
            writer.Write(mat.maxScatteringDistance);
            writer.Write(mat.roughness);
            writer.Write(mat.clearCoat);
            writer.Write(mat.clearCoatRoughness);
            writer.Write(mat.metallic);
            writer.Write(mat.albedoMapIndex);
            writer.Write(mat.albedoMapCanvasIndex);
            writer.Write(mat.normalMapIndex);
            writer.Write(mat.normalMapCanvasIndex);
            writer.Write(mat.roughMapIndex);
            writer.Write(mat.roughMapCanvasIndex);
            writer.Write(mat.metalMapIndex);
            writer.Write(mat.metalMapCanvasIndex);
            writer.Write(mat.ior);
        }
        
        public static void WriteBinary(this SerializedCamera cam, BinaryWriter writer)
        {
            cam.position.WriteBinary(writer);
            cam.forward.WriteBinary(writer);
            cam.right.WriteBinary(writer);
            cam.up.WriteBinary(writer);
            writer.Write(cam.horizontalSize);
            writer.Write(cam.fov);
        }
        
        
        public static void WriteBinary(this FastTriangle t, BinaryWriter writer)
        {
            t.posA.WriteBinary(writer);
            t.p0p1.WriteBinary(writer);
            t.p0p2.WriteBinary(writer);
            t.normalA.WriteBinary(writer);
            t.normalB.WriteBinary(writer);
            t.normalC.WriteBinary(writer);
            t.tangentA.WriteBinary(writer);
            t.tangentB.WriteBinary(writer);
            t.tangentC.WriteBinary(writer);
            t.biTangentA.WriteBinary(writer);
            t.biTangentB.WriteBinary(writer);
            t.biTangentC.WriteBinary(writer);
            t.centerPos.WriteBinary(writer);
            writer.Write(t.materialIndex);
            t.textureUV0.WriteBinary(writer);
            t.textureUV1.WriteBinary(writer);
            t.textureUV2.WriteBinary(writer);
            writer.Write(t.flags);
        }
        
        public static void WriteBinary(this RenderLight light, BinaryWriter writer)
        {
            light.color.WriteBinary(writer);
            light.position.WriteBinary(writer);
            light.forward.WriteBinary(writer);
            writer.Write(light.range);
            writer.Write(light.intensity);
            writer.Write(light.angle);
            writer.Write(light.type);
        }
        
        public static void WriteBinary(this StackBVH4Node node, BinaryWriter writer)
        {
            writer.Write(node.data);
            writer.Write(node.startIndex);
            writer.Write(node.qtyTriangles);
            node.bounds.min.WriteBinary(writer);
            node.bounds.max.WriteBinary(writer);
        }
    }
}