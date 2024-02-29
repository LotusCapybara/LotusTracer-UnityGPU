
// --------------------------------------------------------------------------------------
// -------------------------- MICROFACET STUFF ------------------------------------------
// --------------------------------------------------------------------------------------


// ------------------- GGX ----------------------- 

// distribution 
float D_GGX(in float3 h, float ax, float ay)
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

// geometry 
float G_GGX( in float3 v, float ax, float ay )
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

float3 Sample_GGX_Microfacet(inout uint randState, float ax, float ay)
{
    float r0 = GetRandom0to1(randState);
    float r1 = GetRandom0to1(randState);

    if(ax == ay)
    {
        float theta = atan( ax * sqrt( r1 / ( 1.0 - r1 )) );
        float phi = 2.0 * PI * r0;
        return SphericalToVector(theta, phi);
    }

    // todo: add sampling for anisotropic materials (which are still not implemented)
    return V_ONE;
    
}

float PDF_GGX(in ScatteringData data)
{
    return D_GGX(data.H, data.ax, data.ay) * abs(data.H.y);
}


// ------------------- GTR - Generalized Trowbridge-Reitz ----------------------- 

float D_GTR(float NoH, float a)
{
    if (a >= 1.0)
        return ONE_OVER_PI;
    
    float a2 = a * a;
    float t = 1.0 + (a2 - 1.0) * NoH * NoH;
    return (a2 - 1.0) / (PI * log(a2) * t);
}

float3 Sample_GTR(inout uint randState, float roughness)
{
    float r0 = GetRandom0to1(randState);
    float r1 = GetRandom0to1(randState);
    
    float a = max(0.001, roughness);
    float a2 = a * a;

    // this is jut another spherical sampling, I didn't have time to check it's too different
    // from the one I'm using in specular sampling, but I'll leave it like this for now
    // mostly so I don't mess with pdf too much
    
    float phi = r0 * 2.0 * PI;
    float cosTheta = sqrt((1.0 - pow(a2, 1.0 - r1)) / (1.0 - a2));
    float sinTheta = clamp(sqrt(1.0 - (cosTheta * cosTheta)), 0.0, 1.0);
    float sinPhi = sin(phi);
    float cosPhi = cos(phi);

    return float3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
}

// ------------------- Smith ----------------------- 

float G_Smith(float NoV, float alphaG)
{
    float a = alphaG * alphaG;
    float b = NoV * NoV;
    return (2.0 * NoV) / (NoV + sqrt(a + b - a * b));
}


