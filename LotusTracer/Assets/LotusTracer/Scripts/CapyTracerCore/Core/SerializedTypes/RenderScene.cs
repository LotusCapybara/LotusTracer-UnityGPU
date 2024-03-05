using System.Collections.Generic;
using System.IO;
using CapyTracerCore.Tracer;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public class RenderScene
    {
        public int width, height, totalPixels;
        public int depthDiffuse, depthSpecular, depthTransmission;
        
        public Texture2DArray textureArrayAlbedo;
        public TextureData[] textureDataAlbedo;
        
        public Texture2DArray textureArrayNormal;
        public TextureData[] textureDataNormal;
        
        public Texture2DArray textureArrayRoughness;
        public TextureData[] textureDataRoughness;
        
        public Texture2DArray textureArrayMetallic;
        public TextureData[] textureDataMetallic;
        
        public Texture2DArray textureArrayEmission;
        public TextureData[] textureDataEmission;
        
        public RenderCamera renderCamera;

        public SerializedScene_Data sceneData;
        public SerializedScene_Geometry sceneGeom;

        public void Load(string sceneName, int width, int height, int diffDepth, int specDepth, int transDepth)
        {
            this.width = width;
            this.height = height;
            this.depthDiffuse = diffDepth;
            this.depthSpecular = specDepth;
            this.depthTransmission = transDepth;
            totalPixels = width * height;

            string pathData = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.dat");
            string pathGeometry = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}/{sceneName}.geom");
            (sceneData, sceneGeom) = SceneExporter.DeserializeScene(pathData, pathGeometry);

            for (int m = 0; m < sceneData.materials.Length; m++)
            {
                var mat = sceneData.materials[m];
                mat.GenerateRuntime();
                sceneData.materials[m] = mat;
            }
            
            // -------------- generation of rays
            renderCamera= new RenderCamera(width, height, sceneData.camera);
            
            // create scene textureDatas
            RenderSceneTextures textures = Resources.Load<RenderSceneTextures>($"RenderScenes/{sceneName}/{sceneName}_textures");

            textureArrayAlbedo = CreateTextureArray(4096, 4096, textures.GetAlbedoCanvasTextures(), AtlasFormats.FULL_COLOR);
            textureDataAlbedo = textures.albedoTextureDatas.ToArray();
            
            textureArrayNormal = CreateTextureArray(4096, 4096, textures.GetNormalCanvasTextures(), AtlasFormats.NORMAL);
            textureDataNormal = textures.normalTextureDatas.ToArray();
            
            textureArrayRoughness = CreateTextureArray(4096, 4096, textures.GetRoughnessCanvasTextures(), AtlasFormats.R_CHANNEL_ONLY);
            textureDataRoughness = textures.roughTextureDatas.ToArray();
            
            textureArrayMetallic = CreateTextureArray(4096, 4096, textures.GetMetallicCanvasTextures(), AtlasFormats.R_CHANNEL_ONLY);
            textureDataMetallic = textures.metalTextureDatas.ToArray();
            
            textureArrayEmission = CreateTextureArray(4096, 4096, textures.GetEmissionCanvasTextures(), AtlasFormats.R_CHANNEL_ONLY);
            textureDataEmission = textures.emissionTextureDatas.ToArray();
        }

        private Texture2DArray CreateTextureArray(int w, int h, List<Texture> fromTextures, TextureFormat format)
        {
            if (fromTextures.Count == 0)
            {                
                fromTextures.Add(GetEmptyTexture(w, h, format));
            }
        
            var textureArray = new Texture2DArray(w, h, fromTextures.Count, format, false, false);
            int te = 0;
            foreach (var texturesTexture in fromTextures)
            {
                Graphics.CopyTexture(texturesTexture, 0, 0, textureArray, te++, 0);
            }
            textureArray.Apply();

            return textureArray;
        }

        private Texture2D GetEmptyTexture(int w, int h, TextureFormat format)
        {
            var emptyTexture = new Texture2D(w, h, format, false);
            var pixels = emptyTexture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }
                                        
            return emptyTexture;
        }
        
    }
}