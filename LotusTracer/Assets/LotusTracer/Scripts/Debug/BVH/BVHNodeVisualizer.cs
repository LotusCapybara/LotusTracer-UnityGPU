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
    
    public void SetNode(int nodeIndex, int depth)
    {
        this.nodeIndex = nodeIndex;
        

        ref StackBVH4Node node = ref BVHVisualizer.s_serializedSceneGeometry.bvhNodes[nodeIndex];
        
        bounds = node.bounds;
        dataBinary = Convert.ToString(node.data, 2);
        isLeaf = (node.data & 0b1) == 1;

        triangles = 0;

        if (isLeaf)
        {
            triangles = node.qtyTriangles;
            gameObject.name = $"Leaf-{depth}:" + triangles.ToString();
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
            // boundMesh.enabled = false;

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
                nodeVisualizer.gameObject.SetActive(false);
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

#if  UNITY_EDITOR    
    private void OnDrawGizmosSelected()
    {
        if(!showDebugTriangles)
            return;
        
        ref StackBVH4Node node = ref BVHVisualizer.s_serializedSceneGeometry.bvhNodes[nodeIndex];
        if (isLeaf && node.qtyTriangles > 0)
        {
            Color defaultColor = Gizmos.color;
            
            for(int tIndex = node.startIndex; tIndex < node.startIndex + node.qtyTriangles; tIndex++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA, gizmoSize);
                Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA + BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].p0p1, gizmoSize);
                Gizmos.DrawSphere(BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].posA + BVHVisualizer.s_serializedSceneGeometry.triangleVertices[tIndex].p0p2, gizmoSize);
            }

            Gizmos.color = defaultColor;
        }
    }
#endif    
}