
#include "..\BRDF\distribution.cginc"
#include "..\BRDF\BXDF_Evaluation.cginc"
#include "..\BRDF\BXDF_Sample.cginc"


void GetBSDF_F(inout uint randState, inout ScatteringData data, out float3 eval, out float pdf)
{
    ScatteringToLocal(data);
    
    eval = 0;
    pdf = 0;

    
    if(data.isReflection)
    {
        data.H = normalize(data.L + data.V);    
    }
    else
    {
        data.H = normalize(data.V + data.L * data.eta);
        if(data.H.y < 0)
            data.H = - data.H;
    }
    
    
    EvaluationVars ev = MakeEvaluationVars(data);

    float3 tempF = (float3) 0;
    float tempPDF = 0;
    
    // eval will be divided by pdf later on (due the rendering equation)  
    // so you might think its dumb to multiply and divide by the same value (for instance probs.prClearCoat)
    // however, the pdf is used in other places (like MIS) so it's better to have the whole thing in there
    // if you are careful enough to conserv the values all througout the codebase you can save some little calculation
    // by skipping those factors, however I found too easy to forget things and make energy go crazy, so I opted
    // for not using implicit simplifications for now
    
    if(data.isReflection && data.probs.prDiffuse > 0.0)
    {
        if( ((data.flags >> 1) & 0x1)  == 1)
            Evaluate_Diffuse_OrenNayar(tempF, tempPDF, data, ev);
        else 
            Evaluate_Diffuse_Lambert(tempF, tempPDF, data, ev);

        eval += tempF * data.probs.wDielectric;
        pdf += tempPDF * data.probs.prDiffuse;
    }
    
    if(data.isReflection && data.probs.prDielectric > 0.0)
    {
        float3 F = SchlickFresnel_V(saturate(data.cSpec0), ev.VoH );
        Evaluate_Specular(tempF, tempPDF, data, ev, F);

        eval += tempF *  data.probs.wDielectric;
        pdf += tempPDF * data.probs.prDielectric;
    }

    if(data.isReflection && data.probs.prMetallic > 0.0)
    {
        float3 F = lerp(data.color, V_ONE, SchlickWeight(ev.VoH));
        Evaluate_Specular(tempF, tempPDF, data, ev, F);

        eval += tempF *  data.probs.wMetal;
        pdf += tempPDF * data.probs.prMetallic;
    }
    

    if(data.probs.prGlass > 0.0)
    {        
        Evaluate_Transmission(tempF, tempPDF, data, ev);   
        eval += tempF *  data.probs.wGlass;
        pdf += tempPDF * data.probs.prGlass;
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
    
    float randomSample = GetRandom0to1(randState);
    if(randomSample < data.probs.prRange_Diffuse)
    {
        validSample = Sample_Diffuse(randState, data);
        data.sampledType = SAMPLE_DIFFUSE;
    }
    // both dielectric and metallic
    else if(randomSample < data.probs.prRange_Metallic)
    {
        // same direction for both dielectric and metallic speculars
        validSample = Sample_Specular(randState, data);
        data.sampledType = SAMPLE_SPECULAR;
    }
    else if(randomSample < data.probs.prRange_Glass)
    {
        float scaledR = (randomSample - data.probs.prRange_Metallic) / (data.probs.prRange_Glass - data.probs.prRange_Metallic);        
        validSample = Sample_Transmission(randState, data, scaledR);
        data.sampledType = SAMPLE_TRANSMISSION;
    }
    else if(randomSample < data.probs.prRange_ClearCoat)
    {
        validSample = Sample_ClearCoat(randState, data);
    }

    data.isReflection = data.L.y * data.V.y > 0;

    bool isValid = abs(data.L.y) > EPSILON;
    
    ScatteringToWorld(data);

    return isValid && validSample;
}