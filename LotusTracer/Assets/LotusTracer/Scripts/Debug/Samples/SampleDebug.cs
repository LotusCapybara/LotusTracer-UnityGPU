using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum EDebugSampleType
{
    CosineWeightedHemisphere, Phase
}

public class SamplePoint
{
    public bool isHitPoint;
    public Vector3 position;
}

public class SampleDebug : MonoBehaviour
{
    public float gizmoPointRadius = 0.01f;
    public int qtyRays = 1;
    public int depth = 3;
    public float roughness;
    public float scatterDir = 0.7f;
    public float scatterDist = 0.2f;
    public float mediumDensity = 0.5f;
    

    public EDebugSampleType sampleType;

    private List<List<SamplePoint>> lines;
    
    public void Sample()
    {
        lines = new List<List<SamplePoint>>();
        
        for (int i = 0; i < qtyRays; i++)
        {
            SampleRay();
        }
    }

    public void SampleRay()
    {
        List<SamplePoint> pathSamples = new List<SamplePoint>();
        lines.Add(pathSamples);

        Ray ray = new Ray();
        ray.origin = transform.position;
        ray.direction = transform.forward;
        pathSamples.Add(new SamplePoint{  isHitPoint = false, position = ray.origin} );
        
        switch (sampleType)
        {
            case EDebugSampleType.CosineWeightedHemisphere:
                Sample_CosWeightedHemisphere(ray, pathSamples);
                break;
            case EDebugSampleType.Phase:
                Sample_PhaseHG(ray, pathSamples);
                break;
        }
        

        
    }

    private void Sample_PhaseHG(Ray ray, List<SamplePoint> pathSamples)
    {
        bool isInsideMedium = false;
        
        for (int i = 0; i <= depth; i++)
        {
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                if (isInsideMedium)
                {
                    float hitDist = Vector3.Distance(ray.origin, hitInfo.point);

                    float scatDist = Mathf.Lerp(0.0001f, 0.2f, 1.0f - mediumDensity);
                    scatDist *= (Random.Range(0f, 1f) + 0.00001f);
                    
                    if (hitDist <= scatDist)
                    {
                        isInsideMedium = false;
                        ray.origin = hitInfo.point + ray.direction * 0.01f;
                        pathSamples.Add(new SamplePoint{ position = ray.origin, isHitPoint = true});
                    }
                    else
                    {
                        ray.origin += ray.direction * scatDist;
                        ray.direction = SampleDebugFunctions.SamplePhaseHG(-ray.direction, scatterDir);
                        pathSamples.Add(new SamplePoint{ position = ray.origin, isHitPoint = false});
                    }
                }
                else
                {
                    ray.origin = hitInfo.point + ray.direction * 0.01f;
                    pathSamples.Add(new SamplePoint{ position = ray.origin, isHitPoint = true});
                    ray.direction = ray.direction;
                    isInsideMedium = true;
                }
            }
        }
    }
    
    private void Sample_CosWeightedHemisphere(Ray ray, List<SamplePoint> pathSamples)
    {
        for (int i = 0; i <= depth; i++)
        {
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                ray.origin = hitInfo.point + hitInfo.normal * 0.001f;
                pathSamples.Add(new SamplePoint{ position = ray.origin, isHitPoint = true});
                ray.direction = SampleDebugFunctions.RandomDirectionInHemisphereCosWeighted();
                ray.direction = SampleDebugFunctions.ToWorld(hitInfo.normal, ray.direction);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(lines == null)
            return;

        foreach (var line in lines)
        {
            for (int p = 0; p < line.Count - 1; p++)
            {
                Gizmos.color = line[p].isHitPoint ? Color.red : Color.gray;
                Gizmos.DrawSphere(line[p].position, gizmoPointRadius);
                
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(line[p].position, line[p + 1].position);
            }
            
            Gizmos.color = line[line.Count - 1].isHitPoint ? Color.red : Color.gray;
            Gizmos.DrawSphere(line[line.Count - 1].position, gizmoPointRadius);
        }
    }
}
