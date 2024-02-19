using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace CapyTracerCore.Core
{
    public static class AtlasFormats
    {
        public static TextureFormat ALBEDO = TextureFormat.RGB24;
        public static TextureFormat NORMAL = TextureFormat.RGB24;
        public static TextureFormat ROUGHNESS = TextureFormat.RGB24;
        public static TextureFormat METALLIC = TextureFormat.RGB24;
        public static TextureFormat EMISSION = TextureFormat.RGB24;
    }
    
    // this was in serialized from the Unity Scene into the .dat file
    [StructLayout(LayoutKind.Sequential)]
    public struct SerializedMaterial
    {
        public float emissiveIntensity;
        public float4 color;
        
        public float transmissionPower;
        public float mediumDensity;
        public float scatteringDirection;
        public float maxScatteringDistance;
        
        public float roughness;
        public float clearCoat;
        public float clearCoatRoughness;
        public float metallic;
        
        public int albedoMapIndex;
        public int albedoMapCanvasIndex;

        public int normalMapIndex;
        public int normalMapCanvasIndex;
        
        public int roughMapIndex;
        public int roughMapCanvasIndex;
        
        public int metalMapIndex;
        public int metalMapCanvasIndex;
        
        public int emissionMapIndex;
        public int emissionMapCanvasIndex;
        
        public float ior;
        
        public void GenerateRuntime()
        {
            emissiveIntensity = math.clamp(emissiveIntensity, 0, 20);
            
            // important to avoid crashes, ior can't be 0 since it's a divisor in some places
            ior = math.clamp(ior, 1f, 4f);

            mediumDensity *= transmissionPower;
        }
    }
}