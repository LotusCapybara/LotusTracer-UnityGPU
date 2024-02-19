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
        
        if (GUILayout.Button("Generate"))
        {
            visualizer.Generate();
        }
        
        if (GUILayout.Button("Clear"))
        {
            visualizer.ClearChildren();
        }
    }
}
#endif