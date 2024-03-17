
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

// same as Sample_GGX_Microfacet but using Visible Normal Distribution Function
// I think it looks a bit better so I'm using mostly this one
float3 Sample_GGX_Microfacet_VNDF(inout uint randState, in float3 V, float ax, float ay)
{
    float r1 = GetRandom0to1(randState);
    float r2 = GetRandom0to1(randState);
    
    float3 H = normalize(float3(ax * V.x, V.y, ay * V.z));

    float lensq = H.x * H.x + H.z * H.z;
    float3 T1 = lensq > 0 ? float3(-H.z, 0 , H.x) * rsqrt(lensq) : float3(1, 0, 0);
    float3 T2 = cross(H, T1);

    float r = sqrt(r1);
    float phi = 2.0 * PI * r2;
    float t1 = r * cos(phi);
    float t2 = r * sin(phi);
    float s = 0.5 * (1.0 + H.y);
    t2 = (1.0 - s) * sqrt(1.0 - t1 * t1) + s * t2;

    float3 nH = t1 * T1 + t2 * T2 + sqrt(max(0.0, 1.0 - t1 * t1 - t2 * t2)) * H;

    return normalize(float3(ax * nH.x, max(0.0, nH.y), ay * nH.z ));
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


