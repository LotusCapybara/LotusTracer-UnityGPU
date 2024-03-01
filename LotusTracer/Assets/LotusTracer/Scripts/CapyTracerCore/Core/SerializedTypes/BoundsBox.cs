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

        public float GetArea()
        {
            float3 e = max - min; 
            return e.x * e.y + e.y * e.z + e.z * e.x;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetSize()
        {
            return max - min;
        }

        public void ExpandWithBounds(in BoundsBox other)
        {
            ExpandWithPoint(other.min);
            ExpandWithPoint(other.max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpandWithTriangle(in RenderTriangle triangle)
        {
            ExpandWithPoint(triangle.posA);
            ExpandWithPoint(triangle.posA + triangle.p0p1);
            ExpandWithPoint(triangle.posA + triangle.p0p2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExpandWithPoint(float3 p)
        {
            for (int i = 0; i < 3; i++)
            {
                min[i] = math.min(min[i], p[i]);
                max[i] = math.max(max[i], p[i]);
            }
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
        public float SquaredDistToPoint( in float3 p)
        {
            float Check(float pn, float  bmin, float bmax )
            {
                float result = 0;
                float  v = pn;
 
                if ( v < bmin ) 
                {             
                    float val = (bmin - v);             
                    result += val * val;         
                }         
         
                if ( v > bmax )
                {
                    float val = (v - bmax);
                    result += val * val;
                }

                return result;
            };
 
            // Squared distance
            float sq = 0f;
 
            sq += Check( p.x, min.x, max.x );
            sq += Check( p.y, min.y, max.y );
            sq += Check( p.z, min.z, max.z );
 
            return sq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (float3, float3, float3, float3, float3, float3, float3, float3) GetCorners()
        {
            float3 size = GetSize();


            return (
                min, min + new float3(size.x, 0, 0), min + new float3(0, 0, size.z), min + new float3(size.x, 0, size.z),
                max, max - new float3(size.x, 0, 0), max - new float3(0, 0, size.z), max - new float3(size.x, 0, size.z));
        }
    }
}