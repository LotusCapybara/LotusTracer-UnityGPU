﻿using System.IO;
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
        
        public static uint2 ReadUInt2(BinaryReader reader)
        {
            uint2 u2 = new uint2();
            u2.x = reader.ReadUInt32();
            u2.y =  reader.ReadUInt32();
            return u2;
        }
        
        public static uint3 ReadUInt3(BinaryReader reader)
        {
            uint3 u3 = new uint3();
            u3.x = reader.ReadUInt32();
            u3.y =  reader.ReadUInt32();
            u3.z =  reader.ReadUInt32();
            return u3;
        }
        
        public static uint4 ReadUInt4(BinaryReader reader)
        {
            uint4 u4 = new uint4();
            u4.x = reader.ReadUInt32();
            u4.y =  reader.ReadUInt32();
            u4.z =  reader.ReadUInt32();
            u4.w =  reader.ReadUInt32();
            return u4;
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
            mat.roughness = reader.ReadSingle();
            mat.clearCoat = reader.ReadSingle();
            mat.clearCoatRoughness = reader.ReadSingle();
            mat.metallic = reader.ReadSingle();
            mat.anisotropic = reader.ReadSingle();
            mat.normalStrength = reader.ReadSingle();
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
        
        
        public static (RenderTriangle_Vertices, RenderTriangle_Data) ReadTriangle(BinaryReader reader)
        {
            RenderTriangle_Vertices tv = new RenderTriangle_Vertices();
            tv.posA = ReadFloat3(reader);
            tv.p0p1 = ReadFloat3(reader);
            tv.p0p2 = ReadFloat3(reader);
            tv.flags = reader.ReadInt32();


            RenderTriangle_Data td = new RenderTriangle_Data();
            td.normalA = ReadFloat3(reader);
            td.normalB = ReadFloat3(reader);
            td.normalC = ReadFloat3(reader);
            td.tangentA = ReadFloat3(reader);
            td.tangentB = ReadFloat3(reader);
            td.tangentC = ReadFloat3(reader);
            td.vertexColor = ReadFloat3(reader);
            
            td.materialIndex = reader.ReadInt32();
            td.textureUV0 = ReadFloat2(reader);
            td.textureUV1 = ReadFloat2(reader);
            td.textureUV2 = ReadFloat2(reader);
            
            return (tv, td);
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
            light.castShadows = reader.ReadInt32();
            light.receiveHits = reader.ReadInt32();
            light.radius = reader.ReadSingle();
            light.area = reader.ReadSingle();
            
            return light;
        }
     
        public static StackBVH4Node ReadBvh4Node(BinaryReader reader)
        {
            StackBVH4Node node = new StackBVH4Node();

            node.data = reader.ReadUInt32();
            node.firstElementIndex = reader.ReadInt32();
            node.precisionLoss = reader.ReadSingle();
            node.boundsMin = ReadFloat3(reader);
            node.extends = ReadFloat3(reader);
            
            node.bb01 = ReadUInt4(reader);
            node.bb23 = ReadUInt4(reader);
            node.bb45 = ReadUInt4(reader);
            node.bb67 = ReadUInt4(reader);
            
            return node;
        }
        
    }
}