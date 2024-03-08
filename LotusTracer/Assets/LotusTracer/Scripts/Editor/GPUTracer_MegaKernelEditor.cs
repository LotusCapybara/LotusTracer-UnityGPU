#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GPUTracer_Megakernel))]
public class GPUTracer_MegakernelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("Save Image"))
        {
            GPUTracer_Megakernel tracer = (target as GPUTracer_Megakernel);
            tracer.SaveImage();
        }
    }
}
#endif