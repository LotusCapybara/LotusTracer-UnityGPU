using System.Collections;
using System.Diagnostics;
using System.IO;
using CapyTracerCore.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class BVHVisualizer : MonoBehaviour
{
    public static SerializedScene s_serializedScene;
    
    public string sceneName = "Classic-Cornell";

    public Transform parentContainer;

    public BVHNodeVisualizer prefabNodeVisualizer;

    public void Generate()
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
        
        string scenePath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}.dat");
        s_serializedScene = SceneExporter.DeserializeScene(scenePath);
        
        stopwatch.Stop();
        Debug.Log("Total Time: " + stopwatch.Elapsed.TotalSeconds);
        
        yield return null;
        
        BVHNodeVisualizer nodeVisualizer = Instantiate(prefabNodeVisualizer, parentContainer);
        nodeVisualizer.SetNode(0, 0);
        nodeVisualizer.ReOrderHierarchy();
        
        Debug.Log("Finished Visualizer");
    }
}
