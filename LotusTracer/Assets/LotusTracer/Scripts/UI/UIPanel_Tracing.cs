using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPanel_Tracing : MonoBehaviour
{
    [SerializeField]
    private RawImage _targetImage;

    [SerializeField]
    private TextMeshProUGUI _textTotalTime;
    
    [SerializeField]
    private TextMeshProUGUI _textPhase;
    
    [SerializeField]
    private TextMeshProUGUI _textIndirectSamples;
    
    [SerializeField]
    private TextMeshProUGUI _textIndirectAvg;

    private void OnEnable()
    {
        _targetImage.gameObject.SetActive(true);
    }

    private void Update()
    {
        // _targetImage.texture = _raytracer.renderTexture;
        //
        // _textTotalTime.text = _raytracer.totalTime.ToString("F1");
        // _textPhase.text = _raytracer.renderPhase.ToString();
        // _textIndirectSamples.text = _raytracer.iterations.ToString();
        // _textIndirectAvg.text = _raytracer.indirectSampleAvgTime.ToString("F1");
    }
}
