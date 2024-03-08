using System.Collections.Generic;
using UnityEngine;

public enum ERenderTextureType
{
    Final, SamplerBuffer, SamplerBufferPrev, Debug,
    BloomBrightness, BloomBrightnessBlur, PostProcessInput_1, PostProcessOutput_1
}

public enum EDebugBufferType
{
    Color, Normal, Rough, Metallic, Emissive, SchlickWeight, SchlickFresnel, SchlickFresnelColor, 
    DielectricFresnel, BVHDensity, Dist_D, Dist_GV, Dist_GL,
    Eval_Diffuse, Eval_Reflect
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
            
            { ERenderTextureType.SamplerBuffer, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.SamplerBufferPrev, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.Debug, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
           
            { ERenderTextureType.BloomBrightness, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.BloomBrightnessBlur, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.PostProcessInput_1, 
                ShaderUtils.Create(width, height, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false)},
            
            { ERenderTextureType.PostProcessOutput_1, 
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