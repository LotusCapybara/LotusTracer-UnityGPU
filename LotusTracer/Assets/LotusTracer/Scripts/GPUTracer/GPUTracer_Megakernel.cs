using System.Collections;
using System.Diagnostics;
using CapyTracerCore.Core;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// I'd like to implement a Wavefront version of the tracer at some point
public class GPUTracer_Megakernel : MonoBehaviour, IGPUTracer
{
    [Header("Path Tracer")]
    public string sceneName = "Classic-Cornell";
    public float maxTime = -1;
    [Range(3, 10)]
    public int depthDiffuse = 3;
    [Range(3, 10)]
    public int depthSpecular = 3;
    [Range(5, 20)]
    public int depthTransmission = 12;
    public int totalIterations = 200;

    [Header("Scene")]
    public TracerCamera tracerCamera;
    public bool overrideCameraFov;
    public float overridenFov = 60f;

    public Cubemap cubeMap;
    public bool ignoreCubeInImage;
    public Color ambientLightColor = Color.white;
    public float ambientLightPower = 1.0f;

    [Header("Post Processing")]
    public bool enabledPostProcessing = true;
    public float bloomStrength = 1f;
    public float bloomThreshold = 1f;
    public float bloomRadius = 1;
    
    [Range(-2f, 2f)]
    public float ppCameraExposure = 0;

    [Header("Debug")]
    public bool createCameraDebugBuffers;
    
    private int _width;
    private int _height;
    
    private int _indirectIteration;
    private Stopwatch _stopwatch;
    
    // compute shader holders
    private ComputeShaderHolder_CameraBuffers _csCameraBuffers;
    private ComputeShaderHolder_MegaKernel _csMegaKernel;

    private ComputeShaderHolder_PostProcess _csPostProcess;

    private RenderScene _renderScene;
    private TracerTextures _textures;
    private TracerComputeBuffers _computeBuffers;

    private bool _wasCameraMovingLastFrame;

    public RenderTexture GetRenderTexture(ERenderTextureType type) => _textures.textures[type];
    
    
    public double totalTime { get; private set; }
    public double averageSampleTime { get; private set; }
    public int indirectIteration => _indirectIteration;
    public EDebugBufferType debugType { get; set; }
    public bool isRenderingDebug { get; set; }
    
    private IEnumerator Start()
    {
        Application.runInBackground = true;
        _width = Screen.width;
        _height = Screen.height;

        _indirectIteration = 0;
        totalTime = 0;
        averageSampleTime = 0;

        LoadScene();

        _textures = new TracerTextures(_width, _height);
        
        // important: to be done AFTER initializing the scene
        _computeBuffers = new TracerComputeBuffers(_renderScene);
        
        LoadShaders();

        yield return null;
        
        StartCoroutine(RenderRoutineMegaKernel());    
    }

    private void OnDestroy()
    {
        _textures?.Dispose();
        _computeBuffers?.Dispose();
    }

    private void LoadScene()
    {
        _renderScene = new RenderScene();
        _renderScene.Load(sceneName, _width, _height,
            depthDiffuse, depthSpecular, depthTransmission, 
            ambientLightColor, ambientLightPower);

        _renderScene.cubeMap = cubeMap;
        _renderScene.ignoreCubeInImage = ignoreCubeInImage;
        
        if(tracerCamera != null)
            tracerCamera.Initialize(_renderScene.renderCamera);
    }
    
    private void LoadShaders()
    {
        if (createCameraDebugBuffers)
        {
            _csCameraBuffers = 
                new ComputeShaderHolder_CameraBuffers("Shaders/EntryPoints/entry_camera_buffers", 
                    _renderScene,_computeBuffers, _textures);    
        }
        
        _csMegaKernel = 
            new ComputeShaderHolder_MegaKernel("Shaders/EntryPoints/entry_mega_kernel", _renderScene,_computeBuffers, _textures);

        _csPostProcess = new ComputeShaderHolder_PostProcess("Shaders/PostPro/post_processing", _renderScene, _computeBuffers, _textures);
    }
    
    // this routine executes all the iterations of rendering in compute shaders
    // I'd like to optimize a few things but it's ok for now
    private IEnumerator RenderRoutineMegaKernel()
    {
        totalTime = 0;

        if (overrideCameraFov)
            _renderScene.renderCamera.fov = overridenFov;

        _csMegaKernel.UpdateCameraGPUData();
        _csCameraBuffers.UpdateCameraGPUData();
        CreateCameraDebugBuffers();
        yield return null;

        _stopwatch = Stopwatch.StartNew();

        int metrictStartFrame = 20;
        
        while (_indirectIteration < totalIterations)
        {
            _csMegaKernel.shader.SetInt("someSeed", Random.Range(0, 15000));
            
            if (isRenderingDebug)
            {
                _csCameraBuffers.shader.SetInt("_debugBufferType", (int) debugType);
                CreateCameraDebugBuffers();
            }
            else
            {
                _csMegaKernel.shader.SetInt("iteration", _indirectIteration);
                
                _csMegaKernel.DispatchKernelFull(ComputeShaderHolder_MegaKernel.KERNEL_MEGA_PATH_TRACE, _width, _height);

                _csPostProcess.ResetFrame();
                
                if (enabledPostProcessing && !tracerCamera.isMoving)
                {
                    _csPostProcess.bloomStrength = bloomStrength;
                    _csPostProcess.bloomThreshold = bloomThreshold;
                    _csPostProcess.bloomRadius = bloomRadius;
                    _csPostProcess.ppCameraExposure = ppCameraExposure;
                    _csPostProcess.ExecuteKernels();
                }
                
                _csPostProcess.ToneMapToLDR();
            
                
                _indirectIteration++;

                if (_indirectIteration > metrictStartFrame)
                {
                    totalTime = _stopwatch.Elapsed.TotalSeconds;
                    averageSampleTime = totalTime / indirectIteration;
                }
            
                if(maxTime > 0 && totalTime >= maxTime)
                    break;
            }

            yield return CameraMovementRoutine();

            yield return null;
        }

        totalTime = _stopwatch.Elapsed.TotalSeconds;
        averageSampleTime = totalTime / indirectIteration;
        
        SaveImage("-final");
        
        Debug.Log("Finished");
    }

    
    
    private IEnumerator CameraMovementRoutine()
    {
        if (tracerCamera.isMoving)
        {
            _csMegaKernel.shader.SetBool("_isCameraMoving", true);
            _indirectIteration = 0;
            _stopwatch.Restart();
            totalTime = 0;

            _csMegaKernel.UpdateCameraGPUData();
            _csCameraBuffers.UpdateCameraGPUData();
            yield return null;
            
            _wasCameraMovingLastFrame = true;
        }
        else if (_wasCameraMovingLastFrame)
        {
            _wasCameraMovingLastFrame = false;
            
            _textures.ResetTextures();
            _csMegaKernel.UpdateCameraGPUData();
            _csCameraBuffers.UpdateCameraGPUData();
            CreateCameraDebugBuffers();
        }
        else
        {
            _csMegaKernel.shader.SetBool("_isCameraMoving", false);
        }
    }
    
    private void CreateCameraDebugBuffers()
    {
        if(!createCameraDebugBuffers)
            return;
        _csCameraBuffers.DispatchKernelFull(ComputeShaderHolder_CameraBuffers.KERNEL_DEBUG_TEXTURES, _width, _height);
    }

    public void SaveImage(string nameSubfix = "")
    {
        RenderSaver.SaveTexture(sceneName + nameSubfix, _textures.textures[ERenderTextureType.Final]);
    }
}
