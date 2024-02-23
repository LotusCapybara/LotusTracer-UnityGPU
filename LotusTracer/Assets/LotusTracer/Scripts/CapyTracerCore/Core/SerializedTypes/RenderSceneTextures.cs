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
                maps.Add(Resources.Load<Texture2D>(albedoAtlases[i].resourcePath));
            }

            return maps;
        }
        
        public List<Texture> GetNormalCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < normalAtlases.Count; i++)
            {
                maps.Add(Resources.Load<Texture2D>(normalAtlases[i].resourcePath));
            }
        
            return maps;
        }
        
        public List<Texture> GetRoughnessCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < roughAtlases.Count; i++)
            {
                maps.Add(Resources.Load<Texture2D>(roughAtlases[i].resourcePath));
            }
        
            return maps;
        }
        
        public List<Texture> GetMetallicCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < metalAtlases.Count; i++)
            {
                maps.Add(Resources.Load<Texture2D>(metalAtlases[i].resourcePath));
            }
        
            return maps;
        }
        
        public List<Texture> GetEmissionCanvasTextures()
        {
            List<Texture> maps = new List<Texture>();
        
            for (int i = 0; i < emissionAtlases.Count; i++)
            {
                maps.Add(Resources.Load<Texture2D>(emissionAtlases[i].resourcePath));
            }
        
            return maps;
        }
        
    }
}