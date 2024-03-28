using CapyTracerCore.Core;
using Unity.Mathematics;

namespace LotusTracer.Scripts.Debug.BVH
{
    public class BVHTestFunctions
    {
        private static int firstbitlow(uint v)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((v & 0x1) != 0)
                    return i;

                v = (v >> 1);
            }
            
            return -1;
        }
        
        private static bool DoesRayHitBounds(in RenderRay r, in BoundsBox b, out float dist, in float3 invDirection)
        {
            float3 t1 = (b.min - r.origin) * invDirection;
            float3 t2 = (b.max - r.origin) * invDirection;
    
            float3 tmin3 = math.min(t1, t2);
            float3 tmax3 = math.max(t1, t2);
    
            float tmin = math.max(math.max(tmin3.x, tmin3.y), tmin3.z);
            float tmax = math.min(math.min(tmax3.x, tmax3.y), tmax3.z);

            dist = tmin;
    
            return tmax >= tmin;
        }
        
        private static float FastIntersectRayWithTriangle(in RenderTriangle_Vertices tri, in RenderRay ray, float maxDistance, bool isFirstBounce)
        {

            if(isFirstBounce && (tri.flags & 0x1) == 1)
                return 0;
    
            float3 pVec = math.cross(ray.direction, tri.p0p2);
            float det = math.dot(tri.p0p1, pVec);
        
            if (math.abs(det) < 1E-8)
                return 0;
        
            float invDet = 1.0f / det;

            float3 tVec = ray.origin - tri.posA;
            float u = math.dot(tVec, pVec) * invDet;
        
            if (u < 0 || u > 1)
                return 0;
        
            float3 qVec = math.cross(tVec, tri.p0p1);
        
            float v = math.dot(ray.direction, qVec) * invDet;
        
            if (v < 0 || (u + v) > 1)
                return 0;
        
            float distance = math.dot(tri.p0p2, qVec) * invDet;
        
            if (distance <= 0 || distance > maxDistance)
                return 0;
        
            return distance;
        }
        

        public static int TestRay(RenderRay ray, SerializedScene_Geometry sceneGeometry)
        {
            float3 invDirection = 1.0f / ray.direction;
            
            float closestDistance = 999999999f;
            int hittingTriangleIndex = -1;

            // I'll call to this uint2: node group, which is
            // the data of the node and its children
            // the x contains the index of the node in the _AccelTree array
            // while the y contains a bit mask of the children that need to be visited
            // for instance 0000 0011 means there are still 1 children to be visited
            // the tree is constructed in a way that each node contains the children as contiguous nodes
            // in the array of nodes.
            // This is based on multiple reads you can find online (papers, etc)
            uint2[] shortStack = new uint2[16];
            int stackIndex = 0;

            uint startMask = (sceneGeometry.bvhNodes[0].data >> 1) & 0xff;
            
            shortStack[stackIndex] = new uint2 (0, startMask); // change the y mask based on the amount of children    

            
            while (stackIndex >= 0)
            {
                uint2 nodeGroup = shortStack[stackIndex--];

                // if there are still children to be visit in this node group
                if((nodeGroup.y & 0xff) != 0)
                {
                    int chBit = firstbitlow(nodeGroup.y);

                    nodeGroup.y &= (uint) ~(1<<chBit);

                    if( (nodeGroup.y & 0xff) != 0)
                    {
                        shortStack[++stackIndex] = nodeGroup;
                    }            
                    
                    int chIndex = sceneGeometry.bvhNodes[nodeGroup.x].childFirstIndex + chBit;

                    StackBVH4Node childNode = sceneGeometry.bvhNodes[chIndex];

                    if(childNode.qtyTriangles > 0)
                    {
                        for (int tIndex = childNode.triangleFirstIndex; tIndex < (childNode.triangleFirstIndex + childNode.qtyTriangles); tIndex++)
                        {
                            float hitDistance = FastIntersectRayWithTriangle( sceneGeometry.triangleVertices[tIndex], ray, closestDistance, false);
                            
                            if (hitDistance > 0 &&  hitDistance < closestDistance)
                            {
                                closestDistance = hitDistance;
                                hittingTriangleIndex = tIndex;
                            }
                            int temp = 0;
                        }    
                    }
                    
                    if(childNode.childFirstIndex < 0)
                        continue;

                    uint2[] qMinMax = new uint2[8];
                    qMinMax[0] = childNode.bb0;
                    qMinMax[1] = childNode.bb1;
                    qMinMax[2] = childNode.bb2;
                    qMinMax[3] = childNode.bb3;
                    qMinMax[4] = childNode.bb4;
                    qMinMax[5] = childNode.bb5;
                    qMinMax[6] = childNode.bb6;
                    qMinMax[7] = childNode.bb7;

                    int hitMask = 0;
                        
                    for(int ch = 0; ch < childNode.childQty; ch++)
                    {
                        bool isTrasversable =  ((childNode.data >> (ch + 1)) & 0x1)  == 1;

                        if(isTrasversable)
                        {
                            float entryDist;
                            BoundsBox childBounds = BVHUtils.Decompress(qMinMax[ch], childNode.boundsMin, childNode.extends, childNode.precisionLoss);
                            bool intersects = DoesRayHitBounds(ray, childBounds, out entryDist, invDirection);
                        
                            // bool intersects = DoesRayHitBounds(ray,  childrenBounds[ch], entryDist, invDirection);
                            if(intersects) // && entryDist < closestDistance)
                            {
                                hitMask = hitMask | (1 << ch);
                            }
                        }
                    }            
                        
                    // if the ray hit against any of the children of this child,
                    // we should add to the stack this child as a new group 
                    if(hitMask != 0)
                    {
                        shortStack[++stackIndex] = new uint2((uint)chIndex, (uint)hitMask);
                    }       
                }
            }

            UnityEngine.Debug.LogError($"index {hittingTriangleIndex}");
            return hittingTriangleIndex;
        }
    }
}