using System;
using System.Collections.Generic;
using CapyTracerCore.Core;
using UnityEngine;

#if  UNITY_EDITOR
using UnityEditor;
#endif

public class BVHNodeVisualizer : MonoBehaviour
{
    public bool showDebugTriangles;
    public bool isLeaf;
    public int triangles;
    public string dataBinary;
    public float gizmoSize = 0.06f;

    public Transform prefabBounds;

    public int nodeIndex;
    public BoundsBox bounds;
    private List<BVHNodeVisualizer> children;
    
    public void SetNode(int nodeIndex, int depth, BoundsBox newBounds)
    {
        this.nodeIndex = nodeIndex;
        

        ref StackBVH4Node node = ref BVHVisualizer.s_serializedSceneGeometry.bvhNodes[nodeIndex];

        bounds = newBounds; 
        dataBinary = Convert.ToString(node.data, 2);
        isLeaf = (node.data & 0b1) == 1;

        uint qtyChildren = (node.data >> 9) & 0b11111;

        triangles = (int)(isLeaf ? qtyChildren : 0);
        int childQty = (int)(isLeaf ? 0 : qtyChildren);
        children = new List<BVHNodeVisualizer>();

        gameObject.name = $"Node-{depth}-ch: {childQty}  -t: {triangles}";
        
        if(isLeaf)
            return;
        
        if(depth > 6)
            return;
        
        BoundsBox[] decompressedBounds = BVHUtils.DecompressAll(node);
        
        
        for (int i = 0; i < childQty; i++)
        {
            BVHNodeVisualizer nodeVisualizer = Instantiate(this);

            BoundsBox childBounds = decompressedBounds[i];
            nodeVisualizer.SetNode(node.firstElementIndex + i, depth + 1, childBounds);
            children.Add(nodeVisualizer);

            var boundMeshFilter = nodeVisualizer.gameObject.AddComponent<MeshFilter>();
            var boundMesh = nodeVisualizer.gameObject.AddComponent<MeshRenderer>();
            // boundMesh.enabled = false;

            boundMeshFilter.sharedMesh = prefabBounds.GetComponent<MeshFilter>().sharedMesh;

            boundMesh.sharedMaterial = prefabBounds.GetComponent<MeshRenderer>().sharedMaterial;

            nodeVisualizer.transform.position = nodeVisualizer.bounds.GetCenter();
            nodeVisualizer.transform.localScale = nodeVisualizer.bounds.GetSize();
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

#if  UNITY_EDITOR    
    private void OnDrawGizmosSelected()
    {
        if(!showDebugTriangles)
            return;
        
        ref StackBVH4Node node = ref BVHVisualizer.s_serializedSceneGeometry.bvhNodes[nodeIndex];
        if (isLeaf && triangles > 0)
        {
            Color defaultColor = Gizmos.color;
            
            // for(int tIndex = node.startIndex; tIndex < node.startIndex + node.qtyTriangles; tIndex++)
            // {
            //     Gizmos.color = Color.yellow;
            //     Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA, gizmoSize);
            //     Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA + BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].p0p1, gizmoSize);
            //     Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA + BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].p0p2, gizmoSize);
            // }

            Gizmos.color = defaultColor;
        }
    }
#endif    
}