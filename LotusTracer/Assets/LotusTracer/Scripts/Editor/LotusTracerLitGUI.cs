using System.Linq;
using UnityEditor;
using UnityEngine;

public class LotusTracerLitGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material[] targetMats = materialEditor.targets.Select(o => o as Material).ToArray();

        GUILayout.Label("Diffuse Model");
        int diffuseModel = targetMats[0].GetInteger("_DiffuseModel");
        
        EditorGUI.BeginChangeCheck();
        
        diffuseModel = EditorGUILayout.Popup(diffuseModel, new string[] { "Lambert", "OrenNayar", "Disney" });

        if (EditorGUI.EndChangeCheck())
        {
            foreach (var targetMat in targetMats)
            {
                targetMat.SetInteger("_DiffuseModel", diffuseModel);    
            }
        }
        
        base.OnGUI(materialEditor, properties);
    }
}
