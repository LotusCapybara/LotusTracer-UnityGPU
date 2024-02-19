using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class ComputeShaderHolder_Bloom : ComputeShaderHolder
{
    public const string KERNEL_DOWN_SAMPLE = "Downsample";
    public const string KERNEL_LOW_PASS = "LowPass";
    public const string KERNEL_UP_SAMPLE = "Upsample";
    
    public const int QTY_SAMPLES = 5;

    public RenderTexture[] _temporalBloomSamples;
    
    public int2[] _bloomSizes;

    public float bloomStrength = 1f;
    public float filterRadius = 5f;
    
    public ComputeShaderHolder_Bloom(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures) : 
        base(shaderName, renderScene, buffers, tracerTextures)
    {
        _temporalBloomSamples = new RenderTexture[QTY_SAMPLES];
        _bloomSizes = new int2[QTY_SAMPLES];

        int rWidth = _scene.width / 2;
        int rHeight = _scene.height / 2;
        for (int i = 0; i < QTY_SAMPLES; i++)
        {
            _bloomSizes[i] = new int2(rWidth, rHeight);
            _temporalBloomSamples[i] = ShaderUtils.Create(rWidth, rHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear, false);
            rWidth /= 2;
            rHeight /= 2;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        
        foreach (var temporalBloomSample in _temporalBloomSamples)
        {
            temporalBloomSample.Release();
        }
    }

    protected override void Initialize()
    {
        _kernelIds.Add(KERNEL_DOWN_SAMPLE, _shader.FindKernel(KERNEL_DOWN_SAMPLE));
        _kernelIds.Add(KERNEL_LOW_PASS, _shader.FindKernel(KERNEL_LOW_PASS));
        _kernelIds.Add(KERNEL_UP_SAMPLE, _shader.FindKernel(KERNEL_UP_SAMPLE));
        
        // shader variables
        _shader.SetInt("width", _scene.width);

        
        // Kernel Low Sample

    }

    public void ExecuteBloom()
    {
        RenderTexture originalScene = RenderTexture.GetTemporary(_scene.width, _scene.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Graphics.Blit(_texturesHolder.textures[ERenderTextureType.Final], originalScene);

        
        // _shader.SetFloat("filterRadius", filterRadius);
        _shader.SetFloat("strength", bloomStrength);
        _shader.SetInt("width", _scene.width);
        _shader.SetInt("height", _scene.height);
        _shader.SetInt("TargetWidth", _bloomSizes[0].x);
        _shader.SetInt("TargetHeight", _bloomSizes[0].y);
        

        _shader.SetTexture(_kernelIds[KERNEL_LOW_PASS], "InputTex", originalScene);
        _shader.SetTexture(_kernelIds[KERNEL_LOW_PASS], "OutputTex", _temporalBloomSamples[0]);
        
        // todo fix shader to use 8,8,1
        DispatchKernelFull(KERNEL_LOW_PASS, _bloomSizes[0].x, _bloomSizes[0].y);

        for (int i = 1; i < QTY_SAMPLES - 1 ; i++)
        {
            _shader.SetInt("TargetWidth", _bloomSizes[i].x);
            _shader.SetInt("TargetHeight", _bloomSizes[i].y);
            _shader.SetInt("width", _bloomSizes[i - 1].x);
            _shader.SetInt("height", _bloomSizes[i - 1].y);
            
            
            _shader.SetTexture(_kernelIds[KERNEL_DOWN_SAMPLE], "InputTex", _temporalBloomSamples[i - 1]);
            _shader.SetTexture(_kernelIds[KERNEL_DOWN_SAMPLE], "OutputTex", _temporalBloomSamples[i]);
            
            // todo fix shader to use 8,8,1
            DispatchKernelFull(KERNEL_DOWN_SAMPLE, _bloomSizes[i - 1].x, _bloomSizes[i - 1].y);
        }
        
        _shader.SetBool("IsFinal", false);
        
        for (int i = QTY_SAMPLES - 1; i > 0; i--)
        {
            _shader.SetInt("TargetWidth", _bloomSizes[i - 1].x);
            _shader.SetInt("TargetHeight", _bloomSizes[i - 1].y);
            _shader.SetInt("width", _bloomSizes[i].x);
            _shader.SetInt("height", _bloomSizes[i].y);
            
            
            _shader.SetTexture(_kernelIds[KERNEL_UP_SAMPLE], "InputTex", _temporalBloomSamples[i]);
            _shader.SetTexture(_kernelIds[KERNEL_UP_SAMPLE], "OutputTex", _temporalBloomSamples[i - 1]);
            _shader.SetTexture(_kernelIds[KERNEL_UP_SAMPLE], "OrigTex", _temporalBloomSamples[i - 1]);
            
            // todo fix shader to use 8,8,1
            DispatchKernelFull(KERNEL_UP_SAMPLE, _bloomSizes[i - 1].x, _bloomSizes[i - 1].y);
        }
        
        _shader.SetInt("TargetWidth", _scene.width);
        _shader.SetInt("TargetHeight", _scene.height);
        _shader.SetInt("width", _bloomSizes[0].x);
        _shader.SetInt("height", _bloomSizes[0].y);
        
        
        _shader.SetBool("IsFinal", true);
        
        _shader.SetTexture(_kernelIds[KERNEL_UP_SAMPLE], "InputTex", _temporalBloomSamples[0]);
        _shader.SetTexture(_kernelIds[KERNEL_UP_SAMPLE], "OutputTex", _texturesHolder.textures[ERenderTextureType.Final]);
        _shader.SetTexture(_kernelIds[KERNEL_UP_SAMPLE], "OrigTex", originalScene);
            
        // todo fix shader to use 8,8,1
        DispatchKernelFull(KERNEL_UP_SAMPLE, _scene.width, _scene.height);
        
        RenderTexture.ReleaseTemporary(originalScene);
    }
}