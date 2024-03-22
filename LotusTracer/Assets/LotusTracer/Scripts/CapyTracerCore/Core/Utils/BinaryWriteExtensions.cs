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
        
        public static void WriteBinary(this uint2 u2, BinaryWriter writer)
        {
            writer.Write(u2.x);
            writer.Write(u2.y);
        }
        
        public static void WriteBinary(this uint3 u3, BinaryWriter writer)
        {
            writer.Write(u3.x);
            writer.Write(u3.y);
            writer.Write(u3.z);
        }
        
        public static void WriteBinary(this uint4 u4, BinaryWriter writer)
        {
            writer.Write(u4.x);
            writer.Write(u4.y);
            writer.Write(u4.z);
            writer.Write(u4.w);
        }
        
    
        public static void WriteBinary(this SerializedMaterial mat, BinaryWriter writer)
        {
            writer.Write(mat.emissiveIntensity);
            mat.color.WriteBinary(writer);
            writer.Write(mat.transmissionPower);
            writer.Write(mat.mediumDensity);
            writer.Write(mat.scatteringDirection);
            writer.Write(mat.roughness);
            writer.Write(mat.clearCoat);
            writer.Write(mat.clearCoatRoughness);
            writer.Write(mat.metallic);
            writer.Write(mat.anisotropic);
            writer.Write(mat.normalStrength);
            writer.Write(mat.albedoMapIndex);
            writer.Write(mat.albedoMapCanvasIndex);
            writer.Write(mat.normalMapIndex);
            writer.Write(mat.normalMapCanvasIndex);
            writer.Write(mat.roughMapIndex);
            writer.Write(mat.roughMapCanvasIndex);
            writer.Write(mat.metalMapIndex);
            writer.Write(mat.metalMapCanvasIndex);
            writer.Write(mat.emissionMapIndex);
            writer.Write(mat.emissionMapCanvasIndex);
            writer.Write(mat.ior);
            writer.Write(mat.flags);
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
        
        
        public static void WriteBinary(this RenderTriangle t, BinaryWriter writer)
        {
            // Render Triangle Vertices
            t.posA.WriteBinary(writer);
            t.p0p1.WriteBinary(writer);
            t.p0p2.WriteBinary(writer);
            writer.Write(t.flags);
            
            // Render Triangle Data
            t.normalA.WriteBinary(writer);
            t.normalB.WriteBinary(writer);
            t.normalC.WriteBinary(writer);
            t.tangentA.WriteBinary(writer);
            t.tangentB.WriteBinary(writer);
            t.tangentC.WriteBinary(writer);
            t.vertexColor.WriteBinary(writer);
            writer.Write(t.materialIndex);
            t.textureUV0.WriteBinary(writer);
            t.textureUV1.WriteBinary(writer);
            t.textureUV2.WriteBinary(writer);
            
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
            writer.Write(light.castShadows);
            writer.Write(light.receiveHits);
            writer.Write(light.radius);
            writer.Write(light.area);
        }
        
        public static void WriteBinary(this StackBVH4Node node, BinaryWriter writer)
        {
            writer.Write(node.data);
            writer.Write(node.startIndex);
            writer.Write(node.qtyTriangles);
            
            writer.Write(node.precisionLoss);
            
            node.boundsMin.WriteBinary(writer);
            node.extends.WriteBinary(writer);
            
            node.bb0.WriteBinary(writer);
            node.bb1.WriteBinary(writer);
            node.bb2.WriteBinary(writer);
            node.bb3.WriteBinary(writer);
        }
    }
}