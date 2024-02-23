using System.Collections.Generic;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public class RenderSceneTextures : ScriptableObject
    {
        public List<TextureData> albedoTextureDatas = new();
        public List<TextureAtlasData> albedoAtlases = new();

        public List<TextureData> normalTextureDatas = new();
        public List<TextureAtlasData> normalAtlases = new();

        public List<TextureData> roughTextureDatas = new();
        public List<TextureAtlasData> roughAtlases = new();
        
        public List<TextureData> metalTextureDatas = new();
        public List<TextureAtlasData> metalAtlases = new();
        
        public List<TextureData> emissionTextureDatas = new();
        public List<TextureAtlasData> emissionAtlases = new();
        
        public List<Texture> GetAlbedoCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();

            for (int i = 0; i < albedoAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.FULL_COLOR, false);
                texture.LoadRawTextureData(albedoAtlases[i].texture);
                maps.Add(texture);
            }

            return maps;
        }
        
        public List<Texture> GetNormalCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < normalAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.FLOAT_COLOR, false);

                Color[] colors = new Color[4096 * 4096];

                for (int p = 0; p < 4096 * 4096; p ++)
                {
                    colors[p] = new Color(
                        normalAtlases[i].texture[p * 4] / 255f, 
                        normalAtlases[i].texture[p * 4 + 1] / 255f, 
                        normalAtlases[i].texture[p * 4 + 2] / 255f, 
                        normalAtlases[i].texture[p * 4 + 3] / 255f);
                    
                    // colors[p] = new Color(0.5f, 0.5f, 1f, 1f);
                }
                
                texture.SetPixels(colors);
                texture.Apply();
                maps.Add(texture);
            }
        
            return maps;
        }
        
        public List<Texture> GetRoughnessCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < roughAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.R_CHANNEL_ONLY, false);
                texture.LoadRawTextureData(roughAtlases[i].texture);
                
                maps.Add(texture);
            }
        
            return maps;
        }
        
        public List<Texture> GetMetallicCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < metalAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.R_CHANNEL_ONLY, false);
                texture.LoadRawTextureData(metalAtlases[i].texture);
                
                maps.Add(texture);
            }
        
            return maps;
        }
        
        public List<Texture> GetEmissionCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < emissionAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.R_CHANNEL_ONLY, false);
                texture.LoadRawTextureData(emissionAtlases[i].texture);
                
                maps.Add(texture);
            }
        
            return maps;
        }
        
    }
}