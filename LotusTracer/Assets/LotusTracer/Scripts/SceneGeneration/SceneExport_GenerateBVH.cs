using System.Collections.Generic;
using CapyTracerCore.Core;

public static class SceneExport_GenerateBVH
{
    public static void Export(List<BVHNode> sortedHeapNodes, SerializedScene scene)
    {
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

            uint nodeData = 0;
            nodeData = heapNode.isLeaf ? nodeData | 0x80000000 : nodeData & 0x7FFFFFFF;

            if (!heapNode.isLeaf)
            {
                for (byte ch = 0; ch < heapNode.children.Count; ch++)
                {
                    if (!heapNode.children[ch].isLeaf ||
                        (heapNode.children[ch].isLeaf && heapNode.children[ch].triangleIndices.Count > 0))
                        nodeData = (uint)(nodeData | (0x1 << (30 - ch)));
                }
            }
            //
            // string debst = "";
            // for (int b = 0; b < 32; b++)
            //     debst += (nodeData >> (31 - b)) & 0x1;
            //
            // Debug.LogError(debst);

            StackBVH4Node stackNode = new StackBVH4Node
            {
                data = nodeData,
                startIndex = heapNode.isLeaf ? ti : heapNode.firstChildIndex,
                qtyTriangles = qtyTriangles,
                bounds = heapNode.bounds
            };

            outNodes[i] = stackNode;

            ti += qtyTriangles;
        }

        scene.qtyBVHNodes = outNodes.Length;
        scene.bvhNodes = outNodes;

        var sortedTrianglesArray = new FastTriangle[sortedTriangles.Count];
        
        List<int> emissiveTriangleIndices = new List<int>();

        for (int t = 0; t < sortedTriangles.Count; t++)
        {
            sortedTrianglesArray[t] = scene.triangles[sortedTriangles[t]];
            
            if(scene.materials[ sortedTrianglesArray[t].materialIndex ].emissiveIntensity > 0)
                emissiveTriangleIndices.Add(t);
        }

        // annoying but it has to have at least 1 to avoid zero count compute buffers
        if(emissiveTriangleIndices.Count == 0)
            emissiveTriangleIndices.Add(-1);
        
        scene.triangles = sortedTrianglesArray;
    }

    public static List<BVHNode> GenerateHeapNodes(SerializedScene scene, int bvhMaxDepth, int bvhMaxNodeTriangles)
    {
        // first I create a BVH tree using a BVHNode which is a heap based implementation
        // each node has pointers to the children. The thing is that to be used in the Burst jobs
        // I need to pass this to a value type array. This is common too with GPU data structures,
        // you'll find that's common to do this same process to pass kd trees to the GPU.
        BVHNode heapNodeRoot = new BVHNode(new BoundsBox(scene.boundMin, scene.boundMax), scene.qtyTriangles);
        BVHSplit.SplitNode(heapNodeRoot, bvhMaxDepth, bvhMaxNodeTriangles, scene.triangles);

        List<BVHNode> heapNodes = new List<BVHNode>();
        
        // important! this GetAllNodesSorted algorithm has to sort properly the nodes while gathering them
        // which is in a way that all children of a node will always be together, so the traversal logic can
        // look for the first index and then iterate by 4
        heapNodeRoot.GetAllNodesSorted(heapNodes, true);

        return heapNodes;
    }
}