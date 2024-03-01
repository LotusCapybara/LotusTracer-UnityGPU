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

                    float3 size = thisNode.bounds.GetSize();
                    float maxSize = math.max(size.x, math.max(size.y, size.z));
                    
                    // avoid pretty small partitions
                    
                    // avoid to create a task and run it if the node is a terminal leaf node
                    if (maxSize <= 0.1f || thisNode.triangleIndices.Count <= maxNodeTriangles * 5 || thisNode.depth >= maxDepth)
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
                    
                    (int, float) splitInfo = GetAxisSplitScore(minCentroid, maxCentroid, triangles, tempNode.triangleIndices);

                    float splitPos = minCentroid[splitInfo.Item1] +
                                     (maxCentroid[splitInfo.Item1] - minCentroid[splitInfo.Item1]) * 0.5f;
                    
                    
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

                    // if (trianglesA.Count == 0 || trianglesB.Count == 0)
                    // {
                    //     string lala = "";
                    //     lala += "SPLIT AXIS: " + splitInfo.Item1 + " ratio: " + splitInfo.Item2 + boundsA.max.ToString() +  "\n";
                    //     
                    //     foreach (var tempNodeTriangle in tempNode.triangleIndices)
                    //     {
                    //         lala += tempNodeTriangle.centerPos[splitInfo.Item1] + "\n";
                    //     }
                    //     Debug.LogError(lala);
                    // }
                        

                    nodeA.splitAxis = splitInfo.Item1;
                    nodeB.splitAxis = splitInfo.Item1;
                    // nodeA.parent = tempNode;
                    // nodeB.parent = tempNode;
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
        private static (int, float) GetAxisSplitScore(in float3 minCentroid, in float3 maxCentroid, in RenderTriangle[] allTriangles, in List<int> tIndices)
        {
            float bestScore = Mathf.Infinity;
            float bestRatio = 0f;
            float bestPosInAxis = 0;
            int bestAxis = -1;
            int qtySplits = 10;
            float biggestAxisSize = -1;
            int biggestAxis = -1;
            
            for (int axis = 0; axis < 3; axis++)
            {
                float axisSize = maxCentroid[axis] - minCentroid[axis];
                
                if (axisSize > biggestAxisSize)
                    biggestAxis = axis;
                
                for (int i = 1; i < qtySplits; i++)
                {
                    float p1 = (1f / qtySplits) * i;
                    float p2 = 1f - p1;

                    float centerPos = minCentroid[axis] + p1;

                    float qty1 = 0;
                    float qty2 = 0;

                    for(int ix = 0; ix < tIndices.Count; ix++)
                    {
                        if (allTriangles[tIndices[ix]].centerPos[axis] <= centerPos)
                        {
                            qty1++;
                        }
                        else
                        {
                            qty2++;
                        }
                    }

                    float score = 1f + p1 * 1.5f * qty1 + p2 * 1.5f * qty2;

                    if (score < bestScore && (qty1 > 0 && qty2 > 0))
                    {
                        bestScore = score;
                        bestRatio = p1;
                        bestAxis = axis;
                    }
                }
            }

            if (bestAxis < 0)
                bestAxis = biggestAxis;

            return (bestAxis, bestRatio);
        }
    }
}