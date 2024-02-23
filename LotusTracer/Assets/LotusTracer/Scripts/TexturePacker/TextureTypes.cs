using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class TextureDataManaged 
{
    [FormerlySerializedAs("index")]
    public int originalIndex;
    public int atlasIndex;
    public int x;
    public int y;
    public int width;
    public int height;

    public TextureData GetStruct()
    {
        return new TextureData
        {
            index = this.originalIndex,
            atlasIndex = this.atlasIndex,
            x = this.x,
            y = this.y,
            width = this.width,
            height = this.height
        };
    }
}

[Serializable]
public struct TextureData 
{
    public int index;
    public int atlasIndex;
    public int x;
    public int y;
    public int width;
    public int height;
}


[Serializable]
public class TextureAtlasData
{
    public List<int> textureIds = new();
    public List<TextureDataManaged> textureDatas = new();
    public byte[] texture;
}
