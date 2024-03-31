

using UnityEngine;

public interface IGPUTracer
{
    RenderTexture GetRenderTexture(ERenderTextureType textureType);
    
    public double totalTime { get; }
    public int indirectIteration { get; }
    public double averageSampleTime { get; }
    public EDebugBufferType debugType { get; set; }
    public bool isRenderingDebug { get; set; }
}