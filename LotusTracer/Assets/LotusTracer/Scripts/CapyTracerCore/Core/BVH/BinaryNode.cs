// using System.Collections.Generic;
// using UnityEngine;
//
// namespace CapyTracerCore.Core
// {
//     
//     public class BinaryNode
//     {
//         public BoundsBox bounds;
//         public BinaryNode left;
//         public BinaryNode right;
//         public int depth;
//         public bool isLeaf;
//         public int objectSplitDepth;
//
//         public int subTreeTrianglesQty;
//         public List<int> triangleIndices;
//
//         public BinaryNode(BoundsBox bounds, int trianglesCount)
//         {
//             this.bounds = bounds;
//             left = null;
//             right = null;
//             depth = 0;
//             triangleIndices = new List<int>();
//             
//             for(int i = 0; i < trianglesCount; i++)
//                 triangleIndices.Add(i);
//
//             subTreeTrianglesQty = triangleIndices.Count;
//             isLeaf = true;
//         }
//         
//         public void GetAllNodes(List<BinaryNode> nodes, in RenderTriangle[] allTriangles)
//         {
//             nodes.Add(this);
//             
//             if (!isLeaf)
//             {
//                 left.GetAllNodes(nodes, allTriangles);
//                 right.GetAllNodes(nodes, allTriangles);
//             }
//         }
//     }
// }