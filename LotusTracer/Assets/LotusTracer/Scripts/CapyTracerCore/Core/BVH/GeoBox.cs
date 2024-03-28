using System.Collections.Generic;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public class GeoBox
    {
        public int triIndex;
        public float3 tCentroid;
        public float3 size;
        public BoundsBox bounds;

        public static List<GeoBox> CollectGeoBoxes(in RenderTriangle[] allTriangles)
        {
            List<GeoBox> allGeoBounds = new List<GeoBox>(allTriangles.Length);

            for (int t = 0; t < allTriangles.Length; t++)
            {
                GeoBox geoBox = new GeoBox();
                geoBox.triIndex = t;
                geoBox.bounds = allTriangles[t].bounds;
                geoBox.tCentroid = allTriangles[t].centerPos;
                geoBox.size = allTriangles[t].bounds.GetSize();
                
                allGeoBounds.Add(geoBox);
            }

            return allGeoBounds;
        }
    }
}