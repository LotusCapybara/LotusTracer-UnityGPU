using System.Collections;
using System.Collections.Generic;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public static class SceneExport_GeneralElements_Job
{
    public static void Export(GameObject sceneContainer)
    {
        SerializedScene_Data scene = SceneExporter.s_sceneData; 
        
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
                LotusLight lotusLight = lights[l].transform.GetComponent<LotusLight>();
                if (lotusLight == null)
                {
                    lotusLight = lights[l].gameObject.AddComponent<LotusLight>();
                    lotusLight.intensity = lights[l].intensity;
                    lotusLight.radius = 0.1f;
                }

                switch (lights[l].type)
                {
                    case LightType.Point:
                        lotusLight.area = 4f * math.PI * lotusLight.radius * lotusLight.radius;
                        break;
                }
                
                
                scene.lights[l] = new RenderLight
                {
                    position = lights[l].transform.position,
                    forward = lights[l].transform.forward,
                    intensity = lotusLight.intensity,
                    range = lights[l].range,
                    angle = lights[l].spotAngle,
                    type = (int) lights[l].type,
                    color = new float4(lotusLight.color.r, lotusLight.color.g, lotusLight.color.b, 1f),
                    castShadows = (lights[l].shadows != LightShadows.None)? 1 : 0,
                    receiveHits = lotusLight.receiveHits ? 1 : 0,
                    radius = lotusLight.radius,
                    area = lotusLight.area
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
