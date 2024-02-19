using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class RenderRawImage : MonoBehaviour
{
    [SerializeField]
    private ERenderTextureType _textureType;
    
    [FormerlySerializedAs("_tracer")]
    [SerializeField]
    private GPUTracer_Megakernel tracerMegakernel;

    private RawImage _rawImage;

    private void Start()
    {
        _rawImage = GetComponent<RawImage>();
        _rawImage.color = Color.white;
    }

    private void Update()
    {
        _rawImage.texture = tracerMegakernel.GetRenderTexture(_textureType);
    }
}
