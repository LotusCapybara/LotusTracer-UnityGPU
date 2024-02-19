using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FastTriangle
    {
        public float3 posA;
        public float3 p0p1;
        public float3 p0p2;
        public float3 normalA;
        public float3 normalB;
        public float3 normalC;
        public float3 centerPos;
        public int materialIndex;
        public float2 textureUV0;
        public float2 textureUV1;
        public float2 textureUV2;
        public float3 tangentA;
        public float3 tangentB;
        public float3 tangentC;
        public float3 biTangentA;
        public float3 biTangentB;
        public float3 biTangentC;

        // a set of flags for this triangle packed into an int
        // cant' use booleans since the compute buffers don't seem to receive booleans
        // for buffered structs?
        // last bits xxxxx
        // --------------x : is invisible light bouncer
        // -------------x: 
        public int flags;

        public void SetIsInvisibleLightBouncer()
        {
            flags |= 0b1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexPos(int index, float3 value)
        {
            if (index == 0)
            {
                posA = value;
                return;
            }
        
            if (index == 1)
            {
                p0p1 = value - posA;
                return;
            }
        
            if (index == 2)
            {
                p0p2 = value - posA;
                return;
            }
                
        
            throw new IndexOutOfRangeException();
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexNormal(int index, float3 value)
        {
            if (index == 0)
            {
                normalA = value;
                return;
            }
        
            if (index == 1)
            {
                normalB = value;
                return;
            }
        
            if (index == 2)
            {
                normalC = value;
                return;
            }
        
            throw new IndexOutOfRangeException();
        }
        
        // IMPORTANT! call this after setting the normals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexTangent(int index, float4 value)
        {
            if (index == 0)
            {
                tangentA = value.xyz;
                biTangentA = math.cross(normalA, tangentA);
                return;
            }
        
            if (index == 1)
            {
                tangentB = value.xyz;
                biTangentB = math.cross(normalB, tangentB.xyz);
                return;
            }
        
            if (index == 2)
            {
                tangentC = value.xyz;
                biTangentC = math.cross(normalC, tangentC.xyz);
                return;
            }
        
            throw new IndexOutOfRangeException();
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTextureUV(int index, float2 uv)
        {
            if (index == 0)
            {
                textureUV0 = uv;
                return;
            }
        
            if (index == 1)
            {
                textureUV1 = uv;
                return;
            }
        
            if (index == 2)
            {
                textureUV2 = uv;
                return;
            }
        
            throw new IndexOutOfRangeException();
        }
        
    }
}