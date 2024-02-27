static void Evaluate_Diffuse_Lambert(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    f +=  data.color * ONE_OVER_PI * abs(ev.NoL);
    
    pdf += ONE_OVER_PI;
}

static void Evaluate_Diffuse_OrenNayar(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    float sintheta_i = SIN_THETA(data.sampleData.L);
    float sintheta_o = SIN_THETA(data.V);

    float sinphii = SIN_PHI(data.sampleData.L);
    float cosphii = COS_PHI(data.sampleData.L);
    float sinphio = SIN_PHI(data.V);
    float cosphio = COS_PHI(data.V);
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
    
    f = data.color * ONE_OVER_PI * ( A + B * dcos * sinalpha * tanbeta ) * abs_cos_theta_i;    
    pdf = ev.NoL * ONE_OVER_PI;
}

// evaluate specular dielectric BRDF with micro facet reflections
static void Evaluate_Specular(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    if (ev.NoL <= 0.0 || ev.NoV <= 0.0)
        return;
    
    // evaluate fresnel term    
    float3 F = SchlickFresnel_V(data.F0, dot(data.sampleData.L, data.sampleData.H) );
    float D = GGX_D(data.sampleData.H, data.ax, data.ay);
    float G1 = GGX_G1(data.V, data.ax, data.ay);
    float G2 = GGX_G1(data.sampleData.L, data.ax, data.ay);
    
    f = D * F * G1 * G2 / ( 4.0 * abs(ev.NoV) );    
    pdf = D * G1 / max(0.0001, 4.0 * ev.NoV);
}

// evaluate specular dielectric BRDF with micro facet reflections
static void Evaluate_ClearCoat(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    float aVOH = dot(data.V, data.sampleData.H);
    
    float F = lerp(0.04, 1.0, SchlickWeight(aVOH));
    float D = GTR1( abs( data.sampleData.H.y), data.clearCoatRoughness);
    float G = Smith_G( abs(data.sampleData.L.y), 0.25) * Smith_G(abs(data.V.y), 0.25);
    float jacobian = 1.0 / (4.0 * aVOH);

    f =  ((float3) F) * D * G;
    pdf = D * data.sampleData.H.y * jacobian;
}

static void Evaluate_Transmission(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    if(data.V.y * data.sampleData.L.y > 0)
        return;
    if(ev.NoV == 0)
        return;
    
    float3 H = normalize(data.V + data.sampleData.L * data.eta);
    if(H.y < 0)
        H = -H;
    
    float sVoH = dot(data.V, H);
    float sLoH = dot(data.sampleData.L, H);

    float3 F = DielectricFresnel(dot(data.V, H),  data.eta, 1.0 / data.eta);
    float srqtDenom = sVoH + data.eta * sLoH;
    float t = (data.eta) / srqtDenom;
    
    float D = GGX_D(data.sampleData.H, data.ax, data.ay);
    float G1 = GGX_G1(data.V, data.ax, data.ay);
    float G2 = GGX_G1(data.sampleData.L, data.ax, data.ay);

    f =  (V_ONE - F) * data.color * abs(D * G1 * G2 * t * t * sLoH * sVoH / ev.NoV);
    f *= (1.0 - data.metallic) * data.transmissionPower;

    float dwh_dwi = data.eta * data.eta * abs(dot(data.sampleData.L, H)) / (srqtDenom * srqtDenom);
        
    pdf =  D * G1 / max(0.0001, 4.0 * ev.NoV);
    pdf *= dwh_dwi;
}




