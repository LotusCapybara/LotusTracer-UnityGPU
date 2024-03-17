

SampleProbabilities CreateProbabilities(inout uint randState, in ScatteringData data)
{
    // weight of each of these models based on material properties
    // I was using a more straight forward weight sampling, but I found this implementation online
    // (GLSL-Pathtracer from tigrazone) and I liked it.
    // I dont know if it's based on something else, but I like the idea of have overall weights and then
    // probabilities based on the "bigger" lobes. Also I was not too happy with how dielectric vs metallic
    // looked and I thought by separating it more clearly like this I can improve it a bit
    
    SampleProbabilities prob;
    prob.wDielectric = (1.0 - data.metallic) * (1.0 - data.transmissionPower);
    prob.wMetal = data.metallic;
    prob.wGlass = (1.0 - data.metallic) * data.transmissionPower;

    // Lobe probabilities
    float schlickWt = SchlickWeight(data.V.y);

    prob.prDiffuse =  prob.wDielectric * Luminance(data.color);
    prob.prDielectric = prob.wDielectric * Luminance(lerp(data.cSpec0, V_ONE, schlickWt));
    prob.prMetallic = prob.wMetal * Luminance(lerp(data.color, V_ONE, schlickWt));
    prob.prGlass = prob.wGlass;
    prob.prClearCoat = 0.25 * data.clearCoat;
    
    float invWTotal = 1.0 /
        (prob.prDiffuse + prob.prDielectric + prob.prMetallic + prob.prGlass + prob.prClearCoat);

    prob.prDiffuse *= invWTotal;
    prob.prDielectric *= invWTotal;
    prob.prMetallic *= invWTotal;
    prob.prGlass *= invWTotal;
    prob.prClearCoat *= invWTotal;

    prob.prRange_Diffuse = prob.prDiffuse;
    prob.prRange_Dielectric = prob.prRange_Diffuse + prob.prDielectric;
    prob.prRange_Metallic = prob.prRange_Dielectric + prob.prMetallic;
    prob.prRange_Glass = prob.prRange_Metallic + prob.prGlass;
    prob.prRange_ClearCoat = prob.prRange_Glass + prob.prClearCoat;
    
    return prob;
}

ScatteringData MakeScatteringData(inout uint randState, in TriangleHitInfo hitInfo)
{
    RenderMaterial mat;

    if(hitInfo.isTriangle)
    {
        mat = _Materials[hitInfo.materialIndex];        
    }
    else
    {
        mat = (RenderMaterial) -1;
        mat.color = _Lights[hitInfo.materialIndex].color;
        mat.emissiveIntensity = _Lights[hitInfo.materialIndex].intensity;
        mat.mediumDensity = 0;
    }
    
    ScatteringData data;
    data.isReflection = false;
    data.L = (float) 0;
    data.H = (float) 0;
    data.sampledType = 0;
    
    data.surfacePoint = hitInfo.position;
    data.V = hitInfo.backRayDirection;
    data.color = mat.color * hitInfo.vertexColor;
    
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
    data.emissionPower = mat.emissiveIntensity;
    data.transmissionPower = saturate(mat.transmissionPower); 

    mat.ior = data.transmissionPower > 0.0 ? clamp(mat.ior, 1.0001, 2.0) : mat.ior;
    data.eta = hitInfo.isFrontFace ? 1.0 / mat.ior  : mat.ior;
    data.ior1 =  hitInfo.isFrontFace ? 1.0 : mat.ior;
    data.ior2 =  hitInfo.isFrontFace ? mat.ior : 1.0;
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

        data.roughness = _AtlasesRoughness
            .SampleLevel(sampler_AtlasesRoughness, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;

        if( ((mat.flags >> 13) & 0x1) == 1)
            data.roughness = V_ONE - data.roughness;

        data.roughness *= mat.roughness;
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

    float aspect = sqrt(1.0 - mat.anisotropic * 0.9);
    data.ax = max(0.000001, (mat.roughness * mat.roughness) / aspect);
    data.ay = max(0.000001, (mat.roughness * mat.roughness) * aspect);

    // F0 for dielectrics can vary really based on the eta but I found that 0.1 is all right for
    // most cases in this implementation. It looks dielectric enough and the specularity is strong
    // higher values will make it look too metallic-ish already and lower values will dim the specularity
    // too much imo
    // Many implementations would add an "specularity" value in the materials, I guess I can take a look into
    // implementing that at some point? That could include specular color. That's not realistic really, dielectric
    // materials don't have specular color and metallic materials have specific color for their materials.
    // Renderers that use specular color usually do it for artistic freedom, such as Disney renderers
    float minSpec = max(0.1,  SchlickR0FromEta(1.0 / data.eta));
    // todo: replace V_ONE by "specularColor * specularPower"
    data.cSpec0 = SLERP(minSpec * V_ONE, data.color, mat.metallic);
    
    data.probs =  CreateProbabilities(randState, data);

    data.flags = mat.flags;
    
    return data;
}

EvaluationVars MakeEvaluationVars(in ScatteringData data)
{
    EvaluationVars ev;
    ev.NoL = data.L.y;
    ev.NoV = data.V.y;
    ev.NoH = data.H.y;
    ev.VoH = dot(data.V, data.H);
    ev.VoL = dot(data.V, data.L);;
    ev.FL = SchlickWeight(ev.NoL);
    ev.FV = SchlickWeight(ev.NoV);
    ev.squareR = data.roughness * data.roughness;

    return ev;
}

