

using UnityEngine;

public interface IGPUTracer
{
    RenderTexture GetRenderTexture(ERenderTextureType textureType);
    
    double totalTime { get; }
    int indirectIteration { get; }
    double averageSampleTime { get; }
}