#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneExporter))]
public class SceneExporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("Export Binary"))
        {
            SceneExporter exporter = (target as SceneExporter);
            exporter.GenerateAsBinary();
        }
    }
}
#endif