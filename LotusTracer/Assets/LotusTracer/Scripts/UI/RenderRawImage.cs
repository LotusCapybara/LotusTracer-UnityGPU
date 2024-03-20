using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RenderRawImage : MonoBehaviour
{
    [SerializeField]
    private ERenderTextureType _textureType;

    [SerializeField]
    private EDebugBufferType _debugBufferType;
    
    [SerializeField]
    private IGPUTracer _gpuTracer;
    
    [SerializeField]
    private RawImage _rawImage;

    private void Start()
    {
        _rawImage.color = Color.white;
        _gpuTracer = GetComponent<IGPUTracer>();
    }

    private void Update()
    {
        // tracerMegakernel.isRenderingDebug = _textureType == ERenderTextureType.Debug;
        // tracerMegakernel.debugType = _debugBufferType;
        //
        // _rawImage.texture = tracerMegakernel.GetRenderTexture(_textureType);
        
        _rawImage.texture = _gpuTracer.GetRenderTexture(_textureType);
    }
}
