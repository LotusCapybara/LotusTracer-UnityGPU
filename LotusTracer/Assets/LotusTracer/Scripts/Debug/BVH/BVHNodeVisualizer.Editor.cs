#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BVHNodeVisualizer))]
public class BVHNodeVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        BVHNodeVisualizer visualizer = (target as BVHNodeVisualizer);
        
        if (GUILayout.Button("Print Debug"))
        {
            visualizer.PrintDebug();
        }
        
    }
}
#endif