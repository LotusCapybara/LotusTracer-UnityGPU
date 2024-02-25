bool Sample_Diffuse(inout uint randState, inout ScatteringData data)
{
    // todo replace by Disney Diffuse
    data.sampleData.L = normalize( RandomDirectionInHemisphereCosWeighted(randState) );

    if(data.sampleData.L.y < 0)
        data.sampleData.L.y = -data.sampleData.L.y;

    data.sampleData.H = normalize(data.sampleData.L + data.V);
    
    return true;
}


//uses BRDF microfacet sampling to sample reflection for both dielectric and metallic materials
bool Sample_Specular(inout uint randState, inout ScatteringData data)
{
    // -- Sample visible distribution of normals
    float3 facetH = GetMicroFacetH(randState, data);
    
    data.sampleData.L = normalize(reflect(-data.V, facetH));
    data.sampleData.H = normalize(data.sampleData.L + data.V);
    
    return true;
}


bool Sample_ClearCoat(inout uint randState, inout ScatteringData data)
{
    // -- Sample visible distribution of normals
    float r0 = GetRandom0to1(randState);
    float r1 = GetRandom0to1(randState);

    // it is really similar to how specular sampling is calculated, but
    float3 H = Sample_GTR1(data.clearCoatRoughness, r0, r1);

    if (H.y < 0.0)
        H = -H;
    
    data.sampleData.L = normalize(reflect(-data.V, H));
    data.sampleData.H = normalize(data.sampleData.L + data.V);
    
    return true;
}

bool Sample_Transmission(inout uint randState, inout ScatteringData data)
{
    float3 facetH =  GetMicroFacetH(randState, data);
    if(facetH.y < 0)
        facetH = -facetH;

    data.sampleData.L =
            data.eta == 1 ?
            - data.V :
            refract(-data.V, facetH, data.eta);

    if(data.sampleData.L.y > 0)
        data.sampleData.L = - data.sampleData.L;
    
    data.sampleData.H = normalize(data.sampleData.L + data.V);
    
    return true;
}


float3 Sample_PhaseHG(inout uint randState, float3 V, float g)
{   
    float r1 = GetRandom0to1(randState);
    float r2 = GetRandom0to1(randState);
    
    float cosTheta;

    if (abs(g) < 0.001)
    {
        cosTheta = 1.0 - 2.0 * r2;
    }        
    else 
    {
        float sqrTerm = (1.0 - g * g) / (1.0 + g - 2.0 * g * r2);
        cosTheta = -(1.0 + g * g - sqrTerm * sqrTerm) / (2.0 * g);
    }

    float phi = r1 * 2.0 * PI;
    float sinTheta = clamp(sqrt(1.0 - (cosTheta * cosTheta)), 0.0, 1.0);
    float sinPhi = sin(phi);
    float cosPhi = cos(phi);

    float3 t, bt;
    CreateCoordinateSystem(V, t, bt);

    return sinTheta * cosPhi * t + sinTheta * sinPhi * bt + cosTheta * V;
}

float Evaluate_PhaseHG(float cosTheta, float g)
{
    float denom = 1.0 + g * g + 2.0 * g * cosTheta;
    return INV_4_PI * (1.0 - g * g) / (denom * sqrt(denom));
}
