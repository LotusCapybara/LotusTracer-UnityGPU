using CapyTracerCore.Core;
using UnityEngine;

public class ComputeShaderHolder_MegaKernel : ComputeShaderHolder
{
    public const string KERNEL_MEGA_PATH_TRACE = "Kernel_MegaPathTrace";

    public ComputeShaderHolder_MegaKernel(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures) : 
        base(shaderName, renderScene, buffers, tracerTextures)
    {
    }

    protected override void Initialize()
    {
        // kernels
        _kernelIds.Add(KERNEL_MEGA_PATH_TRACE, _shader.FindKernel(KERNEL_MEGA_PATH_TRACE));
        
        // shader variables
        _shader.SetInt("width", _scene.width);
        _shader.SetInt("height", _scene.height);
        _shader.SetInt("totalSize", _scene.totalPixels);
        _shader.SetInt("_depthDiffuse", _scene.depthDiffuse);
        _shader.SetInt("_depthSpecular", _scene.depthSpecular);
        _shader.SetInt("_depthTransmission", _scene.depthTransmission);
        _shader.SetInt("totalTriangles", _scene.sceneGeom.qtyTriangles);
        _shader.SetInt("qtyDirectLights", _scene.sceneData.qtyLights);
        _shader.SetInt("treeNodesQty", _scene.sceneGeom.qtyBVHNodes);
        _shader.SetInt("totalMaterials", _scene.sceneData.qtyMaterials);
        _shader.SetVector("ambientLightColor", _scene.ambientLightColor);
        _shader.SetFloat("ambientLightPower", _scene.ambientLightPower);   
        
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
        
        _shader.SetInt("_HasCubeMap", _scene.cubeMap != null ? 1:0);
        _shader.SetInt("_IgnoreCubeInImage", _scene.ignoreCubeInImage ? 1:0);
        
        if(_scene.cubeMap != null)
            _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_CubeMap", _scene.cubeMap);
        else
            _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_CubeMap", new Cubemap(64, TextureFormat.RGB24, 0));
        
        
        
        // Mega Kernel
        SetTexture(KERNEL_MEGA_PATH_TRACE, "_SamplingBuffer", ERenderTextureType.SamplerBuffer);
        SetTexture(KERNEL_MEGA_PATH_TRACE, "_SamplingBufferPrev", ERenderTextureType.SamplerBufferPrev);
        SetTexture(KERNEL_MEGA_PATH_TRACE, "_DebugTexture", ERenderTextureType.Debug);
        
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.TRIANGLE_VERTICES);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.TRIANGLE_DATAS);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.BVH_TREE);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MATERIALS);
        SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.LIGHTS);
    }
}