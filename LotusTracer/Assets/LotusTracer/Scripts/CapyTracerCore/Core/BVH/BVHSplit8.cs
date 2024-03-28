using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public static class BVHSplit8
    {
        public static HeapWideNode GetTreeRoot( int maxNodeTriangles, BoundsBox sceneBounds, List<GeoBox> allGeoBounds)
        {
            HeapWideNode rootNode = new HeapWideNode();
            rootNode.children = new List<HeapWideNode>();
            rootNode.geoBoxes = new List<int>();
            rootNode.bounds = BoundsBox.AS_SHRINK;
            
            for(int bIndex = 0; bIndex < allGeoBounds.Count; bIndex++)
            {
                rootNode.geoBoxes.Add(bIndex);
                rootNode.bounds.ExpandWithBounds(allGeoBounds[bIndex].bounds);
            }
            
            Stack<HeapWideNode> stackNodes = new Stack<HeapWideNode>();
            stackNodes.Push(rootNode);

            bool useThreads = true;
            
            while (stackNodes.Count > 0)
            {
                List<HeapWideNode> currentNodes = new List<HeapWideNode>();
                int qtyToPop = stackNodes.Count;

                List<Task> splitTasks = new List<Task>();
                
                for(int i = 0; i < qtyToPop; i++)
                {
                    HeapWideNode thisNode = stackNodes.Pop();
                    currentNodes.Add(thisNode);
                    
                    // avoid to create a task and run it if the node is a terminal leaf node
                    if (thisNode.isLeaf)
                    {
                        continue;
                    }
                    
                    if(useThreads)
                        splitTasks.Add( Task.Run(() => SplitDepthNode(maxNodeTriangles, thisNode, allGeoBounds)) );
                    else 
                        SplitDepthNode(maxNodeTriangles, thisNode, allGeoBounds);
                }

                if(useThreads && splitTasks.Count > 0)
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

            return rootNode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SplitDepthNode(int maxNodeTriangles, HeapWideNode currentNode,  List<GeoBox> allGeoBounds)
        {
            List<HeapWideNode> newNodes = new List<HeapWideNode>();
            newNodes.Add(currentNode);
            List<HeapWideNode> tempNodes = new List<HeapWideNode>(newNodes);
        
            // 3 splits - 8 children
            for (int i = 0; i < 3; i++)
            {
                tempNodes.Clear();
                tempNodes.AddRange(newNodes);
                newNodes.Clear();
            
                foreach (var tempNode in tempNodes)
                {
                    if (tempNode.geoBoxes.Count <= maxNodeTriangles)
                    {
                        newNodes.Add(tempNode);
                        continue;
                    }
                    
                    float3 minCentroid = F3.INFINITY; 
                    float3 maxCentroid = F3.INFINITY_INV;
            
                    foreach (var bIndex in tempNode.geoBoxes)
                    {
                        minCentroid = math.min(minCentroid, allGeoBounds[bIndex].tCentroid);
                        maxCentroid = math.max(maxCentroid, allGeoBounds[bIndex].tCentroid);
                    }
                    
                    // the total volume surface area of this node is used in the sah to calculate the cost
                    // of the candidate partitions
                    float volumeSurfaceArea = (maxCentroid[0] - minCentroid[0]) * 
                                              (maxCentroid[1] - minCentroid[1]) * 
                                              (maxCentroid[2] - minCentroid[2]);

                    volumeSurfaceArea = math.max(volumeSurfaceArea, 0.001f);
                    
                    (int , float) splitInfo = GetAxisSplitScore(minCentroid, maxCentroid, volumeSurfaceArea, allGeoBounds, tempNode.geoBoxes);

                    if (splitInfo.Item1 < 0 || splitInfo.Item1 > 2)
                    {
                        Debug.LogError($"no axis selected? allBoxes: {tempNode.geoBoxes.Count}, best score: {splitInfo.Item2}");
                        continue;
                    }

                    int splitAxis = splitInfo.Item1;
                    float splitPos = math.lerp(minCentroid[splitAxis], maxCentroid[splitAxis], splitInfo.Item2);
                    
                    BoundsBox boundsA = tempNode.bounds;
                    BoundsBox boundsB = tempNode.bounds;
                    boundsA.max[splitAxis] = splitPos;
                    boundsB.min[splitAxis] = splitPos;
                    
                    HeapWideNode nodeA = new HeapWideNode();
                    HeapWideNode nodeB = new HeapWideNode();
                    nodeA.bounds = BoundsBox.AS_SHRINK;
                    nodeB.bounds = BoundsBox.AS_SHRINK;
                
                    foreach (var bIndex in tempNode.geoBoxes)
                    {
                        if (allGeoBounds[bIndex].tCentroid[splitAxis] < boundsA.max[splitAxis])
                        {
                            nodeA.geoBoxes.Add(bIndex);
                            nodeA.bounds.ExpandWithBounds(allGeoBounds[bIndex].bounds);
                        }
                        else
                        {
                            nodeB.geoBoxes.Add(bIndex);
                            nodeB.bounds.ExpandWithBounds(allGeoBounds[bIndex].bounds);

                        }
                    }
                    
                    newNodes.Add(nodeA);
                    newNodes.Add(nodeB);
                }
            }
            
            newNodes.RemoveAll(c => c.geoBoxes.Count == 0);
            if (newNodes.Count <= 1)
            {
                currentNode.isLeaf = true;
                return;
            }
            
            currentNode.children = new List<HeapWideNode>();
            
            foreach (var newNode in newNodes)
            {
                newNode.depth = currentNode.depth + 1;
                newNode.isLeaf = newNode.geoBoxes.Count <= maxNodeTriangles;
                
                currentNode.children.Add(newNode);
            }

            currentNode.geoBoxes.Clear();
            currentNode.isLeaf = false;
        }

        // splitting by using SAH: surface area heuristic
        // this means that based on the existing triangleIndices in the volume, you consider the area of the triangleIndices
        // on each axis and their possible splits, and heuristically come up with a value of what axis and what
        // split would mean better distribution of ray hits.
        // (int, float) = (bestAxis, positionInAxis)   score is the score for this axis, with the bestRatio (at what position of the axis should be split)
        // then outside this function, you check what axis had the best score, and use that one, with the given split position (the ratio)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int, float) GetAxisSplitScore(in float3 minCentroid, in float3 maxCentroid, float volumeSA, List<GeoBox> allBoxes, in List<int> bIndices)
        {
            float bestScore = Mathf.Infinity;
            float bestRatio = 0f;
            int bestAxis = -1;
            int qtySplits = 5;

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
                    
                    for(int ix = 0; ix < bIndices.Count; ix++)
                    {
                        
                        float3 triCenter = allBoxes[bIndices[ix]].tCentroid;
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