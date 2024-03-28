using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public struct BoundsBox
    {
        public static BoundsBox AS_SHRINK = new BoundsBox(F3.INFINITY, F3.INFINITY_INV);
        
        public float3 min;
        public float3 max;
        
        public BoundsBox(float3 boundMin, float3 boundMax)
        {
            min = boundMin;
            max = boundMax;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetSize()
        {
            return max - min;
        }

        public void ExpandWithBounds(in BoundsBox other)
        {
            min = math.min(min, other.min);
            max = math.max(max, other.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpandWithTriangle(in RenderTriangle triangle)
        {
            float3 posB = triangle.posA + triangle.p0p1;
            float3 posC = triangle.posA + triangle.p0p2;
            
            min = math.min(min, math.min(  triangle.posA, math.min(posB, posC)));
            max = math.max(max, math.max(triangle.posA, math.max(posB, posC)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpandWithPoint(float3 p)
        {
            min = math.min(min, p);
            max = math.max(max, p);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetCenter()
        {
            return new float3(
                min.x + GetSize().x * 0.5f,
                min.y + GetSize().y * 0.5f,
                min.z + GetSize().z * 0.5f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool isWithin(BoundsBox other)
        {
            return other.IsPointInside(min) && other.IsPointInside(max);
        }

        public bool IsPointInside(float3 point)
        {
            if (point.x < min.x || point.x > max.x)
                return false;

            if (point.y < min.y || point.y > max.y)
                return false;

            if (point.z < min.z || point.z > max.z)
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (float3, float3, float3, float3, float3, float3, float3, float3) GetCorners()
        {
            float3 size = GetSize();


            return (
                min, min + new float3(size.x, 0, 0), min + new float3(0, 0, size.z), min + new float3(size.x, 0, size.z),
                max, max - new float3(size.x, 0, 0), max - new float3(0, 0, size.z), max - new float3(size.x, 0, size.z));
        }

        public string ToString()
        {
            return $"{min.ToString()}:{max.ToString()}";
        }
    }
}