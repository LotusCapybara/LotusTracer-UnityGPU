static void Evaluate_Diffuse_Lambert(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    f += data.color * ONE_OVER_PI * abs(ev.NoL);
    
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

static void Evaluate_Specular(inout float3 f, inout float pdf, in ScatteringData data, in EvaluationVars ev, float3 F)
{
    f = V_ZERO;
    pdf = 0;

    if(!data.isReflection)
        return;
    
    float aNoV = abs(ev.NoV);
    if(aNoV == 0)
        return;

    float D = D_GGX(data.H, data.ax, data.ay);
    float G1 = G_GGX(data.V, data.ax, data.ay);
    float G2 = G_GGX(data.L, data.ax, data.ay);
    
    f = D * F * G1 * G2 / ( 4.0 * aNoV );
    pdf = D * G1 / (4.0  * data.V.y * data.L.y);
}

// static void Evaluate_ClearCoat(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
// {
//     float aVOH = dot(data.V, data.H);
//     
//     float F = lerp(0.04, 1.0, SchlickWeight(aVOH));
//     float D = D_GTR( abs( data.H.y), data.clearCoatRoughness);
//     float G = G_Smith( abs(data.L.y), 0.25) * G_Smith(abs(data.V.y), 0.25);
//     float jacobian = 1.0 / (4.0 * aVOH);
//
//     f =  ((float3) F) * D * G;
//     pdf = F * D * data.H.y * jacobian;
// }

static void Evaluate_Transmission(inout float3 f, inout float pdf, inout ScatteringData data, in EvaluationVars ev)
{
    f = V_ZERO;
    pdf = 0;

    if(ev.NoV == 0)
        return;

    float F =  DielectricFresnel(abs( ev.VoH), data.eta);
    //float F = SchlickFresnel(data.cSpec0, abs( ev.VoH));
    
    if(data.isReflection)
    {       
        Evaluate_Specular(f, pdf, data, ev, F);
        pdf *= F;
        return;
    }

    float LoH = dot(data.L, data.H);
    
    float D = D_GGX(data.H, data.ax, data.ay);
    float G1 = G_GGX(data.V, data.ax, data.ay);
    float G2 = G_GGX(data.L, data.ax, data.ay);

    float denom2 = pow(LoH + ev.VoH * data.eta , 2.0 );
    float eta2 = data.eta * data.eta;
    float jacobian = abs(LoH) / denom2; 

    float3 T = data.color; //pow(data.color, 0.7);
    
    f = T * (V_ONE - (float3)F) * D * G1 * G2 * abs(ev.VoH) * jacobian * eta2 / abs(data.V.y * data.L.y);    
    // ------- pdf            
    pdf = (1.0 - F) * G1 * max(0,  abs(ev.VoH)) * D * jacobian / data.V.y;     
}




