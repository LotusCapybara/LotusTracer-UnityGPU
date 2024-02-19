using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SliderValue : MonoBehaviour
{
    [SerializeField]
    private Slider _slider;

    private TextMeshProUGUI _textValue;
    
    private void Start()
    {
        _textValue = GetComponent<TextMeshProUGUI>();
        _textValue.text = _slider.wholeNumbers ? _slider.value.ToString() : _slider.value.ToString("F2");
        
        _slider.onValueChanged.AddListener((v) =>
        {
            _textValue.text = _slider.wholeNumbers ? v.ToString() : v.ToString("F2");
        });
    }
}
