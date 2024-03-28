using System;
using System.Collections.Generic;
using CapyTracerCore.Core;
using Unity.Mathematics;

public static class SceneExport_GenerateBVH
{
    public static void Export(List<HeapWideNode> sortedWideNodes, List<GeoBox> allGeoBoxes)
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
        StackBVH4Node[] outNodes = new StackBVH4Node[sortedWideNodes.Count];
        
        List<int> sortedTriangles = new List<int>();
        
        int ti = 0;
        
        var sceneBounds = sortedWideNodes[0].bounds;
        
        List<int> nodeSortedTriangles = new List<int>(16);
        
        for (int i = 0; i < sortedWideNodes.Count; i++)
        {
            HeapWideNode hWideNode = sortedWideNodes[i];
            
            if(hWideNode == null || hWideNode.children == null)
                throw new Exception("malformed wide node");
            if (hWideNode.children.Count > 8)
                throw new Exception("malformed wide node. More than 8 children");

            nodeSortedTriangles.Clear();
            
            for (int b = 0; b < hWideNode.geoBoxes.Count; b++)
            {
                int originalTIndex = allGeoBoxes[hWideNode.geoBoxes[b]].triIndex;

                if (!nodeSortedTriangles.Contains(originalTIndex))
                {
                    nodeSortedTriangles.Add(originalTIndex);
                    sortedTriangles.Add(originalTIndex);
                }
            }
            
            // node data bits (from backwards)
            // 0: is this node leaf?
            // 1-8: is the children at index traversable?
            
            uint nodeData = (uint) (hWideNode.isLeaf ? 1 : 0);
            
            BoundsBox[] childrenBounds = new BoundsBox[hWideNode.children.Count];
        
            for (byte ch = 0; ch < hWideNode.children.Count; ch++)
            {
                nodeData |= (uint)(0x1 << (ch + 1));
                childrenBounds[ch] = hWideNode.children[ch].bounds;
            }

            int qtyElements = hWideNode.isLeaf ? nodeSortedTriangles.Count : hWideNode.children.Count;

            nodeData |= ( (uint)qtyElements & 0b1111) << 9;

            uint childLeavesMask = 0;            
            for (byte ch = 0; ch < hWideNode.children.Count; ch++)
            {
                childLeavesMask |= (uint)( (hWideNode.children[ch].isLeaf ? 1 : 0)  << ch);
            }

            nodeData |= ((uint)childLeavesMask & 0xff) << 13;
            
            StackBVH4Node stackNode = new StackBVH4Node
            {
                data = nodeData,
                firstElementIndex =  hWideNode.isLeaf ? (sortedTriangles.Count - nodeSortedTriangles.Count) : hWideNode.indexFirstChild,
                boundsMin = hWideNode.bounds.min,
                extends = hWideNode.bounds.GetSize()
            };
        
            var compressedBounds = BVHUtils.Compress(childrenBounds, hWideNode.bounds);
            stackNode.bb0 = compressedBounds.Length > 0 ? compressedBounds[0] : new uint2();
            stackNode.bb1 = compressedBounds.Length > 1 ? compressedBounds[1] : new uint2();
            stackNode.bb2 = compressedBounds.Length > 2 ? compressedBounds[2] : new uint2();
            stackNode.bb3 = compressedBounds.Length > 3 ? compressedBounds[3] : new uint2();
            stackNode.bb4 = compressedBounds.Length > 4 ? compressedBounds[4] : new uint2();
            stackNode.bb5 = compressedBounds.Length > 5 ? compressedBounds[5] : new uint2();
            stackNode.bb6 = compressedBounds.Length > 6 ? compressedBounds[6] : new uint2();
            stackNode.bb7 = compressedBounds.Length > 7 ? compressedBounds[7] : new uint2();
                
            stackNode.precisionLoss = 0f;
        
            // I know... this is quite expensive, but I can give me an idea of the precision loss like this
            // I need to either improve base precision or make this part faster
            BoundsBox[] decompressedBounds = BVHUtils.DecompressAll(stackNode);
                
            for (int ch = 0; ch < hWideNode.children.Count; ch++)
            {
                stackNode.precisionLoss = math.max(stackNode.precisionLoss, math.distance(hWideNode.children[ch].bounds.min, decompressedBounds[ch].min));
                stackNode.precisionLoss = math.max(stackNode.precisionLoss, math.distance(hWideNode.children[ch].bounds.max, decompressedBounds[ch].max));
            }
            
            outNodes[i] = stackNode;
        
            ti += nodeSortedTriangles.Count;
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

    // public static List<BinaryNode> GenerateBinaryHeapNodes()
    // {
    //     SerializedScene_Geometry sceneGeom = SceneExporter.s_sceneGeom;
    //     
    //     // first I create a BVH tree using a BinaryNode which is a heap based implementation
    //     // each node has pointers to the children. The thing is that to be used in the Burst jobs
    //     // I need to pass this to a value type array. This is common too with GPU data structures,
    //     // you'll find that's common to do this same process to pass kd trees to the GPU.
    //     BinaryNode heapNodeRoot = new BinaryNode(new BoundsBox(sceneGeom.boundMin, sceneGeom.boundMax), sceneGeom.qtyTriangles);
    //     
    //     BVHSplit.SplitNode(heapNodeRoot, SceneExport_GatherTriangles.s_gatheredTriangles);
    //
    //     List<BinaryNode> heapNodes = new List<BinaryNode>();
    //     
    //     // important! this GetAllNodesSorted algorithm has to sort properly the nodes while gathering them
    //     // which is in a way that all children of a node will always be together, so the traversal logic can
    //     // look for the first index and then iterate by 4
    //     heapNodeRoot.GetAllNodes(heapNodes, SceneExport_GatherTriangles.s_gatheredTriangles);
    //
    //     return heapNodes;
    // }
}