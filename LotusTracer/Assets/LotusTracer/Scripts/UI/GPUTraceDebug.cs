using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GPUTraceDebug : MonoBehaviour
{
    [FormerlySerializedAs("_tracer")]
    [SerializeField]
    private GPUTracer_Megakernel tracerMegakernel;

    [SerializeField]
    private TextMeshProUGUI _textTotalTime;
    
    [SerializeField]
    private TextMeshProUGUI _textIteration;
    
    [SerializeField]
    private TextMeshProUGUI _textAverageTime;

    private void Update()
    {
        _textTotalTime.text = $"Total Time:  {tracerMegakernel.totalTime:F4}";
        _textIteration.text = $"Iteration {tracerMegakernel.indirectIteration.ToString()}";
        _textAverageTime.text = $"Avg Time:  {tracerMegakernel.averageSampleTime:F6}";
    }
}
