using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class ComputeShaderHolder_PostProcess : ComputeShaderHolder
{
    public const string KERNEL_CLEAN_BRIGHNESS_BUFFER = "CleanBrightnessBuffer";
    public const string KERNEL_CREATE_BRIGHNESS_BUFFER = "CreateBrightnessBuffer";
    public const string KERNEL_BLUR_BRIGHNESS_BUFFER = "BlurBrightnessBuffer";
    public const string KERNEL_APPLY_BOOM = "ApplyBoom";
    public const string KERNEL_CAMERA_EXPOSURE = "ApplyCameraExposure";
    public const string KERNEL_APPLY_ACES = "ApplyACES";
    
    

    public float bloomStrength = 01;
    public float bloomThreshold = 0.5f;
    public float bloomRadius = 1f;
    public float ppCameraExposure = 0;

    private int _kernelsRanThisFrame = 0;
    
    public ComputeShaderHolder_PostProcess(string shaderName, RenderScene renderScene,
        TracerComputeBuffers buffers, TracerTextures tracerTextures) : 
        base(shaderName, renderScene, buffers, tracerTextures)
    {
    }

    protected override void Initialize()
    {
        _kernelIds.Add(KERNEL_CLEAN_BRIGHNESS_BUFFER, _shader.FindKernel(KERNEL_CLEAN_BRIGHNESS_BUFFER));
        _kernelIds.Add(KERNEL_CREATE_BRIGHNESS_BUFFER, _shader.FindKernel(KERNEL_CREATE_BRIGHNESS_BUFFER));
        _kernelIds.Add(KERNEL_BLUR_BRIGHNESS_BUFFER, _shader.FindKernel(KERNEL_BLUR_BRIGHNESS_BUFFER));
        _kernelIds.Add(KERNEL_APPLY_BOOM, _shader.FindKernel(KERNEL_APPLY_BOOM));
        _kernelIds.Add(KERNEL_CAMERA_EXPOSURE, _shader.FindKernel(KERNEL_CAMERA_EXPOSURE));
        _kernelIds.Add(KERNEL_APPLY_ACES, _shader.FindKernel(KERNEL_APPLY_ACES));
    }

    public void ResetFrame()
    {
        _kernelsRanThisFrame = 0;
        Graphics.Blit(_tracerTextures.textures[ERenderTextureType.SamplerBuffer], _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);  
    }
    
    
    public void ExecuteKernels()
    {
        ExecuteBloom();
        ExecuteCameraExposure();

        if (_kernelsRanThisFrame > 0)
        {
            Graphics.Blit(_tracerTextures.textures[ERenderTextureType.PostProcessOutput_1], _tracerTextures.textures[ERenderTextureType.SamplerBuffer]);    
        }
    }

    public void ToneMapToLDR()
    {
        if(_kernelsRanThisFrame > 0)
            Graphics.Blit(_tracerTextures.textures[ERenderTextureType.PostProcessOutput_1], _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);
    
        _shader.SetTexture(_kernelIds[KERNEL_APPLY_ACES], "_InputBuffer1", _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);
        _shader.SetTexture(_kernelIds[KERNEL_APPLY_ACES], "_OutputBuffer1", _tracerTextures.textures[ERenderTextureType.Final]);
        
        DispatchKernelFull(KERNEL_APPLY_ACES, _scene.width, _scene.height);
    }

    private void ExecuteCameraExposure()
    {
        if (ppCameraExposure == 0)
            return;

        _kernelsRanThisFrame++;

        // shader variables
        _shader.SetFloat("strength", ppCameraExposure);
        
        _shader.SetTexture(_kernelIds[KERNEL_CAMERA_EXPOSURE], "_InputBuffer1", _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);
        _shader.SetTexture(_kernelIds[KERNEL_CAMERA_EXPOSURE], "_OutputBuffer1", _tracerTextures.textures[ERenderTextureType.PostProcessOutput_1]);
        
        DispatchKernelFull(KERNEL_CAMERA_EXPOSURE, _scene.width, _scene.height);
        
        // important to ensure the next pp has the right input
        Graphics.Blit(_tracerTextures.textures[ERenderTextureType.PostProcessOutput_1], _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);
    }

    private void ExecuteBloom()
    {
        if(bloomStrength <= 0)
            return;

        _kernelsRanThisFrame++;
        
        // shader variables
        _shader.SetInt("width", _scene.width);
        _shader.SetInt("height", _scene.height);
        _shader.SetFloat("strength", bloomStrength);
        _shader.SetFloat("threshold", bloomThreshold);
        _shader.SetFloat("radius", bloomRadius);
        
        // clean brightness buffer
        _shader.SetTexture(_kernelIds[KERNEL_CLEAN_BRIGHNESS_BUFFER], "_BrightnessBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightness]);
        _shader.SetTexture(_kernelIds[KERNEL_CLEAN_BRIGHNESS_BUFFER], "_BrightnessBlurBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightnessBlur]);
        DispatchKernelFull(KERNEL_CLEAN_BRIGHNESS_BUFFER, _scene.width, _scene.height);
        
        // create brightness buffer
        _shader.SetTexture(_kernelIds[KERNEL_CREATE_BRIGHNESS_BUFFER], "_InputBuffer1", _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);
        _shader.SetTexture(_kernelIds[KERNEL_CREATE_BRIGHNESS_BUFFER], "_BrightnessBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightness]);
        
        DispatchKernelFull(KERNEL_CREATE_BRIGHNESS_BUFFER, _scene.width, _scene.height);

        for (int i = 0; i < 5; i++)
        {
            if( i > 0)
                Graphics.Blit(_tracerTextures.textures[ERenderTextureType.BloomBrightnessBlur], _tracerTextures.textures[ERenderTextureType.BloomBrightness]);
            
            // blur brightness buffer - vertical
            _shader.SetFloats("blurDirection", 0, 1f);
            _shader.SetFloat("resolution", _scene.height);
            _shader.SetTexture(_kernelIds[KERNEL_BLUR_BRIGHNESS_BUFFER], "_BrightnessBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightness]);
            _shader.SetTexture(_kernelIds[KERNEL_BLUR_BRIGHNESS_BUFFER], "_BrightnessBlurBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightnessBlur]);
            DispatchKernelFull(KERNEL_BLUR_BRIGHNESS_BUFFER, _scene.width, _scene.height);
        
            Graphics.Blit(_tracerTextures.textures[ERenderTextureType.BloomBrightnessBlur], _tracerTextures.textures[ERenderTextureType.BloomBrightness]);
        
            // blur brightness buffer - vertical
            _shader.SetFloats("blurDirection", 1f, 0f);
            _shader.SetFloat("resolution", _scene.width);
            _shader.SetTexture(_kernelIds[KERNEL_BLUR_BRIGHNESS_BUFFER], "_BrightnessBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightness]);
            _shader.SetTexture(_kernelIds[KERNEL_BLUR_BRIGHNESS_BUFFER], "_BrightnessBlurBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightnessBlur]);
            DispatchKernelFull(KERNEL_BLUR_BRIGHNESS_BUFFER, _scene.width, _scene.height);
        }
        
        // apply bloom to final texture
        _shader.SetTexture(_kernelIds[KERNEL_APPLY_BOOM], "_InputBuffer1", _tracerTextures.textures[ERenderTextureType.SamplerBuffer]);
        _shader.SetTexture(_kernelIds[KERNEL_APPLY_BOOM], "_BrightnessBlurBuffer", _tracerTextures.textures[ERenderTextureType.BloomBrightnessBlur]);
        
        _shader.SetTexture(_kernelIds[KERNEL_APPLY_BOOM], "_OutputBuffer1", _tracerTextures.textures[ERenderTextureType.PostProcessOutput_1]);
        
        DispatchKernelFull(KERNEL_APPLY_BOOM, _scene.width, _scene.height);
        
        // important to ensure the next pp has the right input
        Graphics.Blit(_tracerTextures.textures[ERenderTextureType.PostProcessOutput_1], _tracerTextures.textures[ERenderTextureType.PostProcessInput_1]);
    }
}