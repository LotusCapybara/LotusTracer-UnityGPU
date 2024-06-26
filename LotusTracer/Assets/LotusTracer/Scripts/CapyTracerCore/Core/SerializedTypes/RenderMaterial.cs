﻿using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public static class AtlasFormats
    {
        public static TextureFormat FULL_COLOR = TextureFormat.RGBA32;
        public static TextureFormat NORMAL = TextureFormat.RGBAFloat;
        public static TextureFormat R_CHANNEL_ONLY = TextureFormat.R8;
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
        
        public float roughness;
        public float clearCoat;
        public float clearCoatRoughness;
        public float metallic;
        public float anisotropic;
        public float normalStrength;
        
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

        // from back to front (so watch this in inverse)
        // bits: 0, 1, 2: for diffuse model (lambert, oren-nayar, disney)
        // bit: 12 - if on: flip normal green channel
        // bit: 13 - if on: one minus rough map
        public int flags;
        
        public void GenerateRuntime()
        {
            emissiveIntensity = math.clamp(emissiveIntensity, 0, 100);
            
            // important to avoid crashes, ior can't be 0 since it's a divisor in some places
            ior = math.clamp(ior, 1f, 4f);

            mediumDensity *= transmissionPower;
        }
    }
}