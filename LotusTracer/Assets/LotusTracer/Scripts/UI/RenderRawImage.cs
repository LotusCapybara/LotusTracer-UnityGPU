using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RenderRawImage : MonoBehaviour
{
    [SerializeField]
    private ERenderTextureType _textureType;

    [SerializeField]
    private EDebugBufferType _debugBufferType;
    
    [FormerlySerializedAs("_tracer")]
    [SerializeField]
    private GPUTracer_Megakernel tracerMegakernel;

    [SerializeField]
    private RawImage _rawImage;

    private void Start()
    {
        _rawImage.color = Color.white;
    }

    private void Update()
    {
        tracerMegakernel.isRenderingDebug = _textureType == ERenderTextureType.Debug;
        tracerMegakernel.debugType = _debugBufferType;
        
        _rawImage.texture = tracerMegakernel.GetRenderTexture(_textureType);
    }
}
