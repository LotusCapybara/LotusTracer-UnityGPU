using System.Collections.Generic;
using UnityEngine;

namespace CapyTracerCore.Core
{

    public class BinaryNode
    {
        public BoundsBox bounds;
        public BinaryNode left;
        public BinaryNode right;
        public int depth;
        public bool isLeaf;
        public int objectSplitDepth;

        public int subTreeTrianglesQty;
        public List<int> triangleIndices;

        public BinaryNode(BoundsBox bounds, int trianglesCount)
        {
            this.bounds = bounds;
            left = null;
            right = null;
            depth = 0;
            triangleIndices = new List<int>();
            
            for(int i = 0; i < trianglesCount; i++)
                triangleIndices.Add(i);

            subTreeTrianglesQty = triangleIndices.Count;
            isLeaf = true;
        }

        // public void FinishGeneration(RenderTriangle[] allTriangles)
        // {
        //     bounds = BoundsBox.AS_SHRINK;
        //     
        //     if (isLeaf)
        //     {
        //         expanded = triangleIndices.Count > 0;
        //         
        //         foreach(int tIndex in triangleIndices)
        //         {
        //             bounds.ExpandWithPoint(allTriangles[tIndex].posA);
        //             bounds.ExpandWithPoint(allTriangles[tIndex].posA + allTriangles[tIndex].p0p1);
        //             bounds.ExpandWithPoint(allTriangles[tIndex].posA + allTriangles[tIndex].p0p2);
        //         }
        //     }
        //     else
        //     {
        //         expanded = true;
        //         
        //         for(int i = 0; i < children.Count; i++)
        //             children[i].FinishGeneration(allTriangles);
        //
        //         for (int i = 0; i < children.Count; i++)
        //         {
        //             if (children[i].expanded)
        //             {
        //                 bounds.ExpandWithBounds(children[i].bounds);    
        //             }
        //         }
        //
        //         if (float.IsInfinity(bounds.min.x))
        //         {
        //             isLeaf = true;
        //             expanded = false;
        //         }
        //             
        //     }
        // }
        
        public void GetAllNodes(List<BinaryNode> nodes, in RenderTriangle[] allTriangles)
        {
            if (isLeaf)
            {
                if (triangleIndices.Count > 0)
                {
                    // bounds = BoundsBox.AS_SHRINK;
                    // bounds.ExpandWithTriangle(allTriangles[triangleIndices[0]]);
                    // center = bounds.GetCenter();
                }
            }
            else
            {
                nodes.Add(left);
                nodes.Add(right);
                
                left.GetAllNodes(nodes, allTriangles);
                right.GetAllNodes(nodes, allTriangles);
            }
        }
    }
}