using System.Collections.Generic;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class BVHNodeVisualizer : MonoBehaviour
{
    public bool showTriangles;
    public bool isLeaf;
    public int triangles;

    public Transform prefabBounds;

    public int nodeIndex;
    public BoundsBox bounds;
    private List<BVHNodeVisualizer> children;
    
    public void SetNode(int nodeIndex, int depth)
    {
        this.nodeIndex = nodeIndex;
        

        ref StackBVH4Node node = ref BVHVisualizer.s_serializedSceneGeometry.bvhNodes[nodeIndex];
        
        bounds = node.bounds;
        isLeaf = ((node.data >> 31) & 0x1) == 1;

        triangles = 0;

        if (isLeaf)
        {
            triangles = node.qtyTriangles;
            gameObject.name = $"Leaf:" + triangles.ToString();
            return;
        }

        children = new List<BVHNodeVisualizer>();

        gameObject.name = $"Inner-" + depth;
        
        int startChildIndex = node.startIndex;
        
        for (int i = 0; i < 4; i++)
        {
            BVHNodeVisualizer nodeVisualizer = Instantiate(this);
            nodeVisualizer.SetNode(startChildIndex + i, depth + 1);
            children.Add(nodeVisualizer);

            var boundMeshFilter = nodeVisualizer.gameObject.AddComponent<MeshFilter>();
            var boundMesh = nodeVisualizer.gameObject.AddComponent<MeshRenderer>();

            boundMeshFilter.sharedMesh = prefabBounds.GetComponent<MeshFilter>().sharedMesh;
            boundMesh.sharedMaterial = prefabBounds.GetComponent<MeshRenderer>().sharedMaterial;

            if (!nodeVisualizer.isLeaf || nodeVisualizer.triangles > 0)
            {
                nodeVisualizer.transform.position = nodeVisualizer.bounds.GetCenter();
                nodeVisualizer.transform.localScale = nodeVisualizer.bounds.GetSize();    
            }
            else
            {
                nodeVisualizer.transform.position = Vector3.zero;
                nodeVisualizer.transform.localScale = Vector3.zero;
            }
        }
    }

    public void ReOrderHierarchy()
    {
        if (!isLeaf)
        {
            foreach (var bvhNodeVisualizer in children)
            {
                bvhNodeVisualizer.transform.SetParent(transform);
            }    
            
            foreach (var bvhNodeVisualizer in children)
            {
                bvhNodeVisualizer.ReOrderHierarchy();
            }  
        }
        
    }

    public void PrintDebug()
    {
        // float3 minVertexPos = F3.INFINITY;
        // float3 maxVertexPos = -F3.INFINITY;
        //
        // foreach (var tIndex in node.triangleIndices)
        // {
        //     for (int i = 0; i < 3; i++)
        //     {
        //         minVertexPos[i] = math.min(minVertexPos[i], BVHVisualizer.s_serializedScene.triangles[tIndex].posA[i]);
        //         minVertexPos[i] = math.min(minVertexPos[i], (BVHVisualizer.s_serializedScene.triangles[tIndex].posA + BVHVisualizer.s_serializedScene.triangles[tIndex].p0p1)[i]);
        //         minVertexPos[i] = math.min(minVertexPos[i], (BVHVisualizer.s_serializedScene.triangles[tIndex].posA + BVHVisualizer.s_serializedScene.triangles[tIndex].p0p2)[i]);
        //         
        //         maxVertexPos[i] = math.max(maxVertexPos[i], BVHVisualizer.s_serializedScene.triangles[tIndex].posA[i]);
        //         maxVertexPos[i] = math.max(maxVertexPos[i], (BVHVisualizer.s_serializedScene.triangles[tIndex].posA + BVHVisualizer.s_serializedScene.triangles[tIndex].p0p1)[i]);
        //         maxVertexPos[i] = math.max(maxVertexPos[i], (BVHVisualizer.s_serializedScene.triangles[tIndex].posA + BVHVisualizer.s_serializedScene.triangles[tIndex].p0p2)[i]);
        //     }
        // }
        //
        // Debug.Log($"Triangles:  min={minVertexPos}   max={maxVertexPos}");
        // Debug.Log($"Bounds:  min={node.bounds.min}   max={node.bounds.max}");
        
    }
    
    private void OnDrawGizmosSelected()
    {
        ref StackBVH4Node node = ref BVHVisualizer.s_serializedSceneGeometry.bvhNodes[nodeIndex];
        if (isLeaf && node.qtyTriangles > 0 && showTriangles)
        {
            Color defaultColor = Gizmos.color;
            
            for(int tIndex = node.startIndex; tIndex < node.startIndex + node.qtyTriangles; tIndex++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA, 0.05f);
                Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA + BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].p0p1, 0.05f);
                Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA + BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].p0p2, 0.05f);
            }

            Gizmos.color = defaultColor;
        }
    }
}