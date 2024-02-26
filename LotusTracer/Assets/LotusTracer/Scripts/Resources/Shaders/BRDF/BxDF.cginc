#include "..\BRDF\BXDF_Evaluation.cginc"
#include "..\BRDF\BXDF_Sample.cginc"

void GetBSDF_F(inout uint randState, inout ScatteringData data, out float3 eval, out float pdf)
{
    ScatteringToLocal(data);
    
    eval = 0;
    pdf = 0;

    data.sampleData.H = normalize(data.sampleData.L + data.V);    
    
    EvaluationVars ev;
    ev.NoL = data.sampleData.L.y;
    ev.NoV = data.V.y;
    ev.NoH = data.sampleData.H.y;
    ev.VoH = dot(data.V, data.sampleData.H);
    ev.VoL = dot(data.V, data.sampleData.L);;
    ev.FL = SchlickWeight(ev.NoL);
    ev.FV = SchlickWeight(ev.NoV);
    ev.squareR = data.roughness * data.roughness;


    float3 tempF;
    float tempPDF;
    
    // eval will be divided by pdf later on (due the rendering equation)  
    // so you might think its dumb to multiply and divide by the same value (for instance probs.prClearCoat)
    // however, the pdf is used in other places (like MIS) so it's better to have the whole thing in there
    // if you are careful enough to conserv the values all througout the codebase you can save some little calculation
    // by skipping those factors, however I found too easy to forget things and make energy go crazy, so I opted
    // for not using implicit simplifications for now
    
    if(data.isReflection && data.probs.wDiffuseReflection > 0.0)
    {
        tempF = tempPDF = 0;
        
        if( ((data.flags >> 1) & 0x1)  == 1)
            Evaluate_Diffuse_OrenNayar(tempF, tempPDF, data, ev);
        else 
            Evaluate_Diffuse_Lambert(tempF, tempPDF, data, ev);

        eval += tempF * data.probs.wDiffuseReflection;
        pdf += tempPDF * data.probs.wDiffuseReflection;
    }
    
    if(data.isReflection && data.probs.wSpecularReflection > 0.0)
    {
        tempF = tempPDF = 0;
        
        Evaluate_Specular(tempF, tempPDF, data, ev);

        eval += tempF *  data.probs.wSpecularReflection;
        pdf += tempPDF * data.probs.wSpecularReflection;
    }

    if(!data.isReflection && data.probs.wSpecularTransmission > 0.0)
    {
        tempF = tempPDF = 0;
        Evaluate_Transmission(tempF, tempPDF, data, ev);
        eval += tempF *  data.probs.wSpecularTransmission;
        pdf += tempPDF * data.probs.wSpecularTransmission;
    }
    
    // if(data.probs.wDiffuseTransmission > 0.0)
    // {
    //     eval += float3(0.7, 0.7, 0.7);
    //     pdf += ONE_OVER_PI;
    //     
    //     // note: some people multiply probabilities by the refraction fresnel
    //     // however, since I'm modifying the probabilities themselves based on the fresnel
    //     // I think it's already contemplated? It looks fine anyways, but might need to revisit
    //     // tempF = tempPDF = 0;
    //     // Evaluate_Transmission(tempF, tempPDF, data, ev);
    //     // eval += tempF *  data.probs.weightTransmission;
    //     // pdf += tempPDF * data.probs.prTransmission;
    // }


    //
    // if(data.isReflection && data.probs.prClearCoat > 0.0)
    // {
    //     Evaluate_ClearCoat(data);        
    //     eval += data.sampleData.sampleReflectance * data.probs.prClearCoat;
    //     pdf  += data.sampleData.pdf * data.probs.prClearCoat;
    // }
    //
    
    eval *= abs(data.sampleData.L.y); // N dot L from the Rendering equation
   
    
    ScatteringToWorld(data);
}

bool GetBSDF_Sample(inout uint randState, inout ScatteringData data)
{
    ScatteringToLocal(data);
    
    bool validSample = false;

    if(data.probs.totalW <= 0)
        return false;
    
    float randomSample = GetRandom0to1(randState);
    if(randomSample < data.probs.wRangeDiffuseReflection)
    {
        validSample = Sample_Diffuse(randState, data);
    }
    else if(randomSample < data.probs.wRangeSpecularReflection)
    {
        // same direction for both dielectric and metallic speculars
        validSample = Sample_Specular(randState, data);
    }
    else if(randomSample < data.probs.wRangeDiffuseTransmission)
    {
        validSample = Sample_Transmission(randState, data);
    }
    else if(randomSample < data.probs.wRangeSpecularTransmission)
    {
        
        validSample = Sample_Transmission(randState, data);
    }
    else // clear coat
    {
        //validSample = Sample_ClearCoat(randState, data);
    }

    data.isReflection = data.sampleData.L.y * data.V.y > 0;
    
    ScatteringToWorld(data);

    return validSample;
}