using System.Collections.Generic;
using System.IO;
using System.Linq;
using CapyTracerCore.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The whole UI thing is definitely clunky and made in a rush. If needed and actually used, I'll revisit it 
/// </summary>
public class UIPanel_Startup : MonoBehaviour
{
    // [SerializeField]
    // private RayTracerDots _raytracer;
    //
    // [SerializeField]
    // private UIPanel_Tracing _panelTracing;
    //
    // [SerializeField]
    // private TMP_Dropdown _dropdownScene;
    //
    // [SerializeField]
    // private TMP_Dropdown _dropdownResolution;
    //
    // [SerializeField]
    // private Slider _sliderMaxIterations;
    //
    // [SerializeField]
    // private Slider _sliderIndirectPower;
    //
    // [SerializeField]
    // private Slider _sliderBounces;
    //
    // [SerializeField]
    // private Slider _sliderBVHMaxDepth;
    //
    // [SerializeField]
    // private Slider _sliderBVHTrianglesToExpand;
    //
    // [SerializeField]
    // private Button _btnStartTracing;
    //
    // private void Start()
    // {
    //     _panelTracing.gameObject.SetActive(false);
    //     
    //     _btnStartTracing.onClick.AddListener(OnClick_StartTracing);
    //
    //     List<TMP_Dropdown.OptionData> sceneFiles = Directory
    //         .EnumerateFiles(Application.streamingAssetsPath, "*.dat", SearchOption.AllDirectories)
    //         .Select(fileName => new TMP_Dropdown.OptionData
    //         {
    //             text = Path.GetFileName(fileName)
    //         }).ToList();
    //
    //     _dropdownScene.options = sceneFiles;
    //     _dropdownScene.SetValueWithoutNotify(PlayerPrefs.GetInt("dropScene", 0));
    //
    //     _dropdownResolution.options = new List<TMP_Dropdown.OptionData>
    //     {
    //         new() { text = "480x340" },
    //         new() { text = "640x480" },
    //         new() { text = "800x600" },
    //         new() { text = "1280x720" },
    //     };
    //     _dropdownResolution.SetValueWithoutNotify(PlayerPrefs.GetInt("dropResolution", 0));
    //     
    //     _sliderBounces.SetValueWithoutNotify(PlayerPrefs.GetInt("sliderBounces", 5));
    //     _sliderMaxIterations.SetValueWithoutNotify(PlayerPrefs.GetInt("sliderMaxIterations", 100));
    //     _sliderIndirectPower.SetValueWithoutNotify(PlayerPrefs.GetFloat("sliderIndirectPower", 1f));
    //     _sliderBVHMaxDepth.SetValueWithoutNotify(PlayerPrefs.GetInt("sliderMaxDepth", 20));
    //     _sliderBVHTrianglesToExpand.SetValueWithoutNotify(PlayerPrefs.GetInt("sliderMaxTriangles", 5));
    // }
    //
    // private void OnClick_StartTracing()
    // {
    //
    //     PlayerPrefs.SetInt("dropScene", _dropdownScene.value);
    //     PlayerPrefs.SetInt("dropResolution", _dropdownResolution.value);
    //
    //     PlayerPrefs.SetInt("sliderBounces", (int)_sliderBounces.value);
    //     PlayerPrefs.SetInt("sliderMaxIterations", (int)_sliderMaxIterations.value);
    //     PlayerPrefs.SetFloat("sliderIndirectPower", _sliderIndirectPower.value);
    //     PlayerPrefs.SetInt("sliderMaxDepth", (int)_sliderBVHMaxDepth.value);
    //     PlayerPrefs.SetInt("sliderMaxTriangles", (int)_sliderBVHTrianglesToExpand.value);
    //     
    //     gameObject.SetActive(false);
    //     _panelTracing.gameObject.SetActive(true);
    //
    //     int width = 0;
    //     int height = 0;
    //     switch (_dropdownResolution.value)
    //     {
    //         case 0:
    //             width = 480; height = 340;
    //             break;
    //         case 1:
    //             width = 640; height = 480;
    //             break;
    //         case 2:
    //             width = 800; height = 600;
    //             break;
    //         case 3:
    //             width = 1280; height = 720;
    //             break;
    //     }
    //     
    //     _raytracer.StartTracing(
    //         new TracerSettings
    //         {
    //             width =  width,
    //             height = height,
    //             pathMaxDepth = (int)  _sliderBounces.value,
    //             maxIterations = (int)  _sliderMaxIterations.value, 
    //             indirectPower=  _sliderIndirectPower.value,
    //             bvhMaxDepth =  (int) _sliderBVHMaxDepth.value,
    //             bvhTrianglesToExpand = (int) _sliderBVHTrianglesToExpand.value
    //         },
    //          Path.Combine(Application.streamingAssetsPath, _dropdownScene.options[_dropdownScene.value].text)
    //         );
    // }
}
