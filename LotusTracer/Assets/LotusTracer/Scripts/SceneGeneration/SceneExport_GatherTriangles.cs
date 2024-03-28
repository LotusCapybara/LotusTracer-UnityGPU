using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public static class SceneExport_GatherTriangles
{
    public static RenderTriangle[] s_gatheredTriangles;
    public static BoundsBox[] s_triangleBounds;
    
    public static void Export(GameObject sceneContainer, List<Material> outUnityMaterials)
    {
        SerializedScene_Data sceneData = SceneExporter.s_sceneData;
        SerializedScene_Geometry sceneGeom = SceneExporter.s_sceneGeom;
        
        MeshRenderer[] meshes = sceneContainer.transform.GetComponentsInChildren<MeshRenderer>();
        

        BoundsBox sceneBounds = BoundsBox.AS_SHRINK;

        List<Task<List<RenderTriangle>>> allTasks = new List<Task<List<RenderTriangle>>>();
        
        for(int m = 0; m < meshes.Length; m++)
        {
            foreach (var meshRendererMaterial in meshes[m].sharedMaterials)
            {
                if (!outUnityMaterials.Any( m => m.name == meshRendererMaterial.name))
                    outUnityMaterials.Add(meshRendererMaterial);
            }
            
            Vector3 transformPos = meshes[m].transform.position;
            Vector3 transformScale = meshes[m].transform.lossyScale;
            Quaternion transformRotation = meshes[m].transform.rotation;

            var meshDef = meshes[m].GetComponent<MeshFilter>().sharedMesh;
            bool isInvisibleLightBouncer = meshes[m].GetComponent<InvisibleLightBouncer>() != null;    

            Vector3[] verticesA = meshDef.vertices;
            Vector3[] normalsA = meshDef.normals;
            Vector4[] tangentsA = meshDef.tangents;
            Color[] vertexColorsA = meshDef.colors;
            
            for (int subMesh = 0; subMesh < meshDef.subMeshCount; subMesh++)
            {
                List<Vector2> subMeshUvs = new List<Vector2>();
                meshDef.GetUVs(0, subMeshUvs);
                
                int materialIndex =  (ushort)outUnityMaterials.FindIndex(mat => mat.name == meshes[m].sharedMaterials[subMesh].name);
                int[] triangles = meshDef.GetTriangles(subMesh);
                
                bool useVertexColor = outUnityMaterials[materialIndex].GetInt("_UseVertexColors") != 0;
                
                allTasks.Add(
                    Task.Run(() => GetTrianglesForMesh(
                        transformPos, transformScale, transformRotation,
                        verticesA, normalsA, tangentsA, triangles, subMeshUvs, vertexColorsA, materialIndex, isInvisibleLightBouncer, useVertexColor
                        )
                    ));
            }
        }


        Task.WaitAll(allTasks.ToArray());
        
        List<BoundsBox> triangleBounds = new List<BoundsBox>();

        int totalTriangles = 0;

        foreach (var allTask in allTasks)
            totalTriangles += allTask.Result.Count;

        s_gatheredTriangles = new RenderTriangle[totalTriangles];

        int tIndex = 0;
        foreach (var allTask in allTasks)
        {
            for (int t = 0; t < allTask.Result.Count; t++)
            {
                s_gatheredTriangles[tIndex] = allTask.Result[t];
                
                sceneBounds.ExpandWithBounds(s_gatheredTriangles[tIndex].bounds);
                
                tIndex++;
            }
        }

        s_triangleBounds = triangleBounds.ToArray();
        sceneGeom.qtyTriangles = totalTriangles;
        sceneGeom.boundMin = sceneBounds.min;
        sceneGeom.boundMax = sceneBounds.max;
    }

    private static List<RenderTriangle> GetTrianglesForMesh(
        Vector3 transformPos, Vector3 transformScale, Quaternion transformRotation,
        in Vector3[] vertices, in Vector3[] normals, in Vector4[] tangents, in int[] triangles, List<Vector2> uvs, Color[] vertexColors,
        int materialIndex, bool isInvisibleLightBouncer, bool usesVertexColor)
    {
        List<RenderTriangle> allTriangles = new List<RenderTriangle>();
        allTriangles.Capacity = triangles.Length;
        
        int t = 0;
        
        for (int st = 0; st < triangles.Length; st += 3)
        {
            RenderTriangle newTriangle = new RenderTriangle();
            newTriangle.materialIndex = materialIndex;
            newTriangle.flags = 0;
            newTriangle.vertexColor = usesVertexColor ? new float3(0, 0, 0) : new float3(1, 1, 1);

            if(isInvisibleLightBouncer)
                newTriangle.flags |= 0b1;
            
            // copy position from unity triangle
            // copy normals from unity triangle
            newTriangle.centerPos = float3.zero;

            float3 bMin = F3.INFINITY;
            float3 bMax = F3.INFINITY_INV;

            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = vertices[triangles[st + i]];
                Vector3 nor = normals[triangles[st + i]];
                Vector4 tan = tangents[triangles[st + i]];

                // apply scale
                pos.x *= transformScale.x;
                pos.y *= transformScale.y;
                pos.z *= transformScale.z;

                // apply rotation
                pos = transformRotation * pos;
                nor = Vector3.Normalize( transformRotation * nor );
                Vector3 tanDirection = Vector3.Normalize( transformRotation * tan );
                tan = new Vector4(tanDirection.x, tanDirection.y, tanDirection.z, tan.w);

                // apply translation
                pos += transformPos;

                if (usesVertexColor && vertexColors.Length > triangles[st + i])
                {
                    var vColor = vertexColors[triangles[st + i]];
                    newTriangle.vertexColor += new float3(vColor.r, vColor.g, vColor.b);
                }

                newTriangle.centerPos += (float3)pos;

                newTriangle.SetVertexPos(i, pos);
                newTriangle.SetVertexNormal(i, math.normalize(nor) );
                newTriangle.SetVertexTangent(i, math.normalize(tan) );
                
                if(uvs.Count > 0)
                    newTriangle.SetTextureUV(i, uvs[triangles[st + i]]);
                else
                    newTriangle.SetTextureUV(i, new float2());

                bMin = math.min(pos, bMin);
                bMax = math.max(pos, bMax);
            }
            
            
            newTriangle.centerPos *= 0.3333f;

            if (usesVertexColor)
            {
                newTriangle.vertexColor *= 0.33333f;
            }

            newTriangle.bounds = new BoundsBox(bMin, bMax);

            // just for testing stuff, some scenes will look fine even without good tangents
            // ValidateIsOrthogonal(newTriangle.normalA, newTriangle.tangentA, "NormalA", "TangentA");
            // ValidateIsOrthogonal(newTriangle.normalB, newTriangle.tangentB, "NormalB", "TangentB");
            // ValidateIsOrthogonal(newTriangle.normalC, newTriangle.tangentC, "NormalC", "TangentC");
                        
            t++;

            allTriangles.Add(newTriangle);
        }

        return allTriangles;
    }

    private static void ValidateIsOrthogonal(Vector3 a, Vector3 b, string nameA, string nameB)
    {
        float absDot = math.abs(math.dot(a, b)); 
        if (absDot > 0.0001)
            throw new Exception($"Wrong Orth Validation. d: {absDot} {nameA}:{a}  {nameB}:{b}");
    }
    
}
