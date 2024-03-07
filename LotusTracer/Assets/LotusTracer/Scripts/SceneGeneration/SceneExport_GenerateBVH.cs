using System.Collections.Generic;
using CapyTracerCore.Core;
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

        for (int i = 0; i < sortedHeapNodes.Count; i++)
        {
            BVHNode heapNode = sortedHeapNodes[i];

            int qtyTriangles = heapNode.isLeaf ? heapNode.triangleIndices.Count : 0;

            for (int t = 0; t < qtyTriangles; t++)
            {
                int originalTIndex = heapNode.triangleIndices[t];
                sortedTriangles.Add(originalTIndex);
            }

            uint nodeData = (uint) (heapNode.isLeaf ? 1 : 0);

            if (!heapNode.isLeaf)
            {
                for (byte ch = 0; ch < heapNode.children.Count; ch++)
                {
                    if (!heapNode.children[ch].isLeaf ||
                        (heapNode.children[ch].isLeaf && heapNode.children[ch].triangleIndices.Count > 0))
                        nodeData |= (uint)(0x1 << (ch + 1));
                }
            }
            
            StackBVH4Node stackNode = new StackBVH4Node
            {
                data = nodeData,
                startIndex = heapNode.isLeaf ? ti : heapNode.firstChildIndex,
                qtyTriangles = qtyTriangles,
                // depth = heapNode.depth,
                bounds = heapNode.bounds
            };

            outNodes[i] = stackNode;

            ti += qtyTriangles;
        }

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