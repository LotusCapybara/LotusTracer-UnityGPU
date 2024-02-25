using System.IO;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class SceneBinaryRead
    {
        public static float2 ReadFloat2(BinaryReader reader)
        {
            float2 f2 = float2.zero;
            f2.x =  reader.ReadSingle();
            f2.y =  reader.ReadSingle();
            return f2;
        }
        
        public static float3 ReadFloat3(BinaryReader reader)
        {
            float3 f3 = float3.zero;
            f3.x =  reader.ReadSingle();
            f3.y =  reader.ReadSingle();
            f3.z =  reader.ReadSingle();
            return f3;
        }
        
        public static float4 ReadFloat4(BinaryReader reader)
        {
            float4 f4 = float4.zero;
            f4.x =  reader.ReadSingle();
            f4.y =  reader.ReadSingle();
            f4.z =  reader.ReadSingle();
            f4.w =  reader.ReadSingle();
            return f4;
        }
    
        public static SerializedMaterial ReadMaterial(BinaryReader reader)
        {
            SerializedMaterial mat = new SerializedMaterial();

            mat.emissiveIntensity = reader.ReadSingle();
            mat.color = ReadFloat4(reader);
            mat.transmissionPower = reader.ReadSingle();
            mat.mediumDensity = reader.ReadSingle();
            mat.scatteringDirection = reader.ReadSingle();
            mat.maxScatteringDistance = reader.ReadSingle();
            mat.roughness = reader.ReadSingle();
            mat.clearCoat = reader.ReadSingle();
            mat.clearCoatRoughness = reader.ReadSingle();
            mat.metallic = reader.ReadSingle();
            mat.albedoMapIndex = reader.ReadInt32();
            mat.albedoMapCanvasIndex = reader.ReadInt32();
            mat.normalMapIndex = reader.ReadInt32();
            mat.normalMapCanvasIndex = reader.ReadInt32();
            mat.roughMapIndex = reader.ReadInt32();
            mat.roughMapCanvasIndex = reader.ReadInt32();
            mat.metalMapIndex = reader.ReadInt32();
            mat.metalMapCanvasIndex = reader.ReadInt32();
            mat.emissionMapIndex = reader.ReadInt32();
            mat.emissionMapCanvasIndex = reader.ReadInt32();
            mat.ior = reader.ReadSingle();
            mat.flags = reader.ReadInt32();
            return mat;
        }
        
        public static SerializedCamera ReadCamera(BinaryReader reader)
        {
            SerializedCamera cam = new SerializedCamera();

            cam.position = ReadFloat3(reader);
            cam.forward = ReadFloat3(reader);
            cam.right = ReadFloat3(reader);
            cam.up = ReadFloat3(reader);
            cam.horizontalSize = reader.ReadSingle();
            cam.fov = reader.ReadSingle();
            

            return cam;
        }
        
        
        public static FastTriangle ReadTriangle(BinaryReader reader)
        {
            FastTriangle t = new FastTriangle();

            t.posA = ReadFloat3(reader);
            t.p0p1 = ReadFloat3(reader);
            t.p0p2 = ReadFloat3(reader);
            
            t.normalA = ReadFloat3(reader);
            t.normalB = ReadFloat3(reader);
            t.normalC = ReadFloat3(reader);
            t.tangentA = ReadFloat3(reader);
            t.tangentB = ReadFloat3(reader);
            t.tangentC = ReadFloat3(reader);
            t.biTangentA = ReadFloat3(reader);
            t.biTangentB = ReadFloat3(reader);
            t.biTangentC = ReadFloat3(reader);
            
            t.centerPos = ReadFloat3(reader);
            
            t.materialIndex = reader.ReadInt32();

            t.textureUV0 = ReadFloat2(reader);
            t.textureUV1 = ReadFloat2(reader);
            t.textureUV2 = ReadFloat2(reader);
            t.flags = reader.ReadInt32();
            return t;
        }
        
        public static RenderLight ReadLight(BinaryReader reader)
        {
            RenderLight light = new RenderLight();

            light.color = ReadFloat4(reader);
            light.position = ReadFloat3(reader);
            light.forward = ReadFloat3(reader);
            
            light.range = reader.ReadSingle();
            light.intensity = reader.ReadSingle();
            light.angle = reader.ReadSingle();
            
            light.type = reader.ReadInt32();

            return light;
        }
     
        public static StackBVH4Node ReadBvh4Node(BinaryReader reader)
        {
            StackBVH4Node node = new StackBVH4Node();

            node.data = reader.ReadUInt32();
            node.startIndex = reader.ReadInt32();
            node.qtyTriangles = reader.ReadInt32();

            node.bounds = new BoundsBox(ReadFloat3(reader), ReadFloat3(reader));

            return node;
        }
        
    }
}