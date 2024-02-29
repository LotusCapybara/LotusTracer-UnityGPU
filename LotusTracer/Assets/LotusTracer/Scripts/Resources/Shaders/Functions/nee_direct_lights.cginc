
float IsDirectLightOccluded(int lightIndex, in ScatteringData scatteringData)
{
    float NoL = dot(scatteringData.L, scatteringData.WorldNormal);

    if(NoL <= 0)
        return -1;
    
    RenderRay ray;    
    ray.direction = scatteringData.L;
    ray.origin = scatteringData.surfacePoint + scatteringData.L * EPSILON;
            
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

            float dotToOuter = dot(- scatteringData.L, _Lights[lightIndex].forward);
            float spotAngleFactor = 1.0 - _Lights[lightIndex].angle * ONE_OVER_360;

            if (dotToOuter <= spotAngleFactor)
                return V_ZERO;
        
            float angleDecay = saturate((dotToOuter - spotAngleFactor) / (1.0 - spotAngleFactor));
            power = _Lights[lightIndex].intensity * angleDecay / (dist * dist); 
        break;
    }

    return _Lights[lightIndex].color * power;
}

static float3 GetLightsNEE(inout uint randState, in ScatteringData data, float bouncePDF, bool comesFromMedium)
{
    float3 radiance = V_ZERO;

    if(qtyDirectLights <= 0)
        return radiance;

    // todo this is a hacky temporal thing because right now I'm not handling
    // nee for glass and other transmisive materials pretty well
    if(data.transmissionPower >= 0.9 && data.mediumDensity < 0.1)
        return radiance;

    int lightIndex = (int)(GetRandom0to1(randState) * qtyDirectLights);
    lightIndex = clamp(lightIndex, 0, qtyDirectLights - 1);

    float3 surfacePoint = data.surfacePoint + data.WorldNormal * EPSILON;
    
    data.L = normalize(_Lights[lightIndex].position - surfacePoint);
    data.H = normalize(data.L + data.V);
    data.isReflection = dot(data.L, data.WorldNormal) > 0;

    float dist = IsDirectLightOccluded(lightIndex, data);
    
    if(dist <= 0)
        return radiance;

    float3 lightContribution = GetLightColorContribution(lightIndex, data, dist);

    float3 lightBSDF = V_ZERO;
    float lightPDF = 0;

    if(comesFromMedium)
    {
        float f;
        Evaluate_PhaseHG(dot( data.V, data.L), data.scatteringDirection, f, lightPDF);
        lightBSDF = (float3) f;
        // float p = Evaluate_Phase(data, data.scatteringDirection);
        // lightBSDF = (float3) p;
        // lightPDF = p;
    }
    else
    {
        GetBSDF_F(randState, data, lightBSDF, lightPDF);    
    }

    if(lightPDF > 0)
    {
        float mis = PowerHeuristic(bouncePDF, lightPDF);
        radiance += lightContribution * mis * lightBSDF / lightPDF;    
    }

    return radiance;
}

