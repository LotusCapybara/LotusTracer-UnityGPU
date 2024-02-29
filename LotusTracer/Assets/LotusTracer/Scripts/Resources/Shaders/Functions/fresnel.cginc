
float SchlickWeight(float y)
{
    return  pow(saturate(1.0 - y), 5.0);    
}

float SchlickR0FromEta( float rROI )
{
    float r = ( rROI - 1.0 ) / ( rROI + 1.0 ) ;
    return r * r;
}

float3 SchlickFresnel_V( float3 f0 , float theta )
{
    return f0 + (1.0 - f0) * pow(1.0 - theta, 5.0);
}

float SchlickFresnel(float f0, float theta)
{
    return f0 + SchlickWeight(theta) * ( 1.0 - f0 );
}

float DielectricFresnel(float cosThetaI, float eta)
{
    eta = 1 / eta;
    float sinThetaTSq = eta * eta * (1.0 - cosThetaI * cosThetaI);

    // Total internal reflection
    if (sinThetaTSq > 1.0)
        return 1.0;

    float cosThetaT = sqrt(max(1.0 - sinThetaTSq, 0.0));

    float rs = (eta * cosThetaT - cosThetaI) / (eta * cosThetaT + cosThetaI);
    float rp = (eta * cosThetaI - cosThetaT) / (eta * cosThetaI + cosThetaT);

    return 0.5 * (rs * rs + rp * rp);
}

