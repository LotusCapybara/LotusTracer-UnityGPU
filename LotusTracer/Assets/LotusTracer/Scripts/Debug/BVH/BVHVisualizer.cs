using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using CapyTracerCore.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BVHVisualizer : MonoBehaviour
{
    public static SerializedScene_Data s_serializedSceneData;
    public static SerializedScene_Geometry s_serializedSceneGeometry;
    
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
        
        for (int n = 0; n < s_serializedSceneGeometry.qtyBVHNodes; n++)
        {
            bool isLeaf = (s_serializedSceneGeometry.bvhNodes[n].data & 0b1) == 1;
            int qtyTriangles = s_serializedSceneGeometry.bvhNodes[n].qtyTriangles;
            string bounds = "";// s_serializedSceneGeometry.bvhNodes[n].bounds.ToString();
            int depth = 0; //s_serializedSceneGeometry.bvhNodes[n].depth;

            string padding = "";
            for (int d = 0; d < depth; d++)
                padding += "  ";

            string leafTag = isLeaf ? "leaf" : "node";
            
            str.Append($"{padding} -{depth} - {leafTag} tris: {qtyTriangles}   bounds: {bounds}\n");
        }

        string filepath = Application.dataPath + "/DebugTemp/bvh_debug.txt";
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
        
        BVHNodeVisualizer nodeVisualizer = Instantiate(prefabNodeVisualizer, parentContainer);
        nodeVisualizer.SetNode(0, 0);
        nodeVisualizer.ReOrderHierarchy();
        
        Debug.Log("Finished Visualizer");
    }
}
