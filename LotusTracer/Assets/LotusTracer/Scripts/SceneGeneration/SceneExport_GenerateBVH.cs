using System.Collections.Generic;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public static class SceneExport_GenerateBVH
{
    public static void Export(List<BVHNode> sortedHeapNodes)
    {
        SerializedScene_Data sceneData = SceneExporter.s_sceneData;
        SerializedScene_Geometry sceneGeom = SceneExporter.s_sceneGeom;
        
        // the next steps create the NativeArray of value type stack nodes.
        // also, it sorts the NativeArray of triangleIndices to be ordered in the same way their indices
        // are sorted inside the nodes. Basically, once this finishes, the array of triangleIndices will have
        // "sections" of triangleIndices and each section contains the triangleIndices of a single node.
        // This favor locality and speeds up the process thanks to the CPU cache mechanisms.
        // Triangle sare going to be utilized by algorithms that go through them (check code like
        // StackBVHNode.GetBounceHit for an example. If you keep the RenderTriangle data compact, the cache
        // line would include multiple triangleIndices at once, and since you are linearly iterating it, the chances
        // of the next triangle of being already in the cache increase, compared with having a non sorted array
        StackBVH4Node[] outNodes = new StackBVH4Node[sortedHeapNodes.Count];
        
        List<int> sortedTriangles = new List<int>();

        int ti = 0;

        var sceneBounds = sortedHeapNodes[0].bounds;
        var sceneExtends = sortedHeapNodes[0].bounds.GetSize();

        for (int i = 0; i < sortedHeapNodes.Count; i++)
        {
            BVHNode heapNode = sortedHeapNodes[i];

            int qtyTriangles = heapNode.isLeaf ? heapNode.triangleIndices.Count : 0;

            for (int t = 0; t < qtyTriangles; t++)
            {
                int originalTIndex = heapNode.triangleIndices[t];
                sortedTriangles.Add(originalTIndex);
            }

            
            
            // node data bits (from backwards)
            // 0: is this node leaf?
            // 1-8: is the children at index traversable?
            
            uint nodeData = (uint) (heapNode.isLeaf ? 1 : 0);
            
            BoundsBox[] childrenBounds = new BoundsBox[8];

            if (!heapNode.isLeaf)
            {
                for (byte ch = 0; ch < heapNode.children.Count; ch++)
                {
                    bool isTraversable = (!heapNode.isLeaf && heapNode.children != null && heapNode.children.Count > 0)
                                         || (heapNode.isLeaf && heapNode.triangleIndices.Count > 0);
                    
                    if(isTraversable)
                        nodeData |= (uint)(0x1 << (ch + 1));

                    childrenBounds[ch] = heapNode.children[ch].bounds;
                }
            }
            
            StackBVH4Node stackNode = new StackBVH4Node
            {
                data = nodeData,
                startIndex = heapNode.isLeaf ? ti : heapNode.firstChildIndex,
                qtyTriangles = qtyTriangles,
                boundsMin = heapNode.bounds.min,
                extends = heapNode.bounds.GetSize()
            };
    
            if (heapNode.children == null || heapNode.children.Count <= 0)
            {
                stackNode.bb0 = new uint2();
                stackNode.bb1 = new uint2();
                stackNode.bb2 = new uint2();
                stackNode.bb3 = new uint2();
                stackNode.bb4 = new uint2();
                stackNode.bb5 = new uint2();
                stackNode.bb6 = new uint2();
                stackNode.bb7 = new uint2();
            }
            else
            {
                heapNode.bounds = new BoundsBox(
                    heapNode.bounds.min + (F3.ONE * -0.001f),
                    heapNode.bounds.max + (F3.ONE * 0.001f)); 
                
                StackBVH4Node tempStackNode = BVHUtils.Compress(childrenBounds, heapNode.bounds);
                
                float3 parentMin = heapNode.bounds.min;
                float3 parentSize = heapNode.bounds.GetSize();
                stackNode.boundsMin = parentMin;
                stackNode.extends = parentSize;
                stackNode.bb0 = tempStackNode.bb0;
                stackNode.bb1 = tempStackNode.bb1;
                stackNode.bb2 = tempStackNode.bb2;
                stackNode.bb3 = tempStackNode.bb3;
                stackNode.bb4 = tempStackNode.bb4;
                stackNode.bb5 = tempStackNode.bb5;
                stackNode.bb6 = tempStackNode.bb6;
                stackNode.bb7 = tempStackNode.bb7;

                stackNode.precisionLoss = 0f;

                // I know... this is quite expensive, but I can give me an idea of the precision loss like this
                // I need to either improve base precision or make this part faster
                BoundsBox[] decompressedBounds = BVHUtils.Decompress(stackNode);

                for (int ch = 0; ch < 8; ch++)
                {
                    stackNode.precisionLoss = math.max(stackNode.precisionLoss, math.distance(heapNode.children[ch].bounds.min, decompressedBounds[ch].min));
                    stackNode.precisionLoss = math.max(stackNode.precisionLoss, math.distance(heapNode.children[ch].bounds.max, decompressedBounds[ch].max));
                }
            }

            outNodes[i] = stackNode;

            ti += qtyTriangles;
        }

        sceneGeom.boundMin = sceneBounds.min;
        sceneGeom.boundMax = sceneBounds.max;

        sceneGeom.qtyBVHNodes = outNodes.Length;
        sceneGeom.bvhNodes = outNodes;

        var sortedTrianglesArray = new RenderTriangle[sortedTriangles.Count];
        
        List<int> emissiveTriangleIndices = new List<int>();

        for (int t = 0; t < sortedTriangles.Count; t++)
        {
            sortedTrianglesArray[t] = SceneExport_GatherTriangles.s_gatheredTriangles[sortedTriangles[t]];
            
            if(sceneData.materials[ sortedTrianglesArray[t].materialIndex ].emissiveIntensity > 0)
                emissiveTriangleIndices.Add(t);
        }

        // annoying but it has to have at least 1 to avoid zero count compute buffers
        if(emissiveTriangleIndices.Count == 0)
            emissiveTriangleIndices.Add(-1);
        
        SceneExport_GatherTriangles.s_gatheredTriangles = sortedTrianglesArray;
    }

    public static List<BVHNode> GenerateHeapNodes(int bvhMaxDepth, int bvhMaxNodeTriangles)
    {
        SerializedScene_Geometry sceneGeom = SceneExporter.s_sceneGeom;
        
        // first I create a BVH tree using a BVHNode which is a heap based implementation
        // each node has pointers to the children. The thing is that to be used in the Burst jobs
        // I need to pass this to a value type array. This is common too with GPU data structures,
        // you'll find that's common to do this same process to pass kd trees to the GPU.
        BVHNode heapNodeRoot = new BVHNode(new BoundsBox(sceneGeom.boundMin, sceneGeom.boundMax), sceneGeom.qtyTriangles);
        BVHSplit.SplitNode(heapNodeRoot, bvhMaxDepth, bvhMaxNodeTriangles, SceneExport_GatherTriangles.s_gatheredTriangles);

        List<BVHNode> heapNodes = new List<BVHNode>();
        
        // important! this GetAllNodesSorted algorithm has to sort properly the nodes while gathering them
        // which is in a way that all children of a node will always be together, so the traversal logic can
        // look for the first index and then iterate by 4
        heapNodeRoot.GetAllNodesSorted(heapNodes, true);

        return heapNodes;
    }
}