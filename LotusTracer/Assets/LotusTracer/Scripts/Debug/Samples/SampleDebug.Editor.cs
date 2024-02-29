#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SampleDebug))]
public class SampleDebugEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        SampleDebug visualizer = (target as SampleDebug);
        
        if (GUILayout.Button("Sample"))
        {
            visualizer.Sample();
        }
    }
}
#endif