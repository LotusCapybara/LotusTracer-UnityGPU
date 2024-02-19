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
}