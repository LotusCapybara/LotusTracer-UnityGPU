
#include "..\BRDF\distribution.cginc"
#include "..\BRDF\BXDF_Evaluation.cginc"
#include "..\BRDF\BXDF_Sample.cginc"

void GetBSDF_F(inout uint randState, inout ScatteringData data, out float3 eval, out float pdf)
{
    ScatteringToLocal(data);
    
    eval = 0;
    pdf = 0;

    data.H = normalize(data.L + data.V);    
    
    EvaluationVars ev;
    ev.NoL = data.L.y;
    ev.NoV = data.V.y;
    ev.NoH = data.H.y;
    ev.VoH = dot(data.V, data.H);
    ev.VoL = dot(data.V, data.L);;
    ev.FL = SchlickWeight(ev.NoL);
    ev.FV = SchlickWeight(ev.NoV);
    ev.squareR = data.roughness * data.roughness;


    float3 tempF = (float3) 0;
    float tempPDF = 0;
    
    // eval will be divided by pdf later on (due the rendering equation)  
    // so you might think its dumb to multiply and divide by the same value (for instance probs.prClearCoat)
    // however, the pdf is used in other places (like MIS) so it's better to have the whole thing in there
    // if you are careful enough to conserv the values all througout the codebase you can save some little calculation
    // by skipping those factors, however I found too easy to forget things and make energy go crazy, so I opted
    // for not using implicit simplifications for now
    
    if(data.isReflection && data.probs.wDiffuseReflection > 0.0)
    {
        if( ((data.flags >> 1) & 0x1)  == 1)
            Evaluate_Diffuse_OrenNayar(tempF, tempPDF, data, ev);
        else 
            Evaluate_Diffuse_Lambert(tempF, tempPDF, data, ev);

        eval += tempF * data.probs.wDiffuseReflection;
        pdf += tempPDF * data.probs.wDiffuseReflection;
    }
    
    if(data.isReflection && data.probs.wSpecularReflection > 0.0)
    {
        Evaluate_Specular(tempF, tempPDF, data, ev);

        eval += tempF *  data.probs.wSpecularReflection;
        pdf += tempPDF * data.probs.wSpecularReflection;
    }

    if(data.probs.wTransmission > 0.0)
    {
        float3 F = DielectricFresnel(dot(data.V, data.H),  data.eta);
        
        if(data.isReflection)
        {
            Evaluate_Specular(tempF, tempPDF, data, ev);
            tempPDF *= F;
        }
        else
        {
            Evaluate_Transmission(tempF, tempPDF, data, ev);
            tempPDF *= (V_ONE - F);
        }        
        
        eval += tempF *  data.probs.wTransmission;
        pdf += tempPDF * data.probs.wTransmission;
    }

    // todo: something is wrong with Clear Coat, it's "eating" energy
    // can't find if it's negative values or some exception or what
    // if( data.isReflection && data.probs.wClearCoat > 0.0)
    // {
    //     Evaluate_ClearCoat(tempF, tempPDF, data, ev);        
    //     eval += tempF * data.probs.wClearCoat;
    //     pdf  += tempPDF * data.probs.wClearCoat;
    // }    
    
    eval *= abs(data.L.y); // N dot L from the Rendering equation   
    
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
    else if(randomSample < data.probs.wRangeTransmission)
    {
        validSample = Sample_Transmission(randState, data);
    }
    else // clear coat
    {
        validSample = Sample_ClearCoat(randState, data);
    }

    data.isReflection = data.L.y * data.V.y > 0;
    
    ScatteringToWorld(data);

    return validSample;
}