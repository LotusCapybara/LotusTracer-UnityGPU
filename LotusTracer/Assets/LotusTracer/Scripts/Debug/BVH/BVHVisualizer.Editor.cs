#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BVHVisualizer))]
public class BVHVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        BVHVisualizer visualizer = (target as BVHVisualizer);
        
        if (GUILayout.Button("GenerateVisualization Debug File"))
        {
            visualizer.GenerateDebugFile();
        }
        
        if (GUILayout.Button("GenerateVisualization Visualization"))
        {
            visualizer.GenerateVisualization();
        }
        
        if (GUILayout.Button("Clear"))
        {
            visualizer.ClearChildren();
        }
        
        if (GUILayout.Button("Test Compression"))
        {
            visualizer.TestCompression();
        }
    }
}
#endif