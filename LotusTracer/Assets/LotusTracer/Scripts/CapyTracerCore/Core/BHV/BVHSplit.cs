using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public static class BVHSplit
    {
        public static void SplitNode(BVHNode node, int maxDepth, int maxNodeTriangles, RenderTriangle[] triangles)
        {
            Stack<BVHNode> stackNodes = new Stack<BVHNode>();
            stackNodes.Push(node);

            while (stackNodes.Count > 0)
            {
                List<BVHNode> currentNodes = new List<BVHNode>();
                int qtyToPop = stackNodes.Count;

                List<Task> splitTasks = new List<Task>();
                
                for(int i = 0; i < qtyToPop; i++)
                {
                    BVHNode thisNode = stackNodes.Pop();
                    currentNodes.Add(thisNode);
                    
                    // avoid pretty small partitions

                    bool isLeaf = thisNode.triangleIndices.Count <= maxNodeTriangles ||
                                  thisNode.depth >= maxDepth || thisNode.triangleIndices.Count == 0;
                    
                    // avoid to create a task and run it if the node is a terminal leaf node
                    if (isLeaf)
                    {
                        thisNode.isLeaf = true;
                        thisNode.children = null;
                        continue;
                    }
                    
                    // SplitDepthNode(thisNode, triangles);
                    splitTasks.Add( Task.Run(() => SplitDepthNode(thisNode, triangles)) );
                }

                if(splitTasks.Count > 0)
                    Task.WaitAll(splitTasks.ToArray());
                
                foreach (var currentNode in currentNodes)
                {
                    if (currentNode.children != null)
                    {
                        for(int ch = 0; ch < currentNode.children.Count; ch++)
                            stackNodes.Push(currentNode.children[ch]);
                    }
                }
            }
            
            node.FinishGeneration(triangles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SplitDepthNode(BVHNode currentNode, RenderTriangle[] triangles)
        {
            List<BVHNode> newNodes = new List<BVHNode>();
            newNodes.Add(currentNode);
            List<BVHNode> tempNodes = new List<BVHNode>(newNodes);
        
            for (int i = 0; i < BVHNode.QTY_SPLITS; i++)
            {
                tempNodes.Clear();
                tempNodes.AddRange(newNodes);
                newNodes.Clear();
            
                foreach (var tempNode in tempNodes)
                {
                    float3 minCentroid = F3.INFINITY; 
                    float3 maxCentroid = F3.INFINITY_INV;
            
                    foreach (var tIndex in tempNode.triangleIndices)
                    {
                        minCentroid = math.min(minCentroid, triangles[tIndex].centerPos);
                        maxCentroid = math.max(maxCentroid, triangles[tIndex].centerPos);
                    }
                    
                    // the total volume surface area of this node is used in the sah to calculate the cost
                    // of the candidate partitions
                    float volumeSurfaceArea = (maxCentroid[0] - minCentroid[0]) * 
                                              (maxCentroid[1] - minCentroid[1]) * 
                                              (maxCentroid[2] - minCentroid[2]);

                    (int, float) splitInfo = (1, 0.5f);

                    volumeSurfaceArea = math.max(volumeSurfaceArea, 0.001f);
                    
                    // this only happens if all the triangles have their centroid exactly in the same place
                    // It could happen in some geometries, we can't assume they are well made
                    // I'm not using 0, because very tiny volume surfaces algo mess up the calculation of sah
                    // if (volumeSurfaceArea > 0.0001f)
                    // {
                    //     splitInfo = GetAxisSplitScore(minCentroid, maxCentroid, volumeSurfaceArea, triangles, tempNode.triangleIndices);
                    // }
                    splitInfo = GetAxisSplitScore(minCentroid, maxCentroid, volumeSurfaceArea, triangles, tempNode.triangleIndices);
                    

                    // Debug.Log($"axis :{splitInfo.Item1}   ratio: {splitInfo.Item2}");
                    //
                    if (splitInfo.Item1 < 0 || splitInfo.Item1 > 2)
                    {
                        Debug.LogError($"no axis selected? triangles: {tempNode.triangleIndices.Count}, best score: {splitInfo.Item2}");
                        continue;
                    }

                    float splitPos = math.lerp(minCentroid[splitInfo.Item1], maxCentroid[splitInfo.Item1], splitInfo.Item2);
                    // float splitPos = minCentroid[splitInfo.Item1] +
                    //                  (maxCentroid[splitInfo.Item1] - minCentroid[splitInfo.Item1]) * splitInfo.Item2;
                    
                    // Debug.Log($"axis: {splitInfo.Item1}  pos: {splitInfo.Item2}");
                    
                    BoundsBox boundsA = tempNode.bounds;
                    BoundsBox boundsB = tempNode.bounds;
                    BVHNode nodeA = new BVHNode(boundsA, 0);
                    BVHNode nodeB = new BVHNode(boundsB, 0);

                    boundsA.max[splitInfo.Item1] = splitPos;
                    boundsB.min[splitInfo.Item1] = splitPos;
                
                    foreach (var tIndex in tempNode.triangleIndices)
                    {
                        if (triangles[tIndex].centerPos[splitInfo.Item1] < boundsA.max[splitInfo.Item1])
                            nodeA.triangleIndices.Add(tIndex);
                        else
                            nodeB.triangleIndices.Add(tIndex);
                    }
                    
                    newNodes.Add(nodeA);
                    newNodes.Add(nodeB);
                }
            }

            currentNode.children = newNodes;

            for (int i = 0; i < currentNode.children.Count; i++)
            {
                currentNode.children[i].depth = currentNode.depth + 1;
            }

            currentNode.triangleIndices.Clear();
            currentNode.isLeaf = false;
        }

        // splitting by using SAH: surface area heuristic
        // this means that based on the existing triangleIndices in the volume, you consider the area of the triangleIndices
        // on each axis and their possible splits, and heuristically come up with a value of what axis and what
        // split would mean better distribution of ray hits.
        // (int, float) = (bestAxis, positionInAxis)   score is the score for this axis, with the bestRatio (at what position of the axis should be split)
        // then outside this function, you check what axis had the best score, and use that one, with the given split position (the ratio)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int, float) GetAxisSplitScore(in float3 minCentroid, in float3 maxCentroid, float volumeSA, in RenderTriangle[] allTriangles, in List<int> tIndices)
        {
            float bestScore = Mathf.Infinity;
            float bestRatio = 0f;
            int bestAxis = -1;
            int qtySplits = math.min(10,  10 + ( allTriangles.Length / 1000));

            BoundsBox bbA;
            BoundsBox bbB;

            float[] scores = new float[3];
            
            for (int axis = 0; axis < 3; axis++)
            {
                float axisSize = maxCentroid[axis] - minCentroid[axis];
                
                for (int i = 1; i < qtySplits ; i++)
                {
                    float splitRatio = ((float)i / qtySplits);
                    float centerPos = minCentroid[axis] + axisSize * splitRatio;

                    float qty1 = 0;
                    float qty2 = 0;
                    
                    bbA = BoundsBox.AS_SHRINK;
                    bbB = BoundsBox.AS_SHRINK;
                    
                    for(int ix = 0; ix < tIndices.Count; ix++)
                    {
                        
                        float3 triCenter = allTriangles[tIndices[ix]].centerPos;
                        if (triCenter[axis] <= centerPos)
                        {
                            qty1++;
                            bbA.ExpandWithPoint(triCenter);
                        }
                        else
                        {
                            qty2++;
                            bbB.ExpandWithPoint(triCenter);
                        }
                    }

                    // the other axies that are not the one being evaluated, so we can calculate the
                    // surface area that this axis would intersect
                    int axis1 = axis == 0 ? 1 : 0;
                    int axis2 = axis == 2 ? 1 : 2;

                    float areaA = (bbA.max[axis1] - bbA.min[axis1]) * (bbA.max[axis2] - bbA.min[axis2]);
                    float areaB = (bbB.max[axis1] - bbB.min[axis1]) * (bbB.max[axis2] - bbB.min[axis2]);

                    if (float.IsInfinity(areaA))
                        areaA = 0;
                    
                    if (float.IsInfinity(areaB))
                        areaB = 0;
                    
                    float ct = 1f;
                    float ci = 1f;
                    float score = ct + ci * qty1 * (areaA/volumeSA) + ci * qty2 * (areaB/volumeSA) ;
                    if(float.IsNaN(score))
                        throw new Exception($"nan score. volumeSA: { volumeSA }. aA {areaA} aB {areaB}");
                    
                    scores[axis] = score;
                    
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestRatio = splitRatio;
                        bestAxis = axis;
                    }
                }
            }
            
            return (bestAxis, bestRatio);
        }
    }
}