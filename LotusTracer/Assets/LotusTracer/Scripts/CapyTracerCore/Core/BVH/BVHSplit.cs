// using System;
// using System.Collections.Generic;
// using System.Runtime.CompilerServices;
// using System.Threading.Tasks;
// using Unity.Mathematics;
// using UnityEngine;
//
// namespace CapyTracerCore.Core
// {
//     public static class BVHSplit
//     {
//         public static void SplitNode(BinaryNode node, RenderTriangle[] triangles)
//         {
//             node.isLeaf = triangles.Length <= 1;
//             
//             Stack<BinaryNode> stackNodes = new Stack<BinaryNode>();
//             stackNodes.Push(node);
//
//             bool useThreads = true;
//             
//             while (stackNodes.Count > 0)
//             {
//                 List<BinaryNode> currentNodes = new List<BinaryNode>();
//                 int qtyToPop = stackNodes.Count;
//
//                 List<Task> splitTasks = new List<Task>();
//                 
//                 for(int i = 0; i < qtyToPop; i++)
//                 {
//                     BinaryNode thisNode = stackNodes.Pop();
//
//                     if (!thisNode.isLeaf)
//                     {
//                         currentNodes.Add(thisNode);
//                     
//                         if(useThreads)
//                             splitTasks.Add( Task.Run(() => SplitDepthNode(thisNode, triangles)) );   
//                         else
//                             SplitDepthNode(thisNode, triangles);
//                     }
//                 }
//
//                 if(useThreads && splitTasks.Count > 0)
//                     Task.WaitAll(splitTasks.ToArray());
//                 
//                 foreach (var currentNode in currentNodes)
//                 {
//                     if (!currentNode.isLeaf)
//                     {
//                         if(!currentNode.left.isLeaf)
//                             stackNodes.Push(currentNode.left);
//                         
//                         if(!currentNode.right.isLeaf)
//                             stackNodes.Push(currentNode.right);
//                     }
//                 }
//             }
//         }
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private static void SplitDepthNode(BinaryNode currentNode, RenderTriangle[] triangles)
//         {
//             float3 minCentroid = F3.INFINITY; 
//             float3 maxCentroid = F3.INFINITY_INV;
//
//             if (currentNode.triangleIndices.Count > 1)
//             {
//                 foreach (var tIndex in currentNode.triangleIndices)
//                 {
//                     minCentroid = math.min(minCentroid, triangles[tIndex].centerPos);
//                     maxCentroid = math.max(maxCentroid, triangles[tIndex].centerPos);
//                 }    
//             }
//             else
//             {
//                 minCentroid = currentNode.bounds.min;
//                 maxCentroid = currentNode.bounds.max;
//             }
//                     
//             // the total volume surface area of this node is used in the sah to calculate the cost
//             // of the candidate partitions
//             float volumeSurfaceArea = (maxCentroid[0] - minCentroid[0]) * 
//                                       (maxCentroid[1] - minCentroid[1]) * 
//                                       (maxCentroid[2] - minCentroid[2]);
//
//             volumeSurfaceArea = math.max(volumeSurfaceArea, 0.001f);
//             
//             (int, float) splitInfo = GetAxisSplitScore(minCentroid, maxCentroid, volumeSurfaceArea, triangles, currentNode.triangleIndices);
//
//             if (splitInfo.Item1 < 0 || splitInfo.Item1 > 2)
//             {
//                 if (currentNode.triangleIndices.Count > 1)
//                     throw new Exception($"no axis selected? triangles: {currentNode.triangleIndices.Count}, best score: {splitInfo.Item2}");
//                 
//                 currentNode.isLeaf = true;
//                 return;
//             }
//                 
//             
//             if(splitInfo.Item2 <= 0 || splitInfo.Item2 >= 1)
//                 throw new Exception($"not properly split? {currentNode.triangleIndices.Count}, best score: {splitInfo.Item2}");
//             
//             if(currentNode.triangleIndices.Count == 2 && Math.Abs(splitInfo.Item2 - 0.5f) > 0.000001f)
//                 throw new Exception($"malformed data?");
//
//             int splitAxis = splitInfo.Item1;
//             float splitPos = math.lerp(minCentroid[splitInfo.Item1], maxCentroid[splitInfo.Item1], splitInfo.Item2);
//             
//             
//             BinaryNode nodeA = new BinaryNode(BoundsBox.AS_SHRINK, 0);
//             BinaryNode nodeB = new BinaryNode(BoundsBox.AS_SHRINK, 0);
//
//             BoundsBox boundsA = currentNode.bounds;
//             BoundsBox boundsB = currentNode.bounds;
//             boundsA.max[splitInfo.Item1] = splitPos;
//             boundsB.min[splitInfo.Item1] = splitPos;
//        
//             BoundsBox tightBoundsA = BoundsBox.AS_SHRINK;
//             BoundsBox tightBoundsB = BoundsBox.AS_SHRINK;
//
//             if (currentNode.triangleIndices.Count == 1)
//             {
//                 int tIndex = currentNode.triangleIndices[0];
//                 
//                 nodeA.triangleIndices.Add(tIndex);
//                 nodeB.triangleIndices.Add(tIndex);
//
//                 float3[] vertices = new float3[3];
//                 vertices[0] = triangles[tIndex].posA;
//                 vertices[1] = triangles[tIndex].posA + triangles[tIndex].p0p1;
//                 vertices[2] = triangles[tIndex].posA + triangles[tIndex].p0p2;
//                 
//                 int ortAxis = splitAxis == 0 ? 2 : (splitAxis == 1 ? 0 : 1);
//                 float3 planeNormal = float3.zero;
//                 planeNormal[ortAxis] = 1.0f;
//
//                 Plane splitPlane = new Plane(planeNormal, splitPos);
//
//                 float3[] midIntersects;
//                 HitsTriangleWithPlane(splitAxis, vertices, splitPlane, out midIntersects);
//
//                 for (int i = 0; i < 2; i++)
//                 {
//                     tightBoundsA.ExpandWithPoint(midIntersects[i]);
//                     tightBoundsB.ExpandWithPoint(midIntersects[i]);
//                 }
//
//                 splitPlane.distance = boundsA.min[splitAxis];
//                 float3[] leftIntersects;
//                 int leftQty = HitsTriangleWithPlane(splitAxis, vertices, splitPlane, out leftIntersects);
//                 
//                 for (int i = 0; i < leftQty; i++)
//                 {
//                     tightBoundsA.ExpandWithPoint(leftIntersects[i]);
//                 }
//                 
//                 splitPlane.distance = boundsB.max[splitAxis];
//                 float3[] rightIntersects;
//                 int rightQty = HitsTriangleWithPlane(splitAxis, vertices, splitPlane, out rightIntersects);
//                 
//                 for (int i = 0; i < rightQty; i++)
//                 {
//                     tightBoundsB.ExpandWithPoint(rightIntersects[i]);
//                 }
//                 
//                 nodeA.objectSplitDepth = currentNode.objectSplitDepth + 1;
//                 nodeB.objectSplitDepth = currentNode.objectSplitDepth + 1;
//                 nodeA.bounds = tightBoundsA;
//                 nodeB.bounds = tightBoundsB; 
//             }
//             else
//             {
//                 
//                 foreach (var tIndex in currentNode.triangleIndices)
//                 {
//                     if (triangles[tIndex].centerPos[splitInfo.Item1] < splitPos)
//                     {
//                         nodeA.triangleIndices.Add(tIndex);
//                         tightBoundsA.ExpandWithBounds(triangles[tIndex].bounds);
//                     }
//                 
//                     else
//                     {
//                         nodeB.triangleIndices.Add(tIndex);
//                         tightBoundsB.ExpandWithBounds(triangles[tIndex].bounds);
//                     }
//                 }
//                     
//                 nodeA.bounds = tightBoundsA;
//                 nodeB.bounds = tightBoundsB;
//             }
//
//             SetNodeIfLeaf(nodeA);
//             SetNodeIfLeaf(nodeB);
//                    
//             
//             currentNode.left = nodeA;
//             currentNode.right = nodeB;
//             currentNode.left.depth = currentNode.depth + 1;
//             currentNode.right.depth = currentNode.depth + 1;
//             currentNode.subTreeTrianglesQty = currentNode.triangleIndices.Count;
//             currentNode.isLeaf = false;
//
//             if (currentNode.triangleIndices.Count == 2)
//             {
//                 if (currentNode.left.triangleIndices.Count > 1 || currentNode.right.triangleIndices.Count > 1)
//                 {
//                     currentNode.left.triangleIndices.Clear();
//                     currentNode.right.triangleIndices.Clear();
//                     
//                     currentNode.left.triangleIndices.Add(currentNode.triangleIndices[0]);
//                     currentNode.right.triangleIndices.Add(currentNode.triangleIndices[1]);
//                     // throw new Exception("bad-formation in split generation");
//                 }
//             }
//             
//             currentNode.triangleIndices.Clear();
//         }
//
//         private static void SetNodeIfLeaf(BinaryNode node)
//         {
//             if (node.triangleIndices.Count > 1)
//             {
//                 node.isLeaf = false;
//                 return;
//             }
//
//             if (node.triangleIndices.Count == 0)
//             {
//                 node.isLeaf = true;
//                 return;
//             }
//
//             // node.isLeaf = true;
//             // return;
//             
//             // the following part results in the same triangle being added to multiple children and having
//             // more nodes, but it also means compacting the total size. 
//             float3 boundSize = node.bounds.GetSize();
//             float maxSize = math.max(boundSize.x, math.max(boundSize.y, boundSize.z));
//             node.isLeaf = maxSize < 0.1f || node.objectSplitDepth >= 3;
//         }
//
//         // splitting by using SAH: surface area heuristic
//         // this means that based on the existing triangleIndices in the volume, you consider the area of the triangleIndices
//         // on each axis and their possible splits, and heuristically come up with a value of what axis and what
//         // split would mean better distribution of ray hits.
//         // (int, float) = (bestAxis, positionInAxis)   score is the score for this axis, with the bestRatio (at what position of the axis should be split)
//         // then outside this function, you check what axis had the best score, and use that one, with the given split position (the ratio)
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private static (int, float) GetAxisSplitScore(in float3 minCentroid, in float3 maxCentroid, float volumeSA, in RenderTriangle[] allTriangles, in List<int> tIndices)
//         {
//             float bestScore = Mathf.Infinity;
//             float bestRatio = 0f;
//             int bestAxis = -1;
//
//             if (tIndices.Count <= 0)
//                 throw new Exception("wtf is wrong with you bro?");
//             
//             // for the case in which we divide either 2 triangles or a single triangle
//             // we divide by the widest axis instead of using SAH
//             if (tIndices.Count == 2 || tIndices.Count == 1)
//             {
//                 float maxAxisSize = -1f;
//                 
//                 for (int axis = 0; axis < 3; axis++)
//                 {
//                     float axisSize = maxCentroid[axis] - minCentroid[axis];
//
//                     if (axisSize > maxAxisSize)
//                     {
//                         maxAxisSize = axisSize;
//                         bestAxis = axis;
//                     }
//                 }
//
//                 if (bestAxis >= 0)
//                     return (bestAxis, 0.5f);
//
//                 throw new Exception("no axis could be selected. maybe it's worth merging these vertices?");
//             }
//             
//             int qtySplits = math.min(10,  10 + ( allTriangles.Length / 1000));
//
//             BoundsBox bbA;
//             BoundsBox bbB;
//
//             float[] scores = new float[3];
//             
//             for (int axis = 0; axis < 3; axis++)
//             {
//                 float axisSize = maxCentroid[axis] - minCentroid[axis];
//
//                 if (axisSize < 0.0001f)
//                     continue;
//                 
//                 for (int i = 1; i < qtySplits ; i++)
//                 {
//                     float splitRatio = ((float)i / qtySplits);
//                     float centerPos = minCentroid[axis] + axisSize * splitRatio;
//
//                     float qty1 = 0;
//                     float qty2 = 0;
//                     
//                     bbA = BoundsBox.AS_SHRINK;
//                     bbB = BoundsBox.AS_SHRINK;
//                     
//                     for(int ix = 0; ix < tIndices.Count; ix++)
//                     {
//                         
//                         float3 triCenter = allTriangles[tIndices[ix]].centerPos;
//                         if (triCenter[axis] <= centerPos)
//                         {
//                             qty1++;
//                             bbA.ExpandWithPoint(triCenter);
//                         }
//                         else
//                         {
//                             qty2++;
//                             bbB.ExpandWithPoint(triCenter);
//                         }
//                     }
//
//                     float areaA = (bbA.max[0] - bbA.min[0]) * 
//                                   (bbA.max[1] - bbA.min[1]) * 
//                                   (bbA.max[2] - bbA.min[2]);
//                     
//                     float areaB = (bbB.max[0] - bbB.min[0]) * 
//                                   (bbB.max[1] - bbB.min[1]) * 
//                                   (bbB.max[2] - bbB.min[2]);
//
//                     if (float.IsInfinity(areaA))
//                         areaA = 0;
//                     
//                     if (float.IsInfinity(areaB))
//                         areaB = 0;
//                     
//                     float ct = 1f;
//                     float ci = 1f;
//                     float score = ct + ci * qty1 * (areaA/volumeSA) + ci * qty2 * (areaB/volumeSA) ;
//                     if(float.IsNaN(score))
//                         throw new Exception($"nan score. volumeSA: { volumeSA }. aA {areaA} aB {areaB}");
//                     
//                     scores[axis] = score;
//                     
//                     if (score < bestScore)
//                     {
//                         bestScore = score;
//                         bestRatio = splitRatio;
//                         bestAxis = axis;
//                     }
//                 }
//             }
//             
//             return (bestAxis, bestRatio);
//         }
//
//         private static int HitsTriangleWithPlane(int axis, in float3[] vertices, in Plane plane, out float3[] intersects)
//         {
//             int ortAxis = axis == 0 ? 2 : (axis == 1 ? 0 : 1);
//             
//             float3[] intersections = new float3[2];
//             bool[] directions = new bool[4];
//             int qtyIntersections = 0;
//
//             // Calculate distances for each vertex
//             float dA = vertices[0][axis] - plane.distance;
//             float dB = vertices[1][axis] - plane.distance;
//             float dC = vertices[2][axis] - plane.distance;
//
//             // Check if there is an intersection between each edge and the plane
//             if (dA * dB < 0) // A and B are on opposite sides
//             {
//                 float t = dA / (dA - dB);
//                 float3 intersection = vertices[0] + t * (vertices[1] - vertices[0]);
//
//                 bool aIsLeft = vertices[0][ortAxis] < vertices[1][ortAxis];
//                 bool aIsUp = vertices[0][axis] > vertices[1][axis];
//
//                 if (aIsLeft)
//                     directions[qtyIntersections] = !aIsUp;
//                 else
//                     directions[qtyIntersections] = aIsUp;
//                 
//                 intersections[qtyIntersections++] = intersection;
//             }
//
//             if (dA * dC < 0) // A and C are on opposite sides
//             {
//                 float t = dA / (dA - dC);
//                 float3 intersection = vertices[0] + t * (vertices[2] - vertices[0]);
//                 
//                 bool aIsLeft = vertices[0][ortAxis] < vertices[2][ortAxis];
//                 bool aIsUp = vertices[0][axis] > vertices[2][axis];
//
//                 if (aIsLeft)
//                     directions[qtyIntersections] = !aIsUp;
//                 else
//                     directions[qtyIntersections] = aIsUp;
//                 
//                 intersections[qtyIntersections++] = intersection;
//             }
//
//             if (dB * dC < 0) // B and C are on opposite sides
//             {
//                 float t = dB / (dB - dC);
//                 float3 intersection = vertices[1] + t * (vertices[2] - vertices[1]);
//                 
//                 bool bIsLeft = vertices[1][ortAxis] < vertices[2][ortAxis];
//                 bool bIsUp = vertices[1][axis] > vertices[2][axis];
//
//                 if (bIsLeft)
//                     directions[qtyIntersections] = !bIsUp;
//                 else
//                     directions[qtyIntersections] = bIsUp;
//                 
//                 intersections[qtyIntersections++] = intersection;
//             }
//
//             // Handle cases where triangle vertices lie exactly on the plane
//             if (qtyIntersections < 2 && math.abs(dA) < 0.001f) 
//                 intersections[qtyIntersections++] = vertices[0];
//             if (qtyIntersections < 2 && math.abs(dB) < 0.001f) 
//                 intersections[qtyIntersections++] = vertices[1];
//             if (qtyIntersections < 2 && math.abs(dC) < 0.001f) 
//                 intersections[qtyIntersections++] = vertices[2];
//
//             intersects = intersections;
//             
//             return qtyIntersections;
//         }
//     }
// }