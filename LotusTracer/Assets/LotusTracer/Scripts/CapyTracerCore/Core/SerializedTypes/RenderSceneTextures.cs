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
        
        public List<Texture> GetAlbedoCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();

            for (int i = 0; i < albedoAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.ALBEDO, false);
                texture.LoadImage(albedoAtlases[i].texture);
                maps.Add(texture);
            }

            return maps;
        }
        
        public List<Texture> GetNormalCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < normalAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.NORMAL, false);
                texture.LoadImage(normalAtlases[i].texture);
                
                maps.Add(texture);
            }
        
            return maps;
        }
        
        public List<Texture> GetRoughnessCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < roughAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.ROUGHNESS, false);
                texture.LoadImage(roughAtlases[i].texture);
                
                maps.Add(texture);
            }
        
            return maps;
        }
        
        public List<Texture> GetMetallicCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < metalAtlases.Count; i++)
            {
                var texture = new Texture2D(4096, 4096, AtlasFormats.METALLIC, false);
                texture.LoadImage(metalAtlases[i].texture);
                
                maps.Add(texture);
            }
        
            return maps;
        }
    }
}