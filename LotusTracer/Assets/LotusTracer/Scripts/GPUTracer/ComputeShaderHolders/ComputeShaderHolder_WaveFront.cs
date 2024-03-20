using System.Runtime.InteropServices;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class ComputeShaderHolder_WaveFront : ComputeShaderHolder
{
    public const string KERNEL_WF_INIT_ITERATION = "Kernel_WF_InitIteration";
    public const string KERNEL_WF_GENERATE_RAYS = "Kernel_WF_GenerateRays";
    public const string KERNEL_WF_INTERSECT_GEOMETRY = "Kernel_WF_IntersectGeometry";
    public const string KERNEL_WF_BOUNCE_BSDF = "Kernel_WF_BounceBsdf";
    public const string KERNEL_WF_ACCUMULATE_IMAGE_BUFFER = "Kernel_WF_AccumulateImageBuffer";
    
    
    
    private ComputeBuffer _bufferBufferSizes;
    private ComputeBuffer _bufferBounceRays;
    private ComputeBuffer _bufferBounceHits;
    private ComputeBuffer _bufferRadianceAcc;
    private ComputeBuffer _bufferBounceSamples;
    private ComputeBuffer _bufferThroughput;
    
    public ComputeShaderHolder_WaveFront(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures) : 
        base(shaderName, renderScene, buffers, tracerTextures)
    {
    }

    protected override void Initialize()
    {
        // kernels
        _kernelIds.Add(KERNEL_WF_INIT_ITERATION, _shader.FindKernel(KERNEL_WF_INIT_ITERATION));
        _kernelIds.Add(KERNEL_WF_GENERATE_RAYS, _shader.FindKernel(KERNEL_WF_GENERATE_RAYS));
        _kernelIds.Add(KERNEL_WF_INTERSECT_GEOMETRY, _shader.FindKernel(KERNEL_WF_INTERSECT_GEOMETRY));
        _kernelIds.Add(KERNEL_WF_BOUNCE_BSDF, _shader.FindKernel(KERNEL_WF_BOUNCE_BSDF));
        _kernelIds.Add(KERNEL_WF_ACCUMULATE_IMAGE_BUFFER, _shader.FindKernel(KERNEL_WF_ACCUMULATE_IMAGE_BUFFER));
        
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

        UpdateCameraGPUData();
        
        _bufferBufferSizes = new ComputeBuffer(30, Marshal.SizeOf<BufferSizes>());
        _bufferBounceRays = new ComputeBuffer(_scene.totalPixels, Marshal.SizeOf<RenderRay>());
        _bufferBounceHits = new ComputeBuffer(_scene.totalPixels, Marshal.SizeOf<BounceHitInfo>());
        _bufferRadianceAcc = new ComputeBuffer(_scene.totalPixels, Marshal.SizeOf<float4>());
        _bufferBounceSamples = new ComputeBuffer(_scene.totalPixels, Marshal.SizeOf<RenderRay>());
        _bufferThroughput = new ComputeBuffer(_scene.totalPixels, Marshal.SizeOf<float4>());
        
        InitKernel_InitIteration();
        InitKernel_GenerateRays();
        InitKernel_IntersectGeometry();
        InitKernel_BounceBsdf();
        InitKernel_AccumulateImageBuffer();
    }

    public override void Dispose()
    {
        _bufferBufferSizes.Dispose();
        _bufferBounceRays.Dispose();
        _bufferBounceHits.Dispose();
        _bufferRadianceAcc.Dispose();
        _bufferBounceSamples.Dispose();
        _bufferThroughput.Dispose();
    }

    private void InitKernel_InitIteration()
    {
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INIT_ITERATION], "_BufferSizes", _bufferBufferSizes);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INIT_ITERATION], "_RadianceAcc", _bufferRadianceAcc);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INIT_ITERATION], "_BounceSamples", _bufferBounceSamples);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INIT_ITERATION], "_Throughput", _bufferThroughput);
    }
    
    private void InitKernel_GenerateRays()
    {
        _shader.SetBuffer(_kernelIds[KERNEL_WF_GENERATE_RAYS], "_BufferSizes", _bufferBufferSizes);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_GENERATE_RAYS], "_BounceRays", _bufferBounceRays);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_GENERATE_RAYS], "_BounceHits", _bufferBounceHits);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_GENERATE_RAYS], "_BounceSamples", _bufferBounceSamples);
    }

    private void InitKernel_IntersectGeometry()
    {
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INTERSECT_GEOMETRY], "_BufferSizes", _bufferBufferSizes);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INTERSECT_GEOMETRY], "_BounceRays", _bufferBounceRays);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_INTERSECT_GEOMETRY], "_BounceHits", _bufferBounceHits);


        SetBuffer(KERNEL_WF_INTERSECT_GEOMETRY, BuffersNames.TRIANGLE_VERTICES);
        SetBuffer(KERNEL_WF_INTERSECT_GEOMETRY, BuffersNames.TRIANGLE_DATAS);
        SetBuffer(KERNEL_WF_INTERSECT_GEOMETRY, BuffersNames.BVH_TREE);
        SetBuffer(KERNEL_WF_INTERSECT_GEOMETRY, BuffersNames.MATERIALS);
        SetBuffer(KERNEL_WF_INTERSECT_GEOMETRY, BuffersNames.LIGHTS);
    }

    private void InitKernel_BounceBsdf()
    {
        _shader.SetBuffer(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_BufferSizes", _bufferBufferSizes);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_BounceHits", _bufferBounceHits);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_BounceRays", _bufferBounceRays);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_RadianceAcc", _bufferRadianceAcc);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_BounceSamples", _bufferBounceSamples);
        _shader.SetBuffer(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_Throughput", _bufferThroughput);
        
        _shader.SetTexture(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_AtlasesAlbedo", _scene.textureArrayAlbedo);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.MAP_DATA_ALBEDO);
        
        _shader.SetTexture(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_AtlasesRoughness", _scene.textureArrayRoughness);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.MAP_DATA_ROUGHNESS);
        
        _shader.SetTexture(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_AtlasesMetallic", _scene.textureArrayMetallic);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.MAP_DATA_METALLIC);
        
        _shader.SetTexture(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_AtlasesEmission", _scene.textureArrayEmission);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.MAP_DATA_EMISSION);

        _shader.SetTexture(_kernelIds[KERNEL_WF_BOUNCE_BSDF], "_AtlasesNormal", _scene.textureArrayNormal);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.MAP_DATA_NORMAL);
        
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.TRIANGLE_VERTICES);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.TRIANGLE_DATAS);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.BVH_TREE);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.MATERIALS);
        SetBuffer(KERNEL_WF_BOUNCE_BSDF, BuffersNames.LIGHTS);
    }
    
    private void InitKernel_AccumulateImageBuffer()
    {
        _shader.SetBuffer(_kernelIds[KERNEL_WF_ACCUMULATE_IMAGE_BUFFER], "_RadianceAcc", _bufferRadianceAcc);
        SetTexture(KERNEL_WF_ACCUMULATE_IMAGE_BUFFER, "_SamplingBuffer", ERenderTextureType.SamplerBuffer);
        SetTexture(KERNEL_WF_ACCUMULATE_IMAGE_BUFFER, "_SamplingBufferPrev", ERenderTextureType.SamplerBufferPrev);
    }

    private void Old()
    {

    // _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesAlbedo", _scene.textureArrayAlbedo);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_ALBEDO);
        //
        // _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesNormal", _scene.textureArrayNormal);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_NORMAL);
        //
        // _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesRoughness", _scene.textureArrayRoughness);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_ROUGHNESS);
        //
        // _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesMetallic", _scene.textureArrayMetallic);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_METALLIC);
        //
        // _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_AtlasesEmission", _scene.textureArrayEmission);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MAP_DATA_EMISSION);
        //
        // _shader.SetInt("_HasCubeMap", _scene.cubeMap != null ? 1:0);
        // _shader.SetInt("_IgnoreCubeInImage", _scene.ignoreCubeInImage ? 1:0);
        //
        // if(_scene.cubeMap != null)
        //     _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_CubeMap", _scene.cubeMap);
        // else
        //     _shader.SetTexture(_kernelIds[KERNEL_MEGA_PATH_TRACE], "_CubeMap", new Cubemap(64, TextureFormat.RGB24, 0));
        //
        //
        //
        // // Mega Kernel
        // SetTexture(KERNEL_MEGA_PATH_TRACE, "_SamplingBuffer", ERenderTextureType.SamplerBuffer);
        // SetTexture(KERNEL_MEGA_PATH_TRACE, "_SamplingBufferPrev", ERenderTextureType.SamplerBufferPrev);
        // SetTexture(KERNEL_MEGA_PATH_TRACE, "_DebugTexture", ERenderTextureType.Debug);
        //
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.TRIANGLE_VERTICES);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.TRIANGLE_DATAS);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.BVH_TREE);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.MATERIALS);
        // SetBuffer(KERNEL_MEGA_PATH_TRACE, BuffersNames.LIGHTS);
    }
}