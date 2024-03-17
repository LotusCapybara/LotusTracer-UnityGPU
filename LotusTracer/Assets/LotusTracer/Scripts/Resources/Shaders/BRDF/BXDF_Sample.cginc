bool Sample_Diffuse(inout uint randState, inout ScatteringData data)
{
    // todo replace by Disney Diffuse
    data.L = normalize( RandomDirectionInHemisphereCosWeighted(randState) );

    if(data.L.y < 0)
        data.L.y = -data.L.y;

    data.H = normalize(data.L + data.V);
    
    return true;
}


//uses BRDF microfacet sampling to sample reflection for both dielectric and metallic materials
bool Sample_Specular(inout uint randState, inout ScatteringData data)
{
    // I'll create some option in the shader to use one or the other?
    float3 facetH = Sample_GGX_Microfacet_VNDF(randState, data.V, data.ax, data.ax);
    // float3 facetH =  Sample_GGX_Microfacet(randState, data.ax, data.ay);

    if(facetH.y < 0)
        facetH.y = - facetH.y;
    
    data.L = normalize(reflect(-data.V, facetH));
    data.H = normalize(data.L + data.V);
    
    return true;
}


bool Sample_ClearCoat(inout uint randState, inout ScatteringData data)
{
    // it is really similar to how specular sampling is calculated, but
    float3 H = Sample_GTR(randState, data.clearCoatRoughness);

    if (H.y < 0.0)
        H = -H;
    
    data.L = normalize(reflect(-data.V, H));
    data.H = normalize(data.L + data.V);
    
    return true;
}

bool Sample_Transmission(inout uint randState, inout ScatteringData data, float scaledR)
{    
    // float3 facetH =  Sample_GGX_Microfacet_VNDF(randState, data.V, data.ax, data.ay);
    float3 facetH =  Sample_GGX_Microfacet(randState, data.ax, data.ay);
    if(facetH.y < 0)
        facetH = -facetH;
    
    float F0 = pow((data.ior1 - data.ior2) / (data.ior1 + data.ior2), 2.0); 
    float F = SchlickFresnel(F0, abs(dot(data.V, facetH)));

    
    if(scaledR < F)
    {
        data.L = normalize(reflect(-data.V, facetH));
        data.H = normalize(data.L + data.V);
    }
    else
    {
        data.L = normalize(refract(-data.V, facetH, data.eta));
        data.H = normalize(data.L * data.eta + data.V);
    }
    
    return true;
}

float Evaluate_Phase(in ScatteringData data, float g)
{
    if( abs(g) < 0.03 )
        return INV_4_PI;

    float cos_theta = dot( data.V , data.L );
    float denom = 1.0 + SQUARE(g) + (2.0 * g) * cos_theta;
    return INV_4_PI * ( 1.0 + SQUARE(g) ) / ( denom * sqrt( denom ) );
}


float3 Sample_Phase(inout uint randState, in float3 V, float g)
{
    g = clamp(g, -0.95, 0.95);
    float cosTheta;
    float r1 = GetRandom0to1(randState);
    float r2 = GetRandom0to1(randState);

    if (abs(g) < 0.001)
        cosTheta = 1.0 - 2.0 * r2;
    else 
    {
        float sqrTerm = (1.0 - g * g) / (1.0 + g - 2.0 * g * r2);
        cosTheta = -(1.0 + g * g - sqrTerm * sqrTerm) / (2.0 * g);
    }

    float phi = r1 * 2.0 * PI;
    float sinTheta = clamp(sqrt(1.0 - (cosTheta * cosTheta)), 0.0, 1.0);
    float sinPhi = sin(phi);
    float cosPhi = cos(phi);

    float3 T;
    float3 B;

    CreateCoordinateSystem(V, T, B);
    return  sinTheta * cosPhi * T + sinTheta * sinPhi * B + cosTheta * V;
}

void Evaluate_PhaseHG(float cosTheta, float g, out float f, out float pdf)
{
    float denom = 1.0 + g * g + 2.0 * g * cosTheta;
    f = INV_4_PI * (1.0 - g * g) / (denom * sqrt(denom));

    pdf = INV_4_PI;
}
