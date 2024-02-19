
float IsDirectLightOccluded(int lightIndex, in ScatteringData scatteringData)
{
    float NoL = dot(scatteringData.sampleData.L, scatteringData.WorldNormal);

    if(NoL <= 0)
        return -1;
    
    RenderRay ray;    
    ray.direction = scatteringData.sampleData.L;
    ray.origin = scatteringData.surfacePoint + scatteringData.sampleData.L * EPSILON;
    ray.invDirection = 1.0 / ray.direction;   
            
    float dist = distance(_Lights[lightIndex].position, scatteringData.surfacePoint);

    if(dist > _Lights[lightIndex].range)
        return -1;
            
    // shadow ray
    if(IsLightOccluded(ray, dist))
        return -1;

    return dist;
}

float3 GetLightColorContribution(int lightIndex, in ScatteringData scatteringData, float dist)
{
    int lightType = _Lights[lightIndex].type;

    // todo: fix this specific case
    // if (lightType == ELightType.Directional)
    // {
    //     scatteringData.L = - scene.lights[lightIndex].forward;
    // }

    float power = _Lights[lightIndex].intensity;

    [branch]
    switch (lightType)
    {
        case LIGHT_POINT:
            power = _Lights[lightIndex].intensity / (dist * dist);
            break;
        case LIGHT_SPOT:

            float dotToOuter = dot(- scatteringData.sampleData.L, _Lights[lightIndex].forward);
            float spotAngleFactor = 1.0 - _Lights[lightIndex].angle * ONE_OVER_360;

            if (dotToOuter <= spotAngleFactor)
                return V_ZERO;
        
            float angleDecay = saturate((dotToOuter - spotAngleFactor) / (1.0 - spotAngleFactor));
            power = _Lights[lightIndex].intensity * angleDecay / (dist * dist); 
        break;
    }

    return _Lights[lightIndex].color * power;
}

static void GetLightsNEE(inout uint randState, in TriangleHitInfo hitInfo, bool comesFromMedium, out float3 radiance, out float pdf)
{
    radiance = V_ZERO;
    pdf = 0;
    
    if(qtyDirectLights <= 0)
        return;


    ScatteringData data = MakeScatteringData_FromHitInfo(randState, hitInfo);

    int lightIndex = (int)(GetRandom0to1(randState) * qtyDirectLights);
    lightIndex = clamp(lightIndex, 0, qtyDirectLights - 1);

    float3 surfacePoint = hitInfo.position + hitInfo.normal * EPSILON;
    
    data.sampleData.L = normalize(_Lights[lightIndex].position - surfacePoint);
    data.sampleData.H = normalize(data.sampleData.L + data.V);
    data.isReflection = dot(data.sampleData.L, data.WorldNormal) > 0;

    float dist = IsDirectLightOccluded(lightIndex, data);
    
    if(dist <= 0)
        return;

    float3 lightContribution = GetLightColorContribution(lightIndex, data, dist);

    float3 lightBSDF;
    float lightPDF;

    if(comesFromMedium)
    {
        float p = Sample_PhaseHG(randState, dot(hitInfo.backRayDirection, data.sampleData.L), data.scatteringDirection);
        lightBSDF = (float3) p;
        lightPDF = p;
    }
    else
    {
        GetBSDF_F(randState, data, lightBSDF, lightPDF);    
    }

    radiance = lightContribution * lightBSDF;
    pdf = lightPDF;
}

