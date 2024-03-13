using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class BVHVisualizer : MonoBehaviour
{
    public static SerializedScene_Data s_serializedSceneData;
    public static SerializedScene_Geometry s_serializedSceneGeometry;
    public static BoundsBox s_sceneBounds;
    
    public string sceneName = "Classic-Cornell";

    public Transform parentContainer;

    public BVHNodeVisualizer prefabNodeVisualizer;

    public void GenerateDebugFile()
    {
        string sceneDataPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.dat");
        string sceneGeometryPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.geom");
        (s_serializedSceneData, s_serializedSceneGeometry)
            = SceneExporter.DeserializeScene(sceneDataPath, sceneGeometryPath);

        StringBuilder str = new StringBuilder("");

        BoundsBox sceneBounds = new BoundsBox(s_serializedSceneGeometry.boundMin, s_serializedSceneGeometry.boundMax);

        Debug.LogError($"bounds: {sceneBounds.ToString()}  extends: {sceneBounds.GetSize().ToString()}");
        
        // for (int n = 0; n < s_serializedSceneGeometry.qtyBVHNodes; n++)
        // {
        //     bool isLeaf = (s_serializedSceneGeometry.bvhNodes[n].data & 0b1) == 1;
        //     int qtyTriangles = s_serializedSceneGeometry.bvhNodes[n].qtyTriangles;
        //     string bounds = BVHUtils.Decompress(s_serializedSceneGeometry.bvhNodes[n].qBounds, sceneBounds, sceneBounds.GetSize()).ToString();
        //     int depth = 0; //s_serializedSceneGeometry.bvhNodes[n].depth;
        //
        //     string padding = "";
        //     for (int d = 0; d < depth; d++)
        //         padding += "  ";
        //
        //     string leafTag = isLeaf ? "leaf" : "node";
        //     
        //     str.Append($"{padding} -{depth} - {leafTag} tris: {qtyTriangles}   bounds: {bounds}\n");
        // }

        string filepath = Application.dataPath + "/DebugTemp/bvh_debug.txt";
        File.WriteAllText(filepath, str.ToString());
    }

    public void TestCompression()
    {
        string sceneDataPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.dat");
        string sceneGeometryPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.geom");
        (s_serializedSceneData, s_serializedSceneGeometry)
            = SceneExporter.DeserializeScene(sceneDataPath, sceneGeometryPath);

        StringBuilder str = new StringBuilder("");

        BoundsBox sceneBounds = new BoundsBox(s_serializedSceneGeometry.boundMin, s_serializedSceneGeometry.boundMax);
        float maxErrorMin = 0;
        float maxErrorMax = 0;

        for (int i = 0; i < 100; i++)
        {
            BoundsBox parentBounds = BoundsBox.AS_SHRINK;;
            BoundsBox[] bounds8 = new BoundsBox[8];

            for (int b = 0; b < 8; b++)
            {
                BoundsBox testBounds = new BoundsBox();
            
                testBounds.min = new float3(Random.Range(-10f, 10f), Random.Range(-10f, 1f), Random.Range(-10f, 10f));
                testBounds.max = new float3(
                    Random.Range(testBounds.min.x, testBounds.min.x + 10f), 
                    Random.Range(testBounds.min.y, testBounds.min.y + 10f),
                    Random.Range(testBounds.min.z, testBounds.min.z + 10f));

                if (b > 0)
                {
                    testBounds.min += bounds8[b - 1].max;
                    testBounds.max += bounds8[b - 1].max;
                }
                
                bounds8[b] = testBounds;
                
                parentBounds.ExpandWithBounds(testBounds);
            }

            StackBVH4Node compressed = BVHUtils.Compress(bounds8, parentBounds);
            BoundsBox[] decompressed = BVHUtils.Decompress(compressed);


            str.Append($"- parent: {parentBounds.ToString()} \n\n");
            
            for (int b = 0; b < 8; b++)
            {
                str.Append($"- before {b}: {bounds8[b].ToString()} \n");
                str.Append($"- after {b}: {decompressed[b].ToString()} \n\n");    
            }

            str.Append($"--------------------------------\n\n\n\n");
        }

        Debug.LogError($"Compression Error.   Min: {maxErrorMin}   Max: {maxErrorMax}");
        
        string filepath = Application.dataPath + "/DebugTemp/bvh_compression_test.txt";
        File.WriteAllText(filepath, str.ToString());
    }
    
    
    public void GenerateVisualization()
    {
        StartCoroutine(GenerateRoutine());
    }

    public void ClearChildren()
    {
        while (parentContainer.childCount > 0)
        {
            DestroyImmediate(parentContainer.GetChild(0).gameObject);
        }
    }

    private IEnumerator GenerateRoutine()
    {
        while (parentContainer.childCount > 0)
        {
            DestroyImmediate(parentContainer.GetChild(0).gameObject);
            yield return null;
        }
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        string sceneDataPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.dat");
        string sceneGeometryPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.geom");
        (s_serializedSceneData, s_serializedSceneGeometry)
            = SceneExporter.DeserializeScene(sceneDataPath, sceneGeometryPath);
        
        stopwatch.Stop();
        Debug.Log("Total Time: " + stopwatch.Elapsed.TotalSeconds);
        
        yield return null;
        
        s_sceneBounds = new BoundsBox(s_serializedSceneGeometry.boundMin, s_serializedSceneGeometry.boundMax);
        
        BVHNodeVisualizer nodeVisualizer = Instantiate(prefabNodeVisualizer, parentContainer);
        nodeVisualizer.SetNode(0, 0, s_sceneBounds);
        nodeVisualizer.ReOrderHierarchy();
        
        Debug.Log("Finished Visualizer");
    }
}
