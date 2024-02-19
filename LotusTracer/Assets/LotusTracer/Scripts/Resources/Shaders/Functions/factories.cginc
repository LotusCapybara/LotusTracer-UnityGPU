

SampleProbabilities CreateProbabilities(inout uint randState, in ScatteringData data)
{   
    // weight of each of these models based on material properties
    SampleProbabilities prob;
    
    prob.weightDielectric   = (1.0 - data.metallic) * (1.0 - data.transmissionPower);  // diffuse and specular
    prob.weightMetallic     = data.metallic;                                 // metallic specular
    prob.weightTransmission = (1.0 - data.metallic) * data.transmissionPower;          // internal specular-opacity;   

    prob.prDiffuse      = prob.weightDielectric * Luminance(data.color);
    prob.prSpecular     = max(prob.weightDielectric, prob.weightMetallic) * Luminance(data.color);
    prob.prTransmission = prob.weightTransmission;
    prob.prClearCoat    = 0.25 * data.clearCoat;   // disney bsdf papers explain this 0.25 (or similar values)

    // in reality, even roughness materials have specular probabilities and the specular sample will
    // take roughness into account so the material will look rough
    // however, I found that by doing this, I can gain some lil performance thanks to diffuse sampling and evaluation
    // being cheaper to execute. So I can "safely" assume when roughness and metallic are 0, it's just the same
    // to execute full diffuse probability
    // Also, I found that allowing specular sampling in pure diffuse materials introduce more fireflies, while
    // gaining basically nothing in terms of quality
    if(data.roughness <= 0 && data.metallic <= 0)
    {
        prob.prDiffuse = 1.0;
        prob.prSpecular = 0;
    }

    // normalization of probabilities. So basically all probabilities together sum up to 1.0
    float normalInvPr = 1.0 /
        (prob.prDiffuse + prob.prSpecular + prob.prTransmission + prob.prClearCoat);

    // normalization of all probabilities (all together sums up to 1)
    prob.prDiffuse      *= normalInvPr;
    prob.prSpecular     *= normalInvPr;    
    prob.prTransmission *= normalInvPr;
    prob.prClearCoat    *= normalInvPr;

    // these are "ranges" of each probabilities from 0 to 1, so later they can be picked up
    // doing a random 0 to 1
    prob.prRangeDiffuse = prob.prDiffuse;
    prob.prRangeSpecular = prob.prRangeDiffuse + prob.prSpecular;
    prob.prRangeClearCoat = prob.prRangeSpecular + prob.prClearCoat;
    prob.prRangeTransmission = prob.prRangeClearCoat + prob.prTransmission;   
    
    return prob;
}


ScatteringData MakeScatteringData(
    inout uint randState, in float3 wo, in float3 surfacePoint, in float3 worldNormal, bool isFrontFace, int matIndex, float2 textureUV)
{
    RenderMaterial mat = _Materials[matIndex];
    
    ScatteringData data;
    data.isReflection = false;
    data.sampleData.L = (float) 0;
    data.sampleData.H = (float) 0;    
    data.sampleData.refractF = 0;
    data.sampleData.refractH = V_ZERO;
    
    data.surfacePoint = surfacePoint;    
    data.V = wo;
    data.color = mat.color;    
    
    data.WorldNormal = isFrontFace ? worldNormal : - worldNormal;
    data.roughness = mat.roughness;
    data.clearCoat = mat.clearCoat;
    data.clearCoatRoughness = data.clearCoat > 0 ? mat.clearCoatRoughness : 1.0;

    if(data.clearCoatRoughness > 0)
    {
        // otherwise it's basically the same as binary 0-1
        // pow 25 might be too much? but it looks good imo
        data.clearCoatRoughness = pow(data.clearCoatRoughness, 25);    
    }    
    
    data.metallic = mat.metallic;    
    
    data.mediumDensity = mat.mediumDensity;
    data.scatteringDirection = clamp(mat.scatteringDirection, -0.95, 0.95);
    data.maxScatteringDistance = mat.maxScatteringDistance;
    data.emissionPower = mat.emissiveIntensity;
    data.transmissionPower = data.mediumDensity >= 1 ? 0 : clamp(mat.transmissionPower, 0, 0.95);
    
    data.eta = isFrontFace ? 1.0 / mat.ior  : mat.ior  / 1.0; 
    
    if(mat.albedoMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasAlbedo[mat.albedoMapIndex];
        float2 targetUV = PackedUV(texture_data, textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        data.color *= _AtlasesAlbedo
            .SampleLevel(sampler_AtlasesAlbedo, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }

    if(mat.roughMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasRoughness[mat.roughMapIndex];
        float2 targetUV = PackedUV(texture_data, textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        data.roughness *= _AtlasesRoughness
            .SampleLevel(sampler_AtlasesRoughness, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }   

    if(mat.metalMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasMetallic[mat.metalMapIndex];
        float2 targetUV = PackedUV(texture_data, textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        data.metallic *= _AtlasesMetallic
            .SampleLevel(sampler_AtlasesMetallic, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }

    if(data.emissionPower > 0 && mat.emissionMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasEmission[mat.emissionMapIndex];
        float2 targetUV = PackedUV(texture_data, textureUV);        
        int atlasIndex = texture_data.atlasIndex;
        
        data.emissionPower *= _AtlasesEmission
            .SampleLevel(sampler_AtlasesEmission, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }
    
    // if(mat.emissiveIntensity >= 0 && data.emissionPower > 0)
    //     data.color = _MapsEmission.SampleLevel(sampler_MapsEmission, float3(uv.x, uv.y, mat.emissionMapIndex) , 0).rgb;
    //

    // I found that materials with roughness 0 have an infinitesimally small highlight, so they
    // look black (because they look black-ish in the parts where they don't reflect light)
    // I fixed it by doing this, but I might be doing something wrong in the cook-torrence formulae
    // also, clamping both rough and metallic to 0 and 1, since the shader allow to multiply up to 10 to add
    // more value to the maps
    data.roughness = clamp(data.roughness, 0.001, 1);
    data.metallic = saturate(data.metallic);

    // I'll implement anisotropic materials at some point
    // It should be easy, the distribution functions already support it and I just need to
    // use a variable in the materials to displace the symmetry
    data.ax = data.roughness  * data.roughness;
    data.ay = data.roughness  * data.roughness;

    // F0 for dielectrics can vary really based on the eta but I found that 0.1 is all right for
    // most cases in this implementation. It looks dielectric enough and the specularity is strong
    // higher values will make it look too metallic-ish already and lower values will dim the specularity
    // too much imo
    // Many implementations would add an "specularity" value in the materials, I guess I can take a look into
    // implementing that at some point? That could include specular color. That's not realistic really, dielectric
    // materials don't have specular color and metallic materials have specific color for their materials.
    // Renderers that use specular color usually do it for artistic freedom, such as Disney renderers
    data.F0 = lerp((float3)0.1, data.color, mat.metallic);
    
    // default event, to be overriden if trasmit
    data.sampleData.pdf = 0;
    data.sampleData.sampleReflectance = 0;
    
    data.probs =  CreateProbabilities(randState, data);
    
    return data;
}


ScatteringData MakeScatteringData_FromHitInfo(inout uint randState, in TriangleHitInfo hitInfo)
{
    return MakeScatteringData(
        randState, hitInfo.backRayDirection, hitInfo.position, hitInfo.normal, hitInfo.isFrontFace, hitInfo.materialIndex, hitInfo.textureUV);
}