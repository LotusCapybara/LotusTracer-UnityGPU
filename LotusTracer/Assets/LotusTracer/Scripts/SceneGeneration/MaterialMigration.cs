using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialMigration : MonoBehaviour
{

    public void ConvertMaterialsToLotus()
    {
        MeshRenderer[] meshes = transform.GetComponentsInChildren<MeshRenderer>();

        foreach (var mesh in meshes)
        {
            Material[] mats = mesh.sharedMaterials;

            foreach (var material in mats)
            {
                if (material.shader.name == "Standard")
                {
                    ConvertFromStandardToLotus(material);
                }
            }
        }
        
        
    }


    public void ConvertFromStandardToLotus(Material mat)
    {
        float smoothness = mat.GetFloat("_Glossiness");
        
        Color color = mat.GetColor("_Color");
        Texture albedo = mat.GetTexture("_MainTex");
        Texture normal = mat.GetTexture("_BumpMap");
        Texture glossMap = mat.GetTexture("_MetallicGlossMap");
        
        mat.shader = Shader.Find("Shader Graphs/lotus-lit");
        
        mat.SetTexture("_AlbedoMap", albedo);
        mat.SetTexture("_NormalMap", normal);
        // not sure about this one....
        mat.SetTexture("_RoughnessMap", glossMap);
        mat.SetInt("_InvertRoughnessMap", 1);
        
        // mat.SetTexture("_MetallicMap", normal);
        // mat.SetTexture("_EmissionMap", normal);

        mat.SetColor("_BaseColor", color);
        
        //mat.SetColor("_EmissionColor", color);
        
        mat.SetFloat("_RoughPower", 1f - smoothness);
        mat.SetFloat("_CoatPower", 0f);
        mat.SetFloat("_CoatRoughnessPower", 0f);
        mat.SetFloat("_MetallicPower", 0f);
        mat.SetFloat("_EmissionPower", 0f);
        mat.SetFloat("_IOR", 1.5f);
        mat.SetFloat("_AnisoPower", 0f);
        mat.SetFloat("_SpecularTransmission", 0f);
        mat.SetFloat("_MediumDensity", 1f);
        mat.SetFloat("_ScatteringDirection", 0f);
    }
    
}
