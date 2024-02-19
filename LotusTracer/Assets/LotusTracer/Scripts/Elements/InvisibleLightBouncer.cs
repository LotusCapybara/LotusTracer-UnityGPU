using UnityEngine;

// meshes with this component will mark the triangles as
// "invisible to camera", which means they still emit light/bounce light
// but they are ignored in the first bounce of the camera path
public class InvisibleLightBouncer : MonoBehaviour
{
}
