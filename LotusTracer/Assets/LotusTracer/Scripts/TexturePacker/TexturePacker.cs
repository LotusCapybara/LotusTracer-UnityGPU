using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CapyTracerCore.Core;
using UnityEditor;
using UnityEngine;

public class TexturePacker
{
    private Dictionary<TextureFormat, TextureImporterFormat> s_formatToFormat =
        new()
        {
            { TextureFormat.RGBA32 , TextureImporterFormat.RGBA32}, 
            { TextureFormat.RGB24 , TextureImporterFormat.RGB24}, 
            { TextureFormat.RGBAFloat , TextureImporterFormat.RGBAFloat},
            { TextureFormat.R8 , TextureImporterFormat.R8}
        };
    
    private List<Texture2D> _originalTextures = new();
    private int _atlasWidth;
    private int _atlasHeight;

    public List<TextureDataManaged> allTextureDatas;
    public List<TextureAtlasData> atlases;

    private TextureFormat _format;

    private string _setName;
    private bool _isNormal;
    
    public TexturePacker(TextureFormat format, string setName, bool isNormal)
    {
        _setName = setName;
        _isNormal = isNormal;
        _atlasWidth = 4096;
        _atlasHeight = 4096;
        _format = format;
    }

    public List<TextureData> GetDatasAsStruct()
    {
        List<TextureData> datas = new List<TextureData>();

        foreach (var textureDataManaged in allTextureDatas)
        {
            datas.Add(textureDataManaged.GetStruct());
        }

        return datas;
    }

    public int GetAtlasIndexForTextureId(int textureId)
    {
        if (textureId < 0)
            return -1;
        
        foreach (var textureData in allTextureDatas)
        {
            if (textureData.originalIndex == textureId)
                return textureData.atlasIndex;
        }

        throw new Exception("texture not found");
    }
    
    public int GetTextureIndexInsideAtlasFromOriginalIndex(int originalIndex)
    {
        if (originalIndex < 0)
            return -1;
        
        foreach (var textureData in allTextureDatas)
        {
            if (textureData.originalIndex == originalIndex)
                return allTextureDatas.IndexOf(textureData);
        }

        throw new Exception("texture not found");
    }
    


    public void PackTextures(List<Texture2D> textures)
    {
        _originalTextures.AddRange(textures);
        allTextureDatas = new List<TextureDataManaged>();

        for (int i = 0; i < _originalTextures.Count; i++)
        {
            TextureDataManaged dataManaged = new TextureDataManaged();
            dataManaged.originalIndex = i;
            dataManaged.width = _originalTextures[i].width;
            dataManaged.height = _originalTextures[i].height;

            allTextureDatas.Add(dataManaged);
        }

        allTextureDatas.Sort((a, b) => a.height.CompareTo(b.height));

        int xPos = 0;
        int yPos = 0;
        int largestHThisRow = 0;

        atlases = new List<TextureAtlasData>();
        TextureAtlasData currentAtlas = new TextureAtlasData();
        int atlasIndex = 0;

        atlases.Add(currentAtlas);

        List<TextureDataManaged> datasToPack = new List<TextureDataManaged>(allTextureDatas);

        while (datasToPack.Count > 0)
        {
            List<TextureDataManaged> packedDatas = new List<TextureDataManaged>();

            foreach (TextureDataManaged textureData in datasToPack)
            {
                // loop to the start of the row if the size went out of the canvas width
                if ((xPos + textureData.width) > _atlasWidth)
                {
                    yPos += largestHThisRow;
                    xPos = 0;
                    largestHThisRow = 0;
                }

                if ((yPos + textureData.height) > _atlasHeight)
                {
                    continue;
                }

                textureData.atlasIndex = atlasIndex;
                textureData.x = xPos;
                textureData.y = yPos;
                xPos += textureData.width;

                if (textureData.height > largestHThisRow)
                    largestHThisRow = textureData.height;

                packedDatas.Add(textureData);
            }

            foreach (TextureDataManaged textureData in packedDatas)
            {
                currentAtlas.textureIds.Add(textureData.originalIndex);
                currentAtlas.textureDatas.Add(textureData);
                datasToPack.Remove(textureData);
            }

            if (packedDatas.Count == 0)
            {
                currentAtlas = new TextureAtlasData();
                atlasIndex++;
                atlases.Add(currentAtlas);
                xPos = 0;
                yPos = 0;
                largestHThisRow = 0;
            }
        }

        atlasIndex = 0;
        foreach (var atlas in atlases)
        {
            Color[] pixels = new Color[_atlasWidth * _atlasHeight];
            

            foreach (var tId in atlas.textureIds)
            {
                var texturePixels = _originalTextures[tId].GetPixels();

                var textureData = allTextureDatas.First(d => d.originalIndex == tId);

                for (int y = 0; y < textureData.height; y++)
                {
                    for (int x = 0; x < textureData.width; x++)
                    {
                        int pixelIndex = y * textureData.width + x;
                        int targetX = x + textureData.x;
                        int targetY = y + textureData.y;
                        int targetIndex = targetY * _atlasWidth + targetX;

                        pixels[targetIndex] = texturePixels[pixelIndex];
                    }
                }
            }

            Texture2D atlasTxt = new Texture2D(_atlasWidth, _atlasHeight, _format, false);
            atlasTxt.SetPixels(pixels);
            atlasTxt.Apply();

            string jpgPath = SceneExporter.SCENES_PATH_BASE + $"/{_setName}_{atlasIndex}.jpg";
            string textAsstPath =  $"Assets/Resources/RenderScenes/{SceneExporter.SCENE_NAME}/{_setName}_{atlasIndex}.jpg";
            
            File.WriteAllBytes(jpgPath, atlasTxt.EncodeToJPG());
            AssetDatabase.Refresh();

            AssetDatabase.ImportAsset(jpgPath, ImportAssetOptions.ForceUpdate);
            
            TextureImporter importer = AssetImporter.GetAtPath(textAsstPath) as TextureImporter;
            
            TextureImporterPlatformSettings importerPlatSettings = new TextureImporterPlatformSettings();
            importerPlatSettings.format = s_formatToFormat[_format];
            importerPlatSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importerPlatSettings.maxTextureSize = 4096;

            TextureImporterSettings importerSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.mipmapEnabled = false;
            importerSettings.textureType = _isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importerSettings.filterMode = FilterMode.Bilinear;

            
            importer.SetTextureSettings(importerSettings);
            importer.SetPlatformTextureSettings(importerPlatSettings);
            importer.SaveAndReimport();
            
            atlas.resourcePath = $"RenderScenes/{SceneExporter.SCENE_NAME}/{_setName}_{atlasIndex}";
            atlasIndex++;
        }
    }

    public string GetDebugText(string setName)
    {
        string debugText = $"---------- {setName}  ----------\n";
        debugText += $"Total Atlases: {atlases.Count}\n";
        debugText += $"Textures: {atlases.Count}\n";
        
        for (int i = 0; i < atlases.Count; i++)
        {
            debugText += $"{i}:\n";
            
            foreach (var textureId in atlases[i].textureIds)
            {
                debugText += $" - {_originalTextures[textureId].name}   {_originalTextures[textureId].format} \n";
            }
            
            foreach (var textureData in atlases[i].textureDatas)
            {
                debugText += $" - at:{textureData.atlasIndex} oi:{textureData.originalIndex} x:{textureData.x} y:{textureData.y} w:{textureData.width} h:{textureData.height} \n";
            }
        }

        debugText += "\n\n";
        
        return debugText;
    }
    
    // public void GenerateDebugFiles(string setName)
    // {
    //     int channelsQty = AtlasFormats.s_channelsByFormat[_format]; 
    //     
    //     for (int i = 0; i < atlases.Count; i++)
    //     {
    //         Texture2D texture = new Texture2D(_atlasWidth, _atlasHeight, TextureFormat.RGBA32, false);
    //
    //         int size = _atlasWidth * _atlasHeight;
    //         Color[] colors = new Color[size]; 
    //         
    //         for (int p = 0; p < size; p ++)
    //         {
    //             float r = channelsQty > 0 ? atlases[i].texture[p * channelsQty] / 255f : 0f;
    //             float g = channelsQty > 1 ? atlases[i].texture[p * channelsQty + 1] / 255f : 0f;
    //             float b = channelsQty > 2 ? atlases[i].texture[p * channelsQty + 2] / 255f : 0f;
    //             float a = channelsQty > 3 ? atlases[i].texture[p * channelsQty + 3] / 255f : 1f;
    //             
    //             colors[p] = new Color(r, g, b, a);
    //         }
    //         
    //         texture.SetPixels(colors);
    //         File.WriteAllBytes(SceneExporter.SCENES_PATH_BASE + $"/{setName}_{i}.jpg", texture.EncodeToJPG());
    //     }
    // }
}
