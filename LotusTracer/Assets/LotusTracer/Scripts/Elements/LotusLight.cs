using UnityEngine;

[ExecuteAlways]
public class LotusLight : MonoBehaviour
{
    public float intensity;
    public float radius;
    public float area;
    public Color color;
    public bool receiveHits;
    
    private Light _unityLight;

    private void OnValidate()
    {
        UpdateValues();
    }

    private void OnEnable()
    {
        UpdateValues();
    }

    private void UpdateValues()
    {
        _unityLight = GetComponent<Light>();
        
        if (radius < 0.1f)
            radius = 0.1f;
        if (intensity < 0)
            intensity = 0;

        _unityLight.intensity = intensity;
        _unityLight.color = color;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(color.r, color.g, color.b, 1);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
