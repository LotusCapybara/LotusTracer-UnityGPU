
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

    float dist = distance(data.surfacePoint, _Lights[lightIndex].position);

    float3 lightContribution = _Lights[lightIndex].color * _Lights[lightIndex].intensity /  (dist * dist);  // GetLightColorContribution(lightIndex, data, dist);

    float3 tramissionPower = V_ONE;

    TriangleHitInfo hitInfo =  (TriangleHitInfo) 0;
    RenderRay shadowRay;    
    shadowRay.direction = normalize(data.L);
    shadowRay.origin = data.surfacePoint + data.L * EPSILON;

    float totalDist = dist;
    
    for(int i = 0; i < 5; i++)
    {
        bool didShadowHit = GetBounceHit(hitInfo, shadowRay, totalDist, false);

        RenderMaterial mat = _Materials[hitInfo.materialIndex];

        if(didShadowHit && mat.transmissionPower <= 0)
            return V_ZERO;       
        
        if(!didShadowHit || mat.emissiveIntensity > 0)
            break;         

        tramissionPower *= mat.color;
        tramissionPower *= exp(- mat.color * max(0.1, mat.mediumDensity) * hitInfo.distance);
        shadowRay.origin = hitInfo.position + shadowRay.direction * 2.0 * EPSILON;

        totalDist -= hitInfo.distance;
    }
    
    float3 lightBSDF = V_ZERO;
    float lightPDF = 0;

    if(comesFromMedium)
    {
        float f;
        Evaluate_PhaseHG(dot( data.V, data.L), data.scatteringDirection, f, lightPDF);
        lightBSDF = (float3) f;
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

    return radiance * tramissionPower;
}

