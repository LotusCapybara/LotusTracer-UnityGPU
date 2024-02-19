using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CapyTracerCore.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SceneExporter : MonoBehaviour
{
    public int bvhMaxDepth = 60;
    public int bvhMaxNodeTriangles = 3;
    
    private void CreateSceneAsset()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        SerializedScene scene = new SerializedScene();
        
        // this serializes camera, lights and other general things
        SceneExport_GeneralElements_Job.Export(scene, gameObject);
        
        sw.Stop();
        Debug.Log($"GeneralElements_Job: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        // this pass finds all the triangles in the scene and makes a first serialization
        List<Material> unityMaterials = new List<Material>();
        SceneExport_GatherTriangles.Export(scene, gameObject, unityMaterials);
      
        sw.Stop();
        Debug.Log($"GatherTriangles: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        SceneExport_GenerateMaterials.Export(scene, gameObject, unityMaterials);
        
        sw.Stop();
        Debug.Log($"GenerateMaterials: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        var heapNodes = SceneExport_GenerateBVH.GenerateHeapNodes(scene, bvhMaxDepth, bvhMaxNodeTriangles);
        
        sw.Stop();
        Debug.Log($"GenerateBVH Heap: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        SceneExport_GenerateBVH.Export(heapNodes, scene);
        
        sw.Stop();
        Debug.Log($"Parse Stack BVH: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        SceneExport_ExportDats.Export(scene, gameObject.name);
        
        sw.Stop();
        Debug.Log($"Serialized Dats: {sw.Elapsed.TotalSeconds}");
    }

    public void GenerateAsBinary()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        CreateSceneAsset();

        sw.Stop();
        Debug.Log("Scene Exported in: " + sw.Elapsed.TotalSeconds);
    }

    public static SerializedScene DeserializeScene(string path)
    {
        SerializedScene scene = new SerializedScene();
        
        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // read scene bounds
                scene.boundMin = SceneBinaryRead.ReadFloat3(reader);
                scene.boundMax = SceneBinaryRead.ReadFloat3(reader);
                
                // read camera
                scene.camera = SceneBinaryRead.ReadCamera(reader);
                
                // read each Material
                scene.qtyMaterials = reader.ReadInt32();
                scene.materials = new SerializedMaterial[scene.qtyMaterials];
                
                for (int m = 0; m < scene.qtyMaterials; m++)
                {
                    scene.materials[m] = SceneBinaryRead.ReadMaterial(reader);
                }
                
                // read all triangleIndices
                scene.qtyTriangles = reader.ReadInt32();
                scene.triangles = new FastTriangle[scene.qtyTriangles];
                
                for (int t = 0; t < scene.qtyTriangles; t++)
                {
                    FastTriangle triangle = SceneBinaryRead.ReadTriangle(reader);
                    scene.triangles[t] = triangle;
                }
                
                // read lights
                scene.qtyLights = reader.ReadInt32();
                scene.lights = new RenderLight[scene.qtyLights];

                for (int l = 0; l < scene.qtyLights; l++)
                {
                    scene.lights[l] = SceneBinaryRead.ReadLight(reader);
                }
                
                // read bvh nodes
                scene.qtyBVHNodes = reader.ReadInt32();
                scene.bvhNodes = new StackBVH4Node[scene.qtyBVHNodes];
                for (int n = 0; n < scene.qtyBVHNodes; n++)
                {
                    scene.bvhNodes[n] = SceneBinaryRead.ReadBvh4Node(reader);
                }
            }
        }

        return scene;
    }
}