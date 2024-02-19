


// used to calculate the proportion/weight of each importance sample method 
float PowerHeuristic(float a, float b)
{
    float t = a * a;
    return t / (b * b + t);
}

// -------------------------------------------------------------------------------------------

static bool TryTransmit(float3 wm, float3 wi, float n, inout float3 wo)
{
    float c = dot(wi, wm);
    if(c < 0.0)
    {
        c = -c;
        wm = -wm;
    }

    float root = 1.0 - n * n * (1.0 - c * c);
    if(root <= 0)
        return false;

    wo = (n * c - sqrt(root)) * wm - n * wi;
    return true;
}

// --------------------------------------------------------------------------------------
// -------------------------- MICROFACET STUFF ------------------------------------------
// --------------------------------------------------------------------------------------


// -- ggx functions

float GGX_D(in float3 h, float ax, float ay)
{
    float cos_theta_h_sq = COS_THETA_2(h);
    if( cos_theta_h_sq <= 0.0 )
        return 0.0;

    float ax2 = ax * ax;
    float ay2 = ay * ay;
    float axy = ax * ay;
    
    float beta = ( cos_theta_h_sq + ( SQUARE( h.x ) / ax2 + SQUARE( h.z ) / ay2));
    return 1.0 / ( PI * axy * beta * beta );
}

float GGX_G1( in float3 v, float ax, float ay )
{
    float tan_theta_sq = COS_THETA_2(v);
    if( tan_theta_sq >= INFINITY )
        return 0.0;

    float ax2 = ax * ax;
    float ay2 = ay * ay;
    
    float cos_phi_sq = COS_PHI_2(v);
    float alpha2 = cos_phi_sq * ax2 + ( 1.0f - cos_phi_sq ) * ay2;
    return 2.0 / ( 1.0 + sqrt( 1.0 + alpha2 * tan_theta_sq ) );    
}

float3 GetMicroFacetH(inout uint randState, in ScatteringData data)
{
    // -- Sample visible distribution of normals
    float r0 = GetRandom0to1(randState);
    float r1 = GetRandom0to1(randState);

    float theta = atan( data.roughness * data.roughness * sqrt( r0 / ( 1.0 - r0 )) );
    float phi = 2.0 * PI * r1;

    return  SphericalToVector(theta, phi);
}


float GTR1(float NoH, float a)
{
    if (a >= 1.0)
        return ONE_OVER_PI;
    
    float a2 = a * a;
    float t = 1.0 + (a2 - 1.0) * NoH * NoH;
    return (a2 - 1.0) / (PI * log(a2) * t);
}

float3 Sample_GTR1(float roughness, float r1, float r2)
{
    float a = max(0.001, roughness);
    float a2 = a * a;

    // this is jut another spherical sampling, I didn't have time to check it's too different
    // from the one I'm using in specular sampling, but I'll leave it like this for now
    // mostly so I don't mess with pdf too much
    
    float phi = r1 * 2.0 * PI;
    float cosTheta = sqrt((1.0 - pow(a2, 1.0 - r2)) / (1.0 - a2));
    float sinTheta = clamp(sqrt(1.0 - (cosTheta * cosTheta)), 0.0, 1.0);
    float sinPhi = sin(phi);
    float cosPhi = cos(phi);

    return float3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
}

float Smith_G(float NoV, float alphaG)
{
    float a = alphaG * alphaG;
    float b = NoV * NoV;
    return (2.0 * NoV) / (NoV + sqrt(a + b - a * b));
}