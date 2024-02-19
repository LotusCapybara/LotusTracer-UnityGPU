// In order: Diffuse, Specular, Coat, Transmission

static void Evaluate_Diffuse(inout ScatteringData data)
{
    data.sampleData.sampleReflectance = V_ZERO;
    data.sampleData.pdf = 0.0;
        
    float NoL = data.sampleData.L.y;
    float NoV = data.V.y;
    if ( NoL <= 0.0 || NoV <= 0.0 )
        return;

    float3 H = normalize(data.sampleData.L + data.V);
    float LoH = dot(data.sampleData.L,H);    
    
    float roughness2 = data.roughness * data.roughness;    
    float FD90 = 0.5 + 2.0 * roughness2 * pow(LoH,2.0);
    float a = SchlickFresnel(1.0, FD90, NoL);
    float b = SchlickFresnel(1.0, FD90, NoV);

    float3 disneyDiffuse = data.color * (a * b / PI);

    float3 F = SchlickFresnel_V(data.F0, dot(data.V, H));
    
    data.sampleData.sampleReflectance = disneyDiffuse * (1.0 - F);
    data.sampleData.pdf  = NoL/PI;
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
    
    data.sampleData.sampleReflectance = D * F * G1 * G2 / ( 4.0 * abs(NoV) );    
    data.sampleData.pdf  = D * G1 / max(0.0001, 4.0 * NoV);
}

// evaluate specular dielectric BRDF with micro facet reflections
static void Evaluate_ClearCoat(inout ScatteringData data)
{
    float VoH = dot(data.V, data.sampleData.H);

    float F = lerp(0.04, 1.0, SchlickWeight(VoH));
    float D = GTR1(data.sampleData.H.y, data.clearCoatRoughness);
    float G = Smith_G(data.sampleData.L.y, 0.25) * Smith_G(data.V.y, 0.25);
    float jacobian = 1.0 / (4.0 * VoH);

    data.sampleData.sampleReflectance = ((float3) F) * D * G;    
    data.sampleData.pdf  = D * data.sampleData.H.y * jacobian;
}

static void Evaluate_Transmission(inout ScatteringData data)
{
    if(data.isReflection)
    {
        Evaluate_Specular(data);
        data.sampleData.pdf *= data.sampleData.refractF;
    }
    else
    {
        // this implementation is garbage. It basically provides a linear/constant energy
        // regardless of how the light enters the surface, but it's fine for a quick
        // implementation....... I'll implement a proper one later on
        data.sampleData.sampleReflectance = pow(data.color, (float3) 0.5);
        data.sampleData.pdf  = 1.0 * (1.0 - data.sampleData.refractF);
    }
}




