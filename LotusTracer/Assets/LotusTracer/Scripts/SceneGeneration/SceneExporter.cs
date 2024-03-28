using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CapyTracerCore.Core;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SceneExporter : MonoBehaviour
{
    
    // this is a horrible pattern but it made my code a bit cleaner and faster to dev, don't hate me
    // this is going to change later anyways,I plan to export all I can in some open source format 
    // instead of my custom binary
    public static SerializedScene_Data s_sceneData;
    public static SerializedScene_Geometry s_sceneGeom;
    
    public static string SCENES_PATH_BASE;
    public static string SCENE_NAME;
    public static bool s_ignoreReCreateTextures;

    public bool generateDebugInfo;
    
    public bool ignoreCreateTextures;
    public bool ignoreCreateGeometry;


    [Header("Gizmos")]
    public bool showGeoBoxes;
    public bool showWideTree;

    // private List<BinaryNode> _justGeneratedHeadNodes = new List<BinaryNode>();
    private List<HeapWideNode> _heapWideNodes = new List<HeapWideNode>();
    private List<GeoBox> _geoBoxes = new List<GeoBox>();
    
    private void CreateSceneAsset()
    {
        s_sceneData = new SerializedScene_Data();
        s_sceneGeom = new SerializedScene_Geometry();
        
        s_ignoreReCreateTextures = ignoreCreateGeometry;
        
        SCENE_NAME = gameObject.name;
        SCENES_PATH_BASE = Application.dataPath + $"/Resources/RenderScenes/{gameObject.name}/";
        
        AssetDatabase.Refresh();
        
        if (!Directory.Exists(SCENES_PATH_BASE))
            Directory.CreateDirectory(SCENES_PATH_BASE);
        
        Stopwatch sw = Stopwatch.StartNew();
        
        // this serializes camera, lights and other general things
        SceneExport_GeneralElements_Job.Export(gameObject);
        
        sw.Stop();
        Debug.Log($"GeneralElements_Job: {sw.Elapsed.TotalSeconds}");
        sw.Restart();

        // this pass finds all the triangles in the scene and makes a first serialization
        List<Material> unityMaterials = new List<Material>();
        SceneExport_GatherTriangles.Export(gameObject, unityMaterials);
      
        sw.Stop();
        Debug.Log($"GatherTriangles: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        SceneExport_GenerateMaterials.Export(gameObject, unityMaterials, generateDebugInfo);
        
        sw.Stop();
        Debug.Log($"GenerateMaterials: {sw.Elapsed.TotalSeconds}");
        sw.Restart();
        
        if (!ignoreCreateGeometry)
        {
            // _justGeneratedHeadNodes = SceneExport_GenerateBVH.GenerateBinaryHeapNodes();
            //
            // sw.Stop();
            // Debug.Log($"Generated Binary Tree: {sw.Elapsed.TotalSeconds}. Nodes: {_justGeneratedHeadNodes.Count}");
            // sw.Restart();
            //
            //
            // var rootHeapWideNode = HeapWideNode.BuildFromBinaryNodes(_justGeneratedHeadNodes[0], 0);


            _geoBoxes = GeoBox.CollectGeoBoxes(SceneExport_GatherTriangles.s_gatheredTriangles);
            sw.Stop();
            Debug.Log($"Generated GeoBox: {sw.Elapsed.TotalSeconds}.");
            sw.Restart();

            BoundsBox sceneBounds = new BoundsBox(s_sceneGeom.boundMin, s_sceneGeom.boundMax);
            HeapWideNode rootHeapWideNode = BVHSplit8.GetTreeRoot(15, sceneBounds, _geoBoxes);
            
            _heapWideNodes = new List<HeapWideNode>();
            _heapWideNodes.Add(rootHeapWideNode);
            HeapWideNode.SortWideNodes(_heapWideNodes, rootHeapWideNode, _geoBoxes);
            
            sw.Stop();
            Debug.Log($"Generated Wide Tree: {sw.Elapsed.TotalSeconds}. Nodes {_heapWideNodes.Count}");
            sw.Restart();
     
            SceneExport_GenerateBVH.Export(_heapWideNodes, _geoBoxes);
        
            sw.Stop();
            Debug.Log($"Generated Stack BVH: {sw.Elapsed.TotalSeconds}");
            sw.Restart();
        }
        
        SceneExport_ExportDats.Export(gameObject.name, ignoreCreateGeometry);
        
        sw.Stop();
        Debug.Log($"Serialized Dats: {sw.Elapsed.TotalSeconds}");
        
        s_sceneData = null;
        s_sceneGeom = null;
    }

    public void GenerateAsBinary()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        CreateSceneAsset();

        sw.Stop();
        Debug.Log("Scene Exported in: " + sw.Elapsed.TotalSeconds);
    }

    public static (SerializedScene_Data, SerializedScene_Geometry) DeserializeScene(string pathData, string pathGeometry)
    {
        SerializedScene_Data sceneData = new SerializedScene_Data();
        SerializedScene_Geometry sceneGeom = new SerializedScene_Geometry();

        using (FileStream fileStream = new FileStream(pathGeometry, FileMode.Open))
        {
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // read scene bounds
                sceneGeom.boundMin = SceneBinaryRead.ReadFloat3(reader);
                sceneGeom.boundMax = SceneBinaryRead.ReadFloat3(reader);
                
                // read all triangleIndices
                sceneGeom.qtyTriangles = reader.ReadInt32();
                sceneGeom.triangleVertices = new RenderTriangle_Vertices[sceneGeom.qtyTriangles];
                sceneGeom.triangleDatas = new RenderTriangle_Data[sceneGeom.qtyTriangles];
                
                for (int t = 0; t < sceneGeom.qtyTriangles; t++)
                {
                    var triangleVD = SceneBinaryRead.ReadTriangle(reader);
                    sceneGeom.triangleVertices[t] = triangleVD.Item1;
                    sceneGeom.triangleDatas[t] = triangleVD.Item2;
                }
                
                // read bvh nodes
                sceneGeom.qtyBVHNodes = reader.ReadInt32();
                sceneGeom.bvhNodes = new StackBVH4Node[sceneGeom.qtyBVHNodes];
                for (int n = 0; n < sceneGeom.qtyBVHNodes; n++)
                {
                    sceneGeom.bvhNodes[n] = SceneBinaryRead.ReadBvh4Node(reader);
                }
            }
        }

        using (FileStream fileStream = new FileStream(pathData, FileMode.Open))
        {
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // read camera
                sceneData.camera = SceneBinaryRead.ReadCamera(reader);
                
                // read each Material
                sceneData.qtyMaterials = reader.ReadInt32();
                sceneData.materials = new SerializedMaterial[sceneData.qtyMaterials];
                
                for (int m = 0; m < sceneData.qtyMaterials; m++)
                {
                    sceneData.materials[m] = SceneBinaryRead.ReadMaterial(reader);
                }
                
                // read lights
                sceneData.qtyLights = reader.ReadInt32();
                sceneData.lights = new RenderLight[sceneData.qtyLights];

                for (int l = 0; l < sceneData.qtyLights; l++)
                {
                    sceneData.lights[l] = SceneBinaryRead.ReadLight(reader);
                }
            }
        }

        return (sceneData, sceneGeom);
    }

    private void OnDrawGizmosSelected()
    {
        if (showGeoBoxes)
        {
            int qty = 0;
            foreach (var geoBox in _geoBoxes)
            {
                // Gizmos.DrawSphere( justGeneratedHeadNode.bounds.GetCenter(), 0.01f);
                Gizmos.DrawWireCube( geoBox.bounds.GetCenter(), geoBox.size);
                qty++;
                
                if(qty > 2000000)
                    break;   
            }   
        }

        if (showWideTree)
        {
            int qty = 0;
            foreach (var wideNode in _heapWideNodes)
            {
                if (wideNode.isLeaf && wideNode.geoBoxes.Count > 0)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube( wideNode.bounds.GetCenter(), wideNode.bounds.GetSize());
                    qty++;
                
                    if(qty > 2000000)
                        break;                              
                }
            }   
        }
    }
}