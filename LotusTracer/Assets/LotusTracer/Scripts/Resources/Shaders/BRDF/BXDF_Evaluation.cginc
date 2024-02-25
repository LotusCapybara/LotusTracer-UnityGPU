static void Evaluate_Diffuse_Lambert(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    f +=  data.color * ONE_OVER_PI * abs(ev.NoL);
    
    pdf += ev.NoL * ONE_OVER_PI * data.probs.prDiffuse;
}

static void Evaluate_Diffuse_OrenNayar(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    float sintheta_i = SIN_THETA(data.sampleData.L);
    float sintheta_o = SIN_THETA(data.V);

    float sinphii = SIN_PHI(data.sampleData.L);
    float cosphii = COS_PHI(data.sampleData.L);
    float sinphio = SIN_PHI(data.V);
    float cosphio = COS_PHI(data.sampleData.L);
    float dcos = cosphii * cosphio + sinphii * sinphio;
    if( dcos < 0.0 )
        dcos = 0.0;

    float abs_cos_theta_o = (float) ABS_COS_THETA(data.V);
    float abs_cos_theta_i = (float) ABS_COS_THETA(data.sampleData.L);

    if( abs_cos_theta_i < 0.00001 && abs_cos_theta_o < 0.00001 )
        return;

    float sinalpha , tanbeta;
    if( abs_cos_theta_o > abs_cos_theta_i )
    {
        sinalpha = sintheta_i;
        tanbeta = sintheta_o / abs_cos_theta_o;
    }else
    {
        sinalpha = sintheta_o;
        tanbeta = sintheta_i / abs_cos_theta_i;
    }

    float sigma2 = data.roughness * data.roughness;
    float A = 1.0 - (sigma2 / (2.0 * (sigma2 + 0.33)));
    float B = 0.45 * sigma2 / (sigma2 + 0.09);
    
    f += float3(0, 1, 0 ) * data.color * ONE_OVER_PI * ( A + B * dcos * sinalpha * tanbeta ) * abs_cos_theta_i;
    
    pdf += ev.NoL * ONE_OVER_PI * data.probs.prDiffuse;
}

// evaluate specular dielectric BRDF with micro facet reflections
static void Evaluate_Specular(inout ScatteringData data)
{
    float NoL = data.sampleData.L.y;
    float NoV = data.V.y;

    if (NoL <= 0.0 || NoV <= 0.0)
        return;

    float NoH = min(data.sampleData.H.y, 0.99);
    
    // evaluate fresnel term    
    float3 F = SchlickFresnel_V(data.F0, dot(data.sampleData.L, data.sampleData.H) );
    float D = GGX_D(data.sampleData.H, data.ax, data.ay);
    float G1 = GGX_G1(data.V, data.ax, data.ay);
    float G2 = GGX_G1(data.sampleData.L, data.ax, data.ay);
    
    // data.sampleData.sampleReflectance = D * F * G1 * G2 / ( 4.0 * abs(NoV) );    
    // data.sampleData.pdf  = D * G1 / max(0.0001, 4.0 * NoV);
}

// evaluate specular dielectric BRDF with micro facet reflections
static void Evaluate_ClearCoat(inout ScatteringData data)
{
    float VoH = dot(data.V, data.sampleData.H);

    float F = lerp(0.04, 1.0, SchlickWeight(VoH));
    float D = GTR1(data.sampleData.H.y, data.clearCoatRoughness);
    float G = Smith_G(data.sampleData.L.y, 0.25) * Smith_G(data.V.y, 0.25);
    float jacobian = 1.0 / (4.0 * VoH);

    // data.sampleData.sampleReflectance = ((float3) F) * D * G;    
    // data.sampleData.pdf  = D * data.sampleData.H.y * jacobian;
}

static void Evaluate_Transmission(inout ScatteringData data)
{
    if(data.isReflection)
    {
        Evaluate_Specular(data);
        // data.sampleData.pdf *= data.sampleData.refractF;
    }
    else
    {
        // this implementation is garbage. It basically provides a linear/constant energy
        // regardless of how the light enters the surface, but it's fine for a quick
        // implementation....... I'll implement a proper one later on
        // data.sampleData.sampleReflectance = pow(data.color, (float3) 0.5);
        // data.sampleData.pdf  = 1.0 * (1.0 - data.sampleData.refractF);
    }
}




