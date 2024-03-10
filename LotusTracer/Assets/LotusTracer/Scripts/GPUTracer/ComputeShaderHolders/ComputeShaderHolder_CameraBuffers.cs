using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class ComputeShaderHolder_CameraBuffers : ComputeShaderHolder
{
    public const string KERNEL_DEBUG_TEXTURES = "Kernel_CameraDebugTextures";

    public ComputeShaderHolder_CameraBuffers(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures) : 
        base(shaderName, renderScene, buffers, tracerTextures)
    {
    }

    protected override void Initialize()
    {
        // kernels
        _kernelIds.Add(KERNEL_DEBUG_TEXTURES, _shader.FindKernel(KERNEL_DEBUG_TEXTURES));
        
        // shader variables
        _shader.SetInt("width", _scene.width);
        _shader.SetInt("height", _scene.height);
        _shader.SetInt("totalSize", _scene.totalPixels);
        _shader.SetInt("_depthDiffuse", _scene.depthDiffuse);
        _shader.SetInt("_depthSpecular", _scene.depthSpecular);
        _shader.SetInt("_depthTransmission", _scene.depthTransmission);
        _shader.SetInt("totalTriangles", _scene.sceneData.qtyLights);
        _shader.SetInt("treeNodesQty", _scene.sceneGeom.qtyBVHNodes);
        _shader.SetInt("totalMaterials", _scene.sceneData.qtyMaterials);
        _shader.SetInt("qtyDirectLights", _scene.sceneData.qtyLights);
        
        BoundsBox sceneBounds = new BoundsBox(_scene.sceneGeom.boundMin, _scene.sceneGeom.boundMax);
        sceneBounds.min -= new float3(0.1f, 0.1f, 0.1f);
        sceneBounds.max += new float3(0.1f, 0.1f, 0.1f);
        
        Vector3 sceneExtends = sceneBounds.GetSize();
        _shader.SetVector("_decompressionProduct", new Vector4(sceneExtends.x, sceneExtends.y, sceneExtends.z) / 65535f);
        
        
        SetTexture(KERNEL_DEBUG_TEXTURES, "_TextureDebugBuffer", ERenderTextureType.Debug);
        
        _shader.SetTexture(_kernelIds[KERNEL_DEBUG_TEXTURES], "_AtlasesAlbedo", _scene.textureArrayAlbedo);
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.MAP_DATA_ALBEDO);
        
        _shader.SetTexture(_kernelIds[KERNEL_DEBUG_TEXTURES], "_AtlasesNormal", _scene.textureArrayNormal);
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.MAP_DATA_NORMAL);
        
        _shader.SetTexture(_kernelIds[KERNEL_DEBUG_TEXTURES], "_AtlasesRoughness", _scene.textureArrayRoughness);
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.MAP_DATA_ROUGHNESS);

        _shader.SetTexture(_kernelIds[KERNEL_DEBUG_TEXTURES], "_AtlasesMetallic", _scene.textureArrayMetallic);
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.MAP_DATA_METALLIC);
        
        _shader.SetTexture(_kernelIds[KERNEL_DEBUG_TEXTURES], "_AtlasesEmission", _scene.textureArrayEmission);
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.MAP_DATA_EMISSION);
        
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.LIGHTS);
        
        SetBuffer(KERNEL_DEBUG_TEXTURES, BuffersNames.SCENE_BOUNDS);

        // buffers for each kernel
        foreach (var kvpKernels in _kernelIds)
        {
            SetBuffer(kvpKernels.Key, BuffersNames.TRIANGLE_VERTICES);
            SetBuffer(kvpKernels.Key, BuffersNames.TRIANGLE_DATAS);
            SetBuffer(kvpKernels.Key, BuffersNames.BVH_TREE);
            SetBuffer(kvpKernels.Key, BuffersNames.MATERIALS);
        }
    }
}