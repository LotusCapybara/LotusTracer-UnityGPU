using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TexturePacker
{
    private List<Texture2D> _textures = new();
    private int _atlasWidth;
    private int _atlasHeight;

    public List<TextureDataManaged> allTextureDatas;
    public List<TextureAtlasData> atlases;

    private TextureFormat _format;

    public TexturePacker(TextureFormat format)
    {
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
        foreach (var textureData in allTextureDatas)
        {
            if (textureData.index == textureId)
                return textureData.atlasIndex;
        }

        return -1;
    }

    public void RegisterTextures(List<Texture2D> textures)
    {
        _textures.AddRange(textures);
    }

    public void PackTextures()
    {
        allTextureDatas = new List<TextureDataManaged>();

        for (int i = 0; i < _textures.Count; i++)
        {
            TextureDataManaged dataManaged = new TextureDataManaged();
            dataManaged.index = i;
            dataManaged.width = _textures[i].width;
            dataManaged.height = _textures[i].height;

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

            foreach (var textureData in datasToPack)
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

            foreach (var textureData in packedDatas)
            {
                currentAtlas.textureIds.Add(textureData.index);
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

        foreach (var atlas in atlases)
        {
            Color[] atlasPixels = new Color[(_atlasWidth * _atlasHeight) * 3];


            foreach (var tId in atlas.textureIds)
            {
                var texturePixels = _textures[tId].GetPixels();

                var textureData = allTextureDatas.First(d => d.index == tId);

                for (int y = 0; y < textureData.height; y++)
                {
                    for (int x = 0; x < textureData.width; x++)
                    {
                        int pixelIndex = y * textureData.height + x;
                        int targetX = x + textureData.x;
                        int targetY = y + textureData.y;
                        int targetIndex = targetY * _atlasWidth + targetX;

                        atlasPixels[targetIndex] = texturePixels[pixelIndex];
                    }
                }
            }

            Texture2D texture = new Texture2D(_atlasWidth, _atlasHeight, _format, false);
            texture.SetPixels(atlasPixels);
            atlas.texture = texture.EncodeToJPG();
        }
    }
}
