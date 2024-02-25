using System;
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
    public static void Export(SerializedScene scene, GameObject sceneContainer, List<Material> unityMaterials, bool generateDebugInfo)
    {
        // todo: packing mono atlases would be nice, for instance
        // rgba = rough, metallic, emissive, something
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

            int flags = 0;
            
            int diffuseModel = unityMaterials[m].GetInteger("_DiffuseModel");
            flags |= (0b1 <<  diffuseModel );
            
            // Debug.LogError($"---- {unityMaterials[m].name} ----" );
            // Debug.LogError($"total {flags}" );
            // Debug.LogError($"lambert: { ((flags & 0b1) == 1).ToString() }   b { Convert.ToString(flags, 2) }" );
            // Debug.LogError($"oren nayar: { (( (flags >> 1) & 0b1) == 1).ToString() } b { Convert.ToString(flags, 2) }" );
            
            
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
                maxScatteringDistance = maxScatteringDistance,
                flags = flags
            };
            
            scene.materials[m] = renderMaterial;
        }

        // albedo atlases
        TexturePacker packerAlbedos = new TexturePacker(AtlasFormats.FULL_COLOR, "albedo", false);
        packerAlbedos.PackTextures(texturesAlbedo);
        sceneTextures.albedoAtlases = packerAlbedos.atlases;
        sceneTextures.albedoTextureDatas = packerAlbedos.GetDatasAsStruct();

        // normal atlases
        TexturePacker packerNormals = new TexturePacker(AtlasFormats.NORMAL, "normal", true);
        packerNormals.PackTextures(texturesNormals);
        sceneTextures.normalAtlases = packerNormals.atlases;
        sceneTextures.normalTextureDatas = packerNormals.GetDatasAsStruct();
        
        // rough atlases
        TexturePacker packerRough = new TexturePacker(AtlasFormats.R_CHANNEL_ONLY, "rough", false);
        packerRough.PackTextures(texturesRough);
        sceneTextures.roughAtlases = packerRough.atlases;
        sceneTextures.roughTextureDatas = packerRough.GetDatasAsStruct();

        // metallic atlases
        TexturePacker packerMetal = new TexturePacker(AtlasFormats.R_CHANNEL_ONLY, "metal", false);
        packerMetal.PackTextures(texturesMetallic);
        sceneTextures.metalAtlases = packerMetal.atlases;
        sceneTextures.metalTextureDatas = packerMetal.GetDatasAsStruct();
        
        // emission atlases
        TexturePacker packerEmission = new TexturePacker(AtlasFormats.R_CHANNEL_ONLY, "emission", false);
        packerEmission.PackTextures(texturesEmission);
        sceneTextures.emissionAtlases = packerEmission.atlases;
        sceneTextures.emissionTextureDatas = packerEmission.GetDatasAsStruct();
        
        for (int m = 0; m < scene.materials.Length; m++)
        {
            var mat = scene.materials[m];
            
            mat.albedoMapCanvasIndex = packerAlbedos.GetAtlasIndexForTextureId(mat.albedoMapIndex);
            mat.albedoMapIndex = packerAlbedos.GetTextureIndexInsideAtlasFromOriginalIndex(mat.albedoMapIndex);
            
            mat.normalMapCanvasIndex = packerNormals.GetAtlasIndexForTextureId(mat.normalMapIndex);
            mat.normalMapIndex = packerNormals.GetTextureIndexInsideAtlasFromOriginalIndex(mat.normalMapIndex);
            
            mat.roughMapCanvasIndex = packerRough.GetAtlasIndexForTextureId(mat.roughMapIndex);
            mat.roughMapIndex = packerRough.GetTextureIndexInsideAtlasFromOriginalIndex(mat.roughMapIndex);
            
            mat.metalMapCanvasIndex = packerMetal.GetAtlasIndexForTextureId(mat.metalMapIndex);
            mat.metalMapIndex = packerMetal.GetTextureIndexInsideAtlasFromOriginalIndex(mat.metalMapIndex);
            
            mat.emissionMapCanvasIndex = packerEmission.GetAtlasIndexForTextureId(mat.emissionMapIndex);
            mat.emissionMapIndex = packerEmission.GetTextureIndexInsideAtlasFromOriginalIndex(mat.emissionMapIndex);

            scene.materials[m] = mat;
        }
        
        if (File.Exists(Application.dataPath + $"/Resources/RenderScenes/{sceneContainer.name}_textures.asset"))
        {
            AssetDatabase.DeleteAsset($"Assets/Resources/RenderScenes/{sceneContainer.name}_textures.asset");
        }

        string assetPath = $"Assets/Resources/RenderScenes/{sceneContainer.name}/{sceneContainer.name}_textures.asset"; 
        AssetDatabase.CreateAsset(sceneTextures, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // it's a bit annoying to make this method so big due additional debug generation but
        // otherwise I'd need to rethink this class and see how to inject into the debug generator the data
        // I think it's fine like this

        if (generateDebugInfo)
        {
            // generate textures that show how the atlases were generated
            // packerAlbedos.GenerateDebugFiles("albedo");
            // packerNormals.GenerateDebugFiles("normal");
            // packerRough.GenerateDebugFiles("rough");
            // packerMetal.GenerateDebugFiles("metal");
            // packerEmission.GenerateDebugFiles("emission");
            
            // dump log file with information of generated atlases
            string atlasText = "";

            atlasText +=  packerAlbedos.GetDebugText("albedo");
            atlasText +=  packerNormals.GetDebugText("normal");
            atlasText +=  packerRough.GetDebugText("rough");
            atlasText +=  packerMetal.GetDebugText("metal");
            atlasText +=  packerEmission.GetDebugText("emission");
            
            File.WriteAllText(SceneExporter.SCENES_PATH_BASE + "Atlases.txt", atlasText);
            
            // dump log file with information of generated materials
            string materialsText = "";
            
            materialsText += $"Total Materials: {scene.materials.Length}\n\n";

            for (int m = 0; m < scene.materials.Length; m++)
            {
                var mat = scene.materials[m];
                
                materialsText += $"Mat: {m}   { unityMaterials[m].name }\n";
                materialsText += $"Canvas Albedo: {mat.albedoMapCanvasIndex}\n";
                materialsText += $"Canvas Normal: {mat.normalMapCanvasIndex}\n";
                materialsText += $"Canvas Rough: {mat.roughMapCanvasIndex}\n";
                materialsText += $"Canvas Metal: {mat.metalMapCanvasIndex}\n";
                materialsText += $"Canvas Emission: {mat.emissionMapCanvasIndex}\n";
                
                materialsText += "----------------\n\n";
            }
            
            File.WriteAllText(SceneExporter.SCENES_PATH_BASE + "Materials.txt", materialsText);
            
            
        }
    }
}
