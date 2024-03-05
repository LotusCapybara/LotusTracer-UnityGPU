using CapyTracerCore.Core;
using UnityEngine;

public class ComputeShaderHolder_MegaKernel : ComputeShaderHolder
{
    public const string KERNEL_MEGA_PATH_TRACE = "Kernel_MegaPathTrace";
    public const string KERNEL_ACCUMULATE_FINAL = "Kernel_AccumulateFinalTexture";

    public ComputeShaderHolder_MegaKernel(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures) : 
        base(shaderName, renderScene, buffers, tracerTextures)
    {
    }

    protected override void Initialize()
    {
        // kernels
        _kernelIds.Add(KERNEL_MEGA_PATH_TRACE, _shader.FindKernel(KERNEL_MEGA_PATH_TRACE));
        _kernelIds.Add(KERNEL_ACCUMULATE_FINAL, _shader.FindKernel(KERNEL_ACCUMULATE_FINAL));
        
        // shader variables
        _shader.SetInt("width", _scene.width);
        _shader.SetInt("height", _scene.height);
        _shader.SetInt("totalSize", _scene.totalPixels);
        _shader.SetInt("_depthDiffuse", _scene.depthDiffuse);
        _shader.SetInt("_depthSpecular", _scene.depthSpecular);
        _shader.SetInt("_depthTransmission", _scene.depthTransmission);
        _shader.SetInt("totalTriangles", _scene.serializedScene.qtyTriangles);
        _shader.SetInt("qtyDirectLights", _scene.serializedScene.qtyLights);
        _shader.SetInt("treeNodesQty", _scene.serializedScene.qtyBVHNodes);
        _shader.SetInt("totalMaterials", _scene.serializedScene.qtyMaterials);
        
        // adding all the texture maps
        _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesAlbedo", _scene.textureArrayAlbedo);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_ALBEDO);
        
        _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesNormal", _scene.textureArrayNormal);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_NORMAL);
        
        _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesRoughness", _scene.textureArrayRoughness);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_ROUGHNESS);

        _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesMetallic", _scene.textureArrayMetallic);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_METALLIC);
        
        _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesEmission", _scene.textureArrayEmission);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_EMISSION);
        
        // Mega Kernel
        SetTexture(KERNEL_MEGA_PATH_TRACE, "_SamplingBuffer", ERenderTextureType.SamplerBuffer);
        SetTexture(KERNEL_MEGA_PATH_TRACE, "_DebugTexture", ERenderTextureType.Debug);
        
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.TRIANGLE_VERTICES);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.TRIANGLE_DATAS);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.BVH_TREE);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MATERIALS);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.LIGHTS);
        
        // Accumulation Kernel Buffers
        SetTexture(KERNEL_ACCUMULATE_FINAL, "_SamplingBuffer", ERenderTextureType.SamplerBuffer);
        SetTexture(KERNEL_ACCUMULATE_FINAL, "_SamplingBufferPrev", ERenderTextureType.SamplerBufferPrev);
        SetTexture(KERNEL_ACCUMULATE_FINAL, "_LDRFinalBuffer", ERenderTextureType.Final);
    }
}