

using System.IO;
using UnityEngine;

public static class RenderSaver
{
    public static void SaveTexture(string textureName, RenderTexture rt)
    {
        string path = Application.dataPath + "/Renders/";

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D text = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        text.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        text.Apply();
        
        RenderTexture.active = currentRT;
        
        byte[] bytes = text.EncodeToPNG();
        
        File.WriteAllBytes($"{path}{textureName}.png", bytes);
        
        Object.Destroy(text);
    }
}