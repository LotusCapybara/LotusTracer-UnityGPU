static void Evaluate_Diffuse_Lambert(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    f +=  data.color * ONE_OVER_PI * abs(ev.NoL);
    
    pdf += ONE_OVER_PI;
}

static void Evaluate_Diffuse_OrenNayar(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    float sintheta_i = SIN_THETA(data.L);
    float sintheta_o = SIN_THETA(data.V);

    float sinphii = SIN_PHI(data.L);
    float cosphii = COS_PHI(data.L);
    float sinphio = SIN_PHI(data.V);
    float cosphio = COS_PHI(data.V);
    float dcos = cosphii * cosphio + sinphii * sinphio;
    if( dcos < 0.0 )
        dcos = 0.0;

    float abs_cos_theta_o = (float) ABS_COS_THETA(data.V);
    float abs_cos_theta_i = (float) ABS_COS_THETA(data.L);

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
    
    f = data.color * ONE_OVER_PI * ( A + B * dcos * sinalpha * tanbeta );     
    pdf = ONE_OVER_PI;
}

static void Evaluate_Specular(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    f = V_ZERO;
    pdf = 0;

    if(!data.isReflection)
        return;
    
    float aNoV = abs(ev.NoV);
    
    // this is perfect internal reflection basically, I'm skipping it
    if(aNoV == 0)
        return;
    
    if( Luminance(data.cSpec0) <= 0 )
        return;
    
    float3 F = SchlickFresnel_V(data.cSpec0, dot(data.V, data.H) );
    float D = D_GGX(data.H, data.ax, data.ay);
    float G1 = G_GGX(data.V, data.ax, data.ay);
    float G2 = G_GGX(data.L, data.ax, data.ay);
    
    f = D * F * G1 * G2 / ( 4.0 * aNoV );

    float EoH = abs(ev.VoH);
    pdf = PDF_GGX(data) / (4.0  * EoH);
}

static void Evaluate_ClearCoat(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    float aVOH = dot(data.V, data.H);
    
    float F = lerp(0.04, 1.0, SchlickWeight(aVOH));
    float D = D_GTR( abs( data.H.y), data.clearCoatRoughness);
    float G = G_Smith( abs(data.L.y), 0.25) * G_Smith(abs(data.V.y), 0.25);
    float jacobian = 1.0 / (4.0 * aVOH);

    f =  ((float3) F) * D * G;
    pdf = D * data.H.y * jacobian;
}

static void Evaluate_Transmission(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    if(data.V.y * data.L.y > 0)
        return;
    if(ev.NoV == 0)
        return;
    
    float3 H = normalize(data.V + data.L * data.eta);
    if(H.y < 0)
        H = -H;
    
    float sVoH = dot(data.V, H);
    float sLoH = dot(data.L, H);

    float3 F = SchlickFresnel(dot(data.V, H),  data.eta) / (1.0 - data.cSpec0);
    F = clamp(F, V_ZERO, V_ONE);
    F = lerp(data.cSpec0, V_ONE, F);
    
    float srqtDenom = sVoH + data.eta * sLoH;
    float t = (data.eta) / srqtDenom;
    
    float D = D_GGX(data.H, data.ax, data.ay);
    float G1 = G_GGX(data.V, data.ax, data.ay);
    float G2 = G_GGX(data.L, data.ax, data.ay);

    f =  data.color * abs(D * G1 * G2 * t * t * sLoH * sVoH / ev.NoV);
    f *= (1.0 - data.metallic) * data.transmissionPower;

    float dwh_dwi = data.eta * data.eta * abs(dot(data.L, H)) / (srqtDenom * srqtDenom);
        
    pdf =  D * G1 / max(0.0001, 4.0 * ev.NoV);
    pdf *= dwh_dwi;
}




