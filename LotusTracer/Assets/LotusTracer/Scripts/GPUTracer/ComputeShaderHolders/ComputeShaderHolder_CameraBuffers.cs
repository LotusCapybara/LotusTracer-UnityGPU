using CapyTracerCore.Core;

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
        _shader.SetInt("maxBounces", _scene.maxBounces);
        _shader.SetInt("totalTriangles", _scene.serializedScene.qtyLights);
        _shader.SetInt("treeNodesQty", _scene.serializedScene.qtyBVHNodes);
        _shader.SetInt("totalMaterials", _scene.serializedScene.qtyMaterials);
        
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
        

        // buffers for each kernel
        foreach (var kvpKernels in _kernelIds)
        {
            SetBuffer(kvpKernels.Key, BuffersNames.CAMERA_RAYS);
            SetBuffer(kvpKernels.Key, BuffersNames.TRIANGLES);
            SetBuffer(kvpKernels.Key, BuffersNames.BVH_TREE);
            SetBuffer(kvpKernels.Key, BuffersNames.MATERIALS);
        }
    }
}