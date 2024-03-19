using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GPUTraceDebug : MonoBehaviour
{
    [SerializeField]
    private GPUTracer_Megakernel tracerMegakernel;
    
    [SerializeField]
    private GPUTracer_WaveFront tracerWaveFront;

    [SerializeField]
    private TextMeshProUGUI _textTotalTime;
    
    [SerializeField]
    private TextMeshProUGUI _textIteration;
    
    [SerializeField]
    private TextMeshProUGUI _textAverageTime;

    private void Update()
    {
        _textTotalTime.text = $"Total Time:  {tracerWaveFront.totalTime:F4}";
        _textIteration.text = $"Iteration {tracerWaveFront.indirectIteration.ToString()}";
        _textAverageTime.text = $"Avg Time:  {tracerWaveFront.averageSampleTime:F6}";
    }
}
