

using System.Collections.Generic;
using CapyTracerCore.Core;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class ComputeShaderHolder
{
    protected ComputeShader _shader;
    protected Dictionary<string, int> _kernelIds;
    protected RenderScene _scene;
    protected TracerTextures _texturesHolder;
    protected TracerComputeBuffers _buffers;
    protected TracerTextures _tracerTextures;
    
    public ComputeShader shader => _shader;

    protected ComputeShaderHolder(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures)
    {
        _shader = Resources.Load<ComputeShader>(shaderName);
        _kernelIds = new Dictionary<string, int>();
        _scene = renderScene;
        _buffers = buffers;
        _tracerTextures = tracerTextures;
        
        Initialize();
    }
    
    protected virtual void Initialize() {}

    public void DispatchKernelSingle(string kernelName)
    {
        DispatchKernel(kernelName, 1, 1, 1);
    }
    
    public void DispatchKernelFull(string kernelName, int xSize, int ySize)
    {
        DispatchKernel(kernelName, xSize/8f, ySize/8f, 1);
    }
    
    private void DispatchKernel(string kernelName, float x, float y, float z)
    {
        int workGroupsX = Mathf.CeilToInt(x);
        int workGroupsY = Mathf.CeilToInt(y);
        int workGroupsZ = Mathf.CeilToInt(z);
        
        Profiler.BeginSample($"CS-{kernelName}");
        _shader.Dispatch(_kernelIds[kernelName], workGroupsX, workGroupsY, workGroupsZ);
        Profiler.EndSample();
    }

    public virtual void Dispose()
    {
    }

    protected void SetBuffer(string kernelId, string bufferName )
    {
        _shader.SetBuffer(_kernelIds[kernelId], bufferName, _buffers.GetBuffer(bufferName));
    }
    
    protected void SetTexture(string kernelId, string variableName, ERenderTextureType textureType)
    {
        _shader.SetTexture(_kernelIds[kernelId], variableName, _tracerTextures.textures[textureType]);
    }
}