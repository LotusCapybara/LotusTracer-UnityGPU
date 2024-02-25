using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LotusTracerLitGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material targetMat = materialEditor.target as Material;

        GUILayout.Label("Diffuse Model");
        int diffuseModel = targetMat.GetInteger("_DiffuseModel");
        diffuseModel = EditorGUILayout.Popup(diffuseModel, new string[] { "Lambert", "OrenNayar", "Disney" });
        
        targetMat.SetInteger("_DiffuseModel", diffuseModel);
        
        base.OnGUI(materialEditor, properties);
    }
}
