using System.Collections.Generic;
using System.IO;
using CapyTracerCore.Tracer;
using UnityEngine;

namespace CapyTracerCore.Core
{
    public class RenderScene
    {
        public RenderRay[] cameraRays;
        public int width, height, totalPixels, maxBounces;
        
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

        public SerializedScene serializedScene;

        public void Load(string sceneName, int width, int height, int maxBounces)
        {
            this.width = width;
            this.height = height;
            this.maxBounces = maxBounces;
            totalPixels = width * height;

            string scenePath = Path.Combine(Application.dataPath, $"Resources/RenderScenes/{sceneName}.dat");
            serializedScene = SceneExporter.DeserializeScene(scenePath);

            for (int m = 0; m < serializedScene.materials.Length; m++)
            {
                var mat = serializedScene.materials[m];
                mat.GenerateRuntime();
                serializedScene.materials[m] = mat;
            }
            
            int totalPixes = width * height;
            
            // -------------- generation of rays
            renderCamera= new RenderCamera(width, height, serializedScene.camera);
            cameraRays = new RenderRay[totalPixes];
            UpdateCameraRays();
            
            // create scene textureDatas
            RenderSceneTextures textures = Resources.Load<RenderSceneTextures>($"RenderScenes/{sceneName}_textures");

            textureArrayAlbedo = CreateTextureArray(4096, 4096, textures.GetAlbedoCanvasTextures(), AtlasFormats.ALBEDO);
            textureDataAlbedo = textures.albedoTextureDatas.ToArray();
            
            textureArrayNormal = CreateTextureArray(4096, 4096, textures.GetNormalCanvasTextures(), AtlasFormats.NORMAL);
            textureDataNormal = textures.normalTextureDatas.ToArray();
            
            textureArrayRoughness = CreateTextureArray(4096, 4096, textures.GetRoughnessCanvasTextures(), AtlasFormats.ROUGHNESS);
            textureDataRoughness = textures.roughTextureDatas.ToArray();
            
            textureArrayMetallic = CreateTextureArray(4096, 4096, textures.GetMetallicCanvasTextures(), AtlasFormats.METALLIC);
            textureDataMetallic = textures.metalTextureDatas.ToArray();
            
            textureArrayEmission = CreateTextureArray(4096, 4096, textures.GetEmissionCanvasTextures(), AtlasFormats.EMISSION);
            textureDataEmission = textures.emissionTextureDatas.ToArray();
        }

        public void UpdateCameraRays()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cameraRays[y * width + x] = renderCamera.GetRay(x, y);
                }
            }
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