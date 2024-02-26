

SampleProbabilities CreateProbabilities(inout uint randState, in ScatteringData data)
{
    float baseLuminance = Luminance(data.color);
    
    // weight of each of these models based on material properties
    SampleProbabilities prob;

    
    
    prob.wDiffuseReflection  = baseLuminance * (1.0 - data.metallic) * (1.0 -  data.transmissionPower);
    prob.wSpecularReflection  = Luminance(data.F0) * 8.0 * (1.0 - data.roughness); // magic number 8.0 to avoid fireflies?
    prob.wDiffuseTransmission = 0; //baseLuminance * (1.0 - data.metallic) * data.transmissionPower * data.roughness;
    prob.wSpecularTransmission = baseLuminance * (1.0 - data.metallic) * data.transmissionPower;
    prob.wClearCoat = 0.05 * data.clearCoat;

    
    
    prob.totalW = prob.wDiffuseReflection + prob.wSpecularReflection + prob.wDiffuseTransmission +
        prob.wSpecularTransmission +  prob.wClearCoat;

    float invWTotal = 1.0 / prob.totalW;

    prob.wRangeDiffuseReflection  = prob.wDiffuseReflection * invWTotal;
    prob.wRangeSpecularReflection = prob.wRangeDiffuseReflection + prob.wSpecularReflection * invWTotal;
    prob.wRangeDiffuseTransmission = prob.wRangeSpecularReflection + prob.wDiffuseTransmission * invWTotal;
    prob.wRangeSpecularTransmission = prob.wRangeDiffuseTransmission + prob.wSpecularTransmission * invWTotal;
    prob.wRangeClearCoat = 1.0;
    
    return prob;
}


ScatteringData MakeScatteringData(inout uint randState, in TriangleHitInfo hitInfo)
{
    RenderMaterial mat = _Materials[hitInfo.materialIndex];
    
    ScatteringData data;
    data.isReflection = false;
    data.sampleData.L = (float) 0;
    data.sampleData.H = (float) 0;    
    
    data.surfacePoint = hitInfo.position;
    data.V = hitInfo.backRayDirection;
    data.color = mat.color;    
    
    data.WorldNormal = hitInfo.isFrontFace ? hitInfo.normal : - hitInfo.normal;
    data.WorldTangent = hitInfo.tangent;
    data.WorldBiTangent = hitInfo.biTangent;
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
    data.transmissionPower = clamp(mat.transmissionPower, 0.05, 0.95); 

    mat.ior = clamp(mat.ior, 1.0001, 2.0);
    data.eta = dot(hitInfo.normal, hitInfo.backRayDirection) > 0 ? 1.0 / mat.ior  : mat.ior  / 1.0;
    // data.isThin = mat.thi // todo: use material flags to check if it's thin
    data.isThin  = false;
    
    if(mat.albedoMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasAlbedo[mat.albedoMapIndex];
        float2 targetUV = PackedUV(texture_data, hitInfo.textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        data.color *= _AtlasesAlbedo
            .SampleLevel(sampler_AtlasesAlbedo, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }

    if(mat.roughMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasRoughness[mat.roughMapIndex];
        float2 targetUV = PackedUV(texture_data, hitInfo.textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        data.roughness *= _AtlasesRoughness
            .SampleLevel(sampler_AtlasesRoughness, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }   

    if(mat.metalMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasMetallic[mat.metalMapIndex];
        float2 targetUV = PackedUV(texture_data, hitInfo.textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        data.metallic *= _AtlasesMetallic
            .SampleLevel(sampler_AtlasesMetallic, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }

    if(data.emissionPower > 0 && mat.emissionMapIndex >= 0)
    {        
        TextureData texture_data = _MapDatasEmission[mat.emissionMapIndex];
        float2 targetUV = PackedUV(texture_data, hitInfo.textureUV);        
        int atlasIndex = texture_data.atlasIndex;
        
        data.emissionPower *= _AtlasesEmission
            .SampleLevel(sampler_AtlasesEmission, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;
    }

    // I found that materials with roughness 0 have an infinitesimally small highlight, so they
    // look black (because they look black-ish in the parts where they don't reflect light)
    // I fixed it by doing this, but I might be doing something wrong in the cook-torrence formulae
    // also, clamping both rough and metallic to 0 and 1, since the shader allow to multiply up to 10 to add
    // more value to the maps
    data.roughness = clamp(data.roughness, EPSILON, 1.0);
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
    float minSpec = max(0.1,  SchlickR0FromEta(1.0 / data.eta));
    data.F0 = lerp(minSpec * V_ONE, data.color, mat.metallic);
    
    data.probs =  CreateProbabilities(randState, data);

    data.flags = mat.flags;
    
    return data;
}