using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GPUTraceDebug : MonoBehaviour
{
    [SerializeField]
    private IGPUTracer gpuTracer;

    [SerializeField]
    private TextMeshProUGUI _textTotalTime;
    
    [SerializeField]
    private TextMeshProUGUI _textIteration;
    
    [SerializeField]
    private TextMeshProUGUI _textAverageTime;

    private void Start()
    {
        gpuTracer = GameObject.FindObjectOfType<GPUTracer_WaveFront>(false);
        if(gpuTracer == null)
            gpuTracer = GameObject.FindObjectOfType<GPUTracer_Megakernel>(false);
    }

    private void Update()
    {
        _textTotalTime.text = $"Total Time:  {gpuTracer.totalTime:F4}";
        _textIteration.text = $"Iteration {gpuTracer.indirectIteration.ToString()}";
        _textAverageTime.text = $"Avg Time:  {gpuTracer.averageSampleTime:F6}";
    }
}
