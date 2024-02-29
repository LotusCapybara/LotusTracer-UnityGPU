using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SampleDebugFunctions
{
    public static Vector3 SphericalToVector( float theta , float phi )
    {
        float x = Mathf.Sin( theta ) * Mathf.Cos( phi );
        float y = Mathf.Cos( theta );
        float z = Mathf.Sin( theta ) * Mathf.Sin( phi );

        return new Vector3(x, y, z);
    }
    
    public static void CreateCoordinateSystem(Vector3 normal, out Vector3 tangent, out Vector3 biTangent)
    {
        Vector3 up = Mathf.Abs(normal.y) < 0.9999999 ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
        tangent = Vector3.Normalize(Vector3.Cross(up, normal));
        biTangent = Vector3.Normalize( Vector3.Cross(normal, tangent) * -1);
    }

    public static Vector3 ToWorld(Vector3 N, Vector3 v)
    {
        CreateCoordinateSystem(N, out Vector3 T, out Vector3 B);
        
        return Vector3.Normalize( v.x * T + v.z * B + v.y * N);
    }

    public static Vector3 RandomDirectionInHemisphereCosWeighted()
    {
        float u = Random.Range(0f, 1f);
        float v = Random.Range(0f, 1f);
        float sinTheta = Mathf.Sqrt(1.0f - u * u);
        float phi = 2.0f * Mathf.PI * v;
        Vector3 dir = Vector3.zero;
        dir.x = sinTheta * Mathf.Cos(phi);
        dir.y = u; 
        dir.z = sinTheta * Mathf.Sin(phi);

        return dir.normalized;
    }

    public static Vector3 SamplePhaseHG(Vector3 V, float g)
    {
        g = Mathf.Clamp(g, -0.95f, 0.95f);
        float cosTheta;
        float r1 = Random.Range(0f, 1f);
        float r2 = Random.Range(0f, 1f);

        if (Mathf.Abs(g) < 0.001f)
            cosTheta = 1 - 2 * r2;
        else 
        {
            float sqrTerm = (1 - g * g) / (1 + g - 2 * g * r2);
            cosTheta = -(1 + g * g - sqrTerm * sqrTerm) / (2 * g);
        }

        float phi = r1 * 2f * Mathf.PI;
        float sinTheta = Mathf.Clamp(Mathf.Sqrt(1.0f - (cosTheta * cosTheta)), 0.0f, 1.0f);
        float sinPhi = Mathf.Sin(phi);
        float cosPhi = Mathf.Cos(phi);

        CreateCoordinateSystem(V, out Vector3 T, out Vector3 B);
        return sinTheta * cosPhi * T + sinTheta * sinPhi * B + cosTheta * V;
    }
}
