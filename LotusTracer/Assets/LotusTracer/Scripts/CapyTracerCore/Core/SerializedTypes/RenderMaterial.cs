using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public static class AtlasFormats
    {
        public static TextureFormat FULL_COLOR = TextureFormat.RGB24;
        public static TextureFormat FLOAT_COLOR = TextureFormat.RGBAFloat;
        public static TextureFormat R_CHANNEL_ONLY = TextureFormat.R8;

        public static Dictionary<TextureFormat, int> s_channelsByFormat = new()
        {
            { FULL_COLOR, 3 }, {FLOAT_COLOR, 4}, {R_CHANNEL_ONLY, 1}
        };
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