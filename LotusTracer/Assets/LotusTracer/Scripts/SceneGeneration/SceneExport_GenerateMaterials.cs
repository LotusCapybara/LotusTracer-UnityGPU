using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public static class SceneExport_GenerateMaterials
{
    public static void Export(SerializedScene scene, GameObject sceneContainer, List<Material> unityMaterials)
    {
        // todo: it would be nice to create some atlassing of all these texture
        // it could be 1 atlas per type
        // todo important we need to be able to handle different texture sizes....
        RenderSceneTextures sceneTextures = ScriptableObject.CreateInstance<RenderSceneTextures>();
        
        List<Texture2D> texturesAlbedo = new List<Texture2D>();
        List<Texture2D> texturesNormals = new List<Texture2D>();
        List<Texture2D> texturesRough = new List<Texture2D>();
        List<Texture2D> texturesMetallic = new List<Texture2D>();
        List<Texture2D> texturesEmission = new List<Texture2D>();
        
        scene.materials = new SerializedMaterial[unityMaterials.Count];
        for (int m = 0; m < scene.materials.Length; m++)
        {
            Texture albedoMap = unityMaterials[m].HasTexture("_AlbedoMap") ? unityMaterials[m].GetTexture("_AlbedoMap") : null;
            if (albedoMap != null && ! texturesAlbedo.Contains(albedoMap))
                texturesAlbedo.Add(albedoMap as Texture2D);

            Texture normalMap = unityMaterials[m].HasTexture("_NormalMap") ? unityMaterials[m].GetTexture("_NormalMap") : null;
            if (normalMap != null && !texturesNormals.Contains(normalMap))
                texturesNormals.Add(normalMap as Texture2D);
            
            Texture roughMap = unityMaterials[m].HasTexture("_RoughnessMap") ? unityMaterials[m].GetTexture("_RoughnessMap") : null;
            if (roughMap != null && !texturesRough.Contains(roughMap))
                texturesRough.Add(roughMap as Texture2D);
            
            Texture metalMap = unityMaterials[m].HasTexture("_MetallicMap") ? unityMaterials[m].GetTexture("_MetallicMap") : null;
            if (metalMap != null && !texturesMetallic.Contains(metalMap))
                texturesMetallic.Add(metalMap as Texture2D);
            
            Texture emissionMap = unityMaterials[m].HasTexture("_EmissionMap") ? unityMaterials[m].GetTexture("_EmissionMap") : null;
            if (emissionMap != null && !texturesEmission.Contains(emissionMap))
                texturesEmission.Add(emissionMap as Texture2D);
            
            Color baseColor = unityMaterials[m].GetColor("_BaseColor");
            Color emissionColor = unityMaterials[m].GetColor("_EmissionColor");
            float roughnessPower = unityMaterials[m].GetFloat("_RoughPower");
            float coatPower = unityMaterials[m].GetFloat("_CoatPower");
            float coatPowerRoughness = unityMaterials[m].GetFloat("_CoatRoughnessPower");
            float metallicPower = unityMaterials[m].GetFloat("_MetallicPower");
            float emissionPower = unityMaterials[m].GetFloat("_EmissionPower");
            float ior = unityMaterials[m].GetFloat("_IOR");
            // float anisoPower = unityMaterials[m].GetFloat("_AnisoPower");
            float transmissionPower = unityMaterials[m].GetFloat("_SpecularTransmission");
            float mediumDensity = unityMaterials[m].GetFloat("_MediumDensity");
            float scatteringDirection = unityMaterials[m].GetFloat("_ScatteringDirection");
            float maxScatteringDistance = unityMaterials[m].GetFloat("_MaxScatteringDistance");
            // int isThin = unityMaterials[m].GetInt("_IsThin");
            
            SerializedMaterial renderMaterial = new SerializedMaterial
            {
                emissiveIntensity = emissionPower,
                color =  new float4(baseColor.r, baseColor.g, baseColor.b, baseColor.a),
                metallic = metallicPower, 
                roughness = roughnessPower,
                clearCoat = coatPower,
                clearCoatRoughness = coatPowerRoughness,
                albedoMapIndex = albedoMap == null ? -1 : texturesAlbedo.IndexOf(albedoMap as Texture2D),
                albedoMapCanvasIndex = -1,
                normalMapIndex = normalMap == null ? -1 : texturesNormals.IndexOf(normalMap as Texture2D),
                normalMapCanvasIndex = -1,
                roughMapIndex = roughMap == null ? -1 : texturesRough.IndexOf(roughMap as Texture2D),
                roughMapCanvasIndex = -1,
                metalMapIndex = metalMap == null ? -1 : texturesMetallic.IndexOf(metalMap as Texture2D),
                metalMapCanvasIndex = -1,
                emissionMapIndex = emissionMap == null ? -1 : texturesEmission.IndexOf(emissionMap as Texture2D),
                emissionMapCanvasIndex = -1,
                ior = ior,
                transmissionPower = transmissionPower,
                mediumDensity = mediumDensity,
                scatteringDirection = scatteringDirection,
                maxScatteringDistance = scatteringDirection
            };
            
            scene.materials[m] = renderMaterial;
        }

        // albedo atlases
        TexturePacker packerAlbedos = new TexturePacker(AtlasFormats.ALBEDO);
        packerAlbedos.RegisterTextures(texturesAlbedo);
        packerAlbedos.PackTextures();
        sceneTextures.albedoAtlases = packerAlbedos.atlases;
        sceneTextures.albedoTextureDatas = packerAlbedos.GetDatasAsStruct();

        // normal atlases
        TexturePacker packerNormals = new TexturePacker(AtlasFormats.NORMAL);
        packerNormals.RegisterTextures(texturesNormals);
        packerNormals.PackTextures();
        sceneTextures.normalAtlases = packerNormals.atlases;
        sceneTextures.normalTextureDatas = packerNormals.GetDatasAsStruct();
        
        // rough atlases
        TexturePacker packerRough = new TexturePacker(AtlasFormats.ROUGHNESS);
        packerRough.RegisterTextures(texturesRough);
        packerRough.PackTextures();
        sceneTextures.roughAtlases = packerRough.atlases;
        sceneTextures.roughTextureDatas = packerRough.GetDatasAsStruct();

        // metallic atlases
        TexturePacker packerMetal = new TexturePacker(AtlasFormats.METALLIC);
        packerMetal.RegisterTextures(texturesMetallic);
        packerMetal.PackTextures();
        sceneTextures.metalAtlases = packerMetal.atlases;
        sceneTextures.metalTextureDatas = packerMetal.GetDatasAsStruct();
        
        // emission atlases
        TexturePacker packerEmission = new TexturePacker(AtlasFormats.EMISSION);
        packerEmission.RegisterTextures(texturesEmission);
        packerEmission.PackTextures();
        sceneTextures.emissionAtlases = packerEmission.atlases;
        sceneTextures.emissionTextureDatas = packerEmission.GetDatasAsStruct();
        
        for (int m = 0; m < scene.materials.Length; m++)
        {
            var mat = scene.materials[m];

            mat.albedoMapCanvasIndex = packerAlbedos.GetAtlasIndexForTextureId(mat.albedoMapIndex);
            mat.normalMapCanvasIndex = packerNormals.GetAtlasIndexForTextureId(mat.normalMapIndex);
            mat.roughMapCanvasIndex = packerRough.GetAtlasIndexForTextureId(mat.roughMapIndex);
            mat.metalMapCanvasIndex = packerMetal.GetAtlasIndexForTextureId(mat.metalMapIndex);
            mat.emissionMapCanvasIndex = packerEmission.GetAtlasIndexForTextureId(mat.emissionMapIndex);

            scene.materials[m] = mat;
        }
        
        if (File.Exists(Application.dataPath + $"/Resources/RenderScenes/{sceneContainer.name}_textures.asset"))
        {
            AssetDatabase.DeleteAsset($"Assets/Resources/RenderScenes/{sceneContainer.name}_textures.asset");
        }

        string assetPath = $"Assets/Resources/RenderScenes/{sceneContainer.name}_textures.asset"; 
        AssetDatabase.CreateAsset(sceneTextures, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
