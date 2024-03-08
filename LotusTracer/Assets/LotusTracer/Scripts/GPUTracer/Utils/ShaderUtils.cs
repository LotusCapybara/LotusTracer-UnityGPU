using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class ShaderUtils
{
    public static RenderTexture Create(int width, int height, RenderTextureFormat textureFormat, RenderTextureReadWrite filterMode, bool useMipMaps)
    {
        RenderTexture rt  = new RenderTexture(width, height, 0, textureFormat, filterMode);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }
    
    public static RenderTexture GetTemporary(int width, int height, Color color, RenderTextureFormat format, RenderTextureReadWrite space)
    {
        RenderTexture rt  = RenderTexture.GetTemporary(width, height, 0, format, space);
        rt.enableRandomWrite = true;
        rt.Create();
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, color); 
        RenderTexture.active = activeRenderTexture;
        
        return rt;
    }
}