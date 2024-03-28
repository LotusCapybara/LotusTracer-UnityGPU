using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using CapyTracerCore.Core;
using LotusTracer.Scripts.Debug.BVH;
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

    public RenderRay renderRay;

    private int _hittingIndex = -1;
    private Vector3 _hitPoint;

    public void GenerateDebugFile()
    {
        string sceneDataPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.dat");
        string sceneGeometryPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.geom");
        (s_serializedSceneData, s_serializedSceneGeometry)
            = SceneExporter.DeserializeScene(sceneDataPath, sceneGeometryPath);

        StringBuilder str = new StringBuilder("");

        BoundsBox sceneBounds = new BoundsBox(s_serializedSceneGeometry.boundMin, s_serializedSceneGeometry.boundMax);

        Debug.LogError($"bounds: {sceneBounds.ToString()}  extends: {sceneBounds.GetSize().ToString()}");
        
        for (int n = 0; n < s_serializedSceneGeometry.qtyBVHNodes; n++)
        {
            var stackNode = s_serializedSceneGeometry.bvhNodes[n];
            bool isLeaf = (stackNode.data & 0b1) == 1;
            uint qtyElements = (stackNode.data >> 9) & 0b11111;
            uint qtyTriangles = isLeaf ? qtyElements : 0;
            uint qtyChildren = isLeaf ? 0 : qtyElements;

            BoundsBox[] decompressedBounds = new BoundsBox[qtyChildren];

            if (qtyChildren > 0)
                decompressedBounds[0] = BVHUtils.Decompress(stackNode.bb0, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 1)
                decompressedBounds[1] = BVHUtils.Decompress(stackNode.bb1, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 2)
                decompressedBounds[2] = BVHUtils.Decompress(stackNode.bb2, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 3)
                decompressedBounds[3] = BVHUtils.Decompress(stackNode.bb3, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 4)
                decompressedBounds[4] = BVHUtils.Decompress(stackNode.bb4, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 5)
                decompressedBounds[5] = BVHUtils.Decompress(stackNode.bb5, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 6)
                decompressedBounds[6] = BVHUtils.Decompress(stackNode.bb6, stackNode.boundsMin, stackNode.extends, 0);
            if (qtyChildren > 7)
                decompressedBounds[7] = BVHUtils.Decompress(stackNode.bb7, stackNode.boundsMin, stackNode.extends, 0);


            str.Append($"n:{n}");
            string leafTag = isLeaf ? "leaf" : "node";
            str.Append($" - {leafTag} tris: {qtyTriangles} ch: {qtyChildren}  start at: {stackNode.firstElementIndex}\n");

            for (int ch = 0; ch < qtyChildren; ch++)
                str.Append($"      - {decompressedBounds[ch].ToString()}\n");

            str.Append("\n\n\n");
        }

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
            BoundsBox[] bounds4 = new BoundsBox[4];

            for (int b = 0; b < 4; b++)
            {
                BoundsBox testBounds = new BoundsBox();
            
                testBounds.min = new float3(Random.Range(-10f, 10f), Random.Range(-10f, 1f), Random.Range(-10f, 10f));
                testBounds.max = new float3(
                    Random.Range(testBounds.min.x, testBounds.min.x + 10f), 
                    Random.Range(testBounds.min.y, testBounds.min.y + 10f),
                    Random.Range(testBounds.min.z, testBounds.min.z + 10f));

                if (b > 0)
                {
                    testBounds.min += bounds4[b - 1].max;
                    testBounds.max += bounds4[b - 1].max;
                }
                
                bounds4[b] = testBounds;
                
                parentBounds.ExpandWithBounds(testBounds);
            }

            var compressed = BVHUtils.Compress(bounds4, parentBounds);
            BoundsBox[] decompressed = new BoundsBox[bounds4.Length];
                
            for(int c = 0; c < decompressed.Length; c++)
                decompressed[c] = BVHUtils.Decompress(compressed[c], parentBounds.min, parentBounds.GetSize(), 0);


            str.Append($"- parent: {parentBounds.ToString()} \n\n");
            
            for (int b = 0; b < 4; b++)
            {
                str.Append($"- before {b}: {bounds4[b].ToString()} \n");
                str.Append($"- after {b}: {decompressed[b].ToString()} \n\n");

                maxErrorMin = math.max(maxErrorMin, Vector3.Distance(bounds4[b].min, decompressed[b].min));
                maxErrorMax = math.max(maxErrorMax, Vector3.Distance(bounds4[b].max, decompressed[b].max));
            }

            str.Append($"--------------------------------\n\n\n\n");
        }

        Debug.LogError($"Compression Error.   Min: {maxErrorMin}   Max: {maxErrorMax}");
        
        string filepath = Application.dataPath + "/DebugTemp/bvh_compression_test.txt";
        File.WriteAllText(filepath, str.ToString());
    }

    public void TestRay()
    {
        string sceneDataPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.dat");
        string sceneGeometryPath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.geom");
        (s_serializedSceneData, s_serializedSceneGeometry)
            = SceneExporter.DeserializeScene(sceneDataPath, sceneGeometryPath);;

        _hittingIndex = BVHTestFunctions.TestRay(renderRay, s_serializedSceneGeometry);
        
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(renderRay.origin, renderRay.origin + renderRay.direction * 40f);

        if (_hittingIndex >= 0)
        {
            Gizmos.color = Color.green;

            var tri = s_serializedSceneGeometry.triangleVertices[_hittingIndex];
            
            Gizmos.DrawLine(tri.posA, tri.posA + tri.p0p1);
            Gizmos.DrawLine(tri.posA, tri.posA + tri.p0p2);
            Gizmos.DrawLine(tri.posA + tri.p0p1, tri.posA + tri.p0p2);

        }
    }
}
