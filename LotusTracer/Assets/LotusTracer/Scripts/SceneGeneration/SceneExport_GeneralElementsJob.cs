using System.Collections;
using System.Collections.Generic;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public static class SceneExport_GeneralElements_Job
{
    public static void Export(SerializedScene scene, GameObject sceneContainer)
    {
        Camera cam = sceneContainer.transform.GetComponentInChildren<Camera>();
        scene.camera = new SerializedCamera();
        scene.camera.forward = cam.transform.forward;
        scene.camera.position = cam.transform.position;
        scene.camera.fov = cam.fieldOfView;
        scene.camera.right = cam.transform.right;
        scene.camera.up = cam.transform.up;
        scene.camera.horizontalSize = cam.orthographicSize;

        Light[] lights = sceneContainer.transform.GetComponentsInChildren<Light>();

        if (lights.Length > 0)
        {
            scene.lights = new RenderLight[lights.Length];

            for (int l = 0; l < lights.Length; l++)
            {
                scene.lights[l] = new RenderLight
                {
                    position = lights[l].transform.position,
                    forward = lights[l].transform.forward,
                    intensity = lights[l].intensity,
                    range = lights[l].range,
                    angle = lights[l].spotAngle,
                    type = (int) lights[l].type,
                    color = new float4(lights[l].color.r, lights[l].color.g, lights[l].color.b, 1f)
                };
            }
        }
        else
        {
            // this is a bit annoying but if there is no lights at all, we should still create a mock single light
            // this is to avoid having zero size compute buffers later on 
            scene.lights = new RenderLight[1] { new RenderLight() };
        }
        
        
    }
}
