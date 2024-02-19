using System.Collections.Generic;
using UnityEngine;

public enum ERenderTextureType
{
    Final, SamplerBuffer, SamplerBufferPrev, Debug, 
    
    ColorBuffer, NormalBuffer, RoughnessBuffer, MetallicBuffer, EmissiveBuffer, 
    
    BVHDensity
    
}


public class TracerTextures
{
    public Dictionary<ERenderTextureType, RenderTexture> textures;

    public TracerTextures(int width, int height)
    {
        textures = new Dictionary<ERenderTextureType, RenderTexture>
        {
            { ERenderTextureType.Final, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, false)},
            
            { ERenderTextureType.Debug, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.SamplerBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.SamplerBufferPrev, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.ColorBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, false)},
            
            { ERenderTextureType.NormalBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.RoughnessBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, false)},
            
            { ERenderTextureType.MetallicBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, false)},
            
            { ERenderTextureType.EmissiveBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, false)},
            
            { ERenderTextureType.BVHDensity, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
        };
        
        // Initialize all black
        ResetTextures();
    }

    public void Dispose()
    {
        foreach (var kvp in textures)
        {
            kvp.Value?.Release();
        }
    }

    public void ResetTextures()
    {
        foreach (var kvp in textures)
        {
            RenderTexture.active = kvp.Value;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }
    }
    
}