using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public static class SceneExport_GatherTriangles
{
    public static void Export(SerializedScene scene, GameObject sceneContainer, List<Material> outUnityMaterials)
    {
        
        MeshRenderer[] meshes = sceneContainer.transform.GetComponentsInChildren<MeshRenderer>();


        BoundsBox sceneBounds = new BoundsBox();

        List<Task<List<FastTriangle>>> allTasks = new List<Task<List<FastTriangle>>>();
        
        for(int m = 0; m < meshes.Length; m++)
        {
            sceneBounds.ExpandWithPoint(meshes[m].bounds.min);
            sceneBounds.ExpandWithPoint(meshes[m].bounds.max);
            
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
            
            for (int subMesh = 0; subMesh < meshDef.subMeshCount; subMesh++)
            {
                List<Vector2> subMeshUvs = new List<Vector2>();
                meshDef.GetUVs(0, subMeshUvs);
                
                int materialIndex =  (ushort)outUnityMaterials.FindIndex(mat => mat.name == meshes[m].sharedMaterials[subMesh].name);
                int[] triangles = meshDef.GetTriangles(subMesh);
                
                allTasks.Add(
                    Task.Run(() => GetTrianglesForMesh(
                        transformPos, transformScale, transformRotation,
                        verticesA, normalsA, tangentsA, triangles, subMeshUvs, materialIndex, isInvisibleLightBouncer
                        )
                    ));
            }
        }
        
        
        List<FastTriangle> allTriangles = new List<FastTriangle>();
        Task.WaitAll(allTasks.ToArray());
        foreach (var allTask in allTasks)
        {
            allTriangles.AddRange(allTask.Result);
        }
        
        scene.triangles = allTriangles.ToArray();
        scene.qtyTriangles = allTriangles.Count;
        scene.boundMin = sceneBounds.min;
        scene.boundMax = sceneBounds.max;
    }

    private static List<FastTriangle> GetTrianglesForMesh(
        Vector3 transformPos, Vector3 transformScale, Quaternion transformRotation,
        in Vector3[] vertices, in Vector3[] normals, in Vector4[] tangents, in int[] triangles, List<Vector2> uvs, int materialIndex,
        bool isInvisibleLightBouncer)
    {
        List<FastTriangle> allTriangles = new List<FastTriangle>();
        
        int t = 0;

        for (int st = 0; st < triangles.Length; st += 3)
        {
            FastTriangle newTriangle = new FastTriangle();
            newTriangle.materialIndex = materialIndex;
            newTriangle.flags = 0;

            if(isInvisibleLightBouncer)
                newTriangle.flags |= 0b1;
            
            // copy position from unity triangle
            // copy normals from unity triangle
            newTriangle.centerPos = float3.zero;

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

                newTriangle.centerPos += (float3)pos;

                newTriangle.SetVertexPos(i, pos);
                newTriangle.SetVertexNormal(i, math.normalize(nor) );
                newTriangle.SetVertexTangent(i, math.normalize(tan) );
                
                if(uvs.Count > 0)
                    newTriangle.SetTextureUV(i, uvs[triangles[st + i]]);
                else
                    newTriangle.SetTextureUV(i, new float2());
                
            }
            newTriangle.centerPos *= 0.3333f;

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
