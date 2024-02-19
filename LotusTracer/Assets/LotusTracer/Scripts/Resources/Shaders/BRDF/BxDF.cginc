// Support functions
// ReSharper disable All
#include "..\BRDF\BXDF_Evaluation.cginc"
#include "..\BRDF\BXDF_Sample.cginc"



void GetBSDF_F(inout uint randState, inout ScatteringData data, out float3 eval, out float pdf)
{
    ScatteringToLocal(data);
    
    eval = 0;
    pdf = 0;

    // eval will be divided by pdf later on (due the rendering equation)  
    // so you might think its dumb to multiply and divide by the same value (for instance probs.prClearCoat)
    // however, the pdf is used in other places (like MIS) so it's better to have the whole thing in there
    // if you are careful enough to conserv the values all througout the codebase you can save some little calculation
    // by skipping those factors, however I found too easy to forget things and make energy go crazy, so I opted
    // for not using implicit simplifications for now
    
    if(data.isReflection && data.probs.prDiffuse > 0.0)
    {
        Evaluate_Diffuse(data);
        eval += data.sampleData.sampleReflectance * data.probs.weightDielectric;
        pdf  += data.sampleData.pdf * data.probs.prDiffuse;
    }
    if(data.isReflection && data.probs.prSpecular > 0.0)
    {
        Evaluate_Specular(data);
        
        eval += data.sampleData.sampleReflectance * max(data.probs.weightDielectric, data.probs.weightMetallic);
        pdf  += data.sampleData.pdf * data.probs.prSpecular;
    }

    if(data.isReflection && data.probs.prClearCoat > 0.0)
    {
        Evaluate_ClearCoat(data);        
        eval += data.sampleData.sampleReflectance * data.probs.prClearCoat;
        pdf  += data.sampleData.pdf * data.probs.prClearCoat;
    }

    if(data.probs.prTransmission > 0.0)
    {
        // note: some people multiply probabilities by the refraction fresnel
        // however, since I'm modifying the probabilities themselves based on the fresnel
        // I think it's already contemplated? It looks fine anyways, but might need to revisit
        
        Evaluate_Transmission(data);        
        eval += data.sampleData.sampleReflectance * data.probs.weightTransmission;
        pdf  += data.sampleData.pdf * data.probs.prTransmission;
    }

    eval *= abs(data.sampleData.L.y); // N dot L from the Rendering equation
   
    
    ScatteringToWorld(data);
}

bool GetBSDF_Sample(inout uint randState, inout ScatteringData data)
{
    ScatteringToLocal(data);
    
    bool validSample = false;

    float randomSample = GetRandom0to1(randState);
    if(randomSample < data.probs.prRangeDiffuse)
    {
        validSample = Sample_Diffuse(randState, data);
    }
    else if(randomSample < data.probs.prRangeSpecular)
    {
        // same direction for both dielectric and metallic speculars
        validSample = Sample_Specular(randState, data);
    }
    else if(randomSample < data.probs.prRangeClearCoat)
    {
        validSample = Sample_ClearCoat(randState, data);
    }
    else if(randomSample < data.probs.prRangeTransmission)
    {
        // this code rescales the random value that hit in the range of prRangeTransmission
        // and remaps it to 0 to 1. this is used to calculate later inside Sample_Transmission
        // if the ray is refractor or reflected. I was initially just using a new random value, since
        // I thought this was just a way to avoid executing random generation code.
        // However, after reading some more papers, I noticed that for this case, which is a non-uniform
        // distribution, it's better to keep tha actual random we got before to avoid introducing
        // bias and potentially weird results
        float r = randomSample - data.probs.prRangeClearCoat / (data.probs.prRangeTransmission- data.probs.prRangeClearCoat);        
        validSample = Sample_Transmission(randState, r, data);
    }

    data.isReflection = data.sampleData.L.y * data.V.y > 0;
    
    ScatteringToWorld(data);

    return validSample;
}