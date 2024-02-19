#include "random.cginc"

// ---------------------------------------------------

float3 SLERP(float3 a, float3 b, float t)
{
    return a * ( 1.0 - t ) + b * t;
}


float SchlickWeight(float y)
{
    float m = saturate(1.0 - y);
    float m2 = m * m;
    return m2 * m2 * m;
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

float SchlickFresnel(float f0, float f90, float theta)
{
    return f0 + (f90 - f0) * pow(1.0 - theta, 5.0);
}

float DielectricFresnel(float cosThetaI, float eta)
{
    float sinThetaTSq = eta * eta * (1.0 - cosThetaI * cosThetaI);

    // Total internal reflection
    if (sinThetaTSq > 1.0)
        return 1.0;

    float cosThetaT = sqrt(max(1.0 - sinThetaTSq, 0.0));

    float rs = (eta * cosThetaT - cosThetaI) / (eta * cosThetaT + cosThetaI);
    float rp = (eta * cosThetaI - cosThetaT) / (eta * cosThetaI + cosThetaT);

    return 0.5 * (rs * rs + rp * rp);
}


float2 PackedUV(in TextureData texture_data, in float2 uv)
{
    float wScale = (float)texture_data.width / 4096.0;
    float hScale = texture_data.height / 4096.0;

    float startU = texture_data.x / 4096.0;
    float startV = texture_data.y / 4096.0;

    return float2(startU + uv.x * wScale, startV + uv.y * hScale);
}

inline bool SameGlobalHemiSphere(float3 a, float3 b)
{
    return dot(a, b) > 0;
}

float3 SphericalToVector( float theta , float phi )
{
    float x = sin( theta ) * cos( phi );
    float y = cos( theta );
    float z = sin( theta ) * sin( phi );

    return float3(x, y, z);
}

float3 RandomDirectionInHemisphereCosWeighted(inout uint state)
{
    float u = GetRandom0to1(state);
    float v = GetRandom0to1(state);
    float sinTheta = sqrt(1.0 - u * u);
    float phi = 2.0 * PI * v;
    float3 dir;
    dir.x = sinTheta * cos(phi);
    dir.y = u; 
    dir.z = sinTheta * sin(phi);
 
    return normalize( dir );
}



// ----------------------  world to local and that


void CreateCoordinateSystem(in float3 normal, inout float3 tangent, inout float3 biTangent)
{
    float3 up = abs(normal.y) < 0.9999999 ? float3(0, 1, 0) : float3(1, 0, 0);
    tangent = normalize(cross(up, normal));
    biTangent = normalize( cross(normal, tangent) );
}

float3 ToWorld(float3 normal, float3 v)
{
    float3 tangent, bitangent;
    CreateCoordinateSystem(normal, tangent, bitangent);

    return normalize( v.x * tangent + v.z * bitangent + v.y * normal);
}

float3 ToWorldTBN(float3 T, float3 B, float3 N, float3 v)
{
    return normalize( v.x * B + v.y * N + v.z * T);
}

float3 ToWorld2(float3 normal, float3 v, float3 tangent, float3 bitangent)
{
    return normalize( v.x * bitangent + v.y * normal + v.z * tangent);
}


float3 ToLocal(float3 normal, float3 v)
{
    float3 tangent, bitangent;
    CreateCoordinateSystem(normal, tangent, bitangent);
    
    return normalize( float3(dot(v, bitangent), dot(v, normal), dot(v, tangent)));
}

float3 NormalMapToWorld( float3 T, float3 B, float3 N, float3 normalFromMap)
{
    float3x3 TBN = float3x3(T, B, N);
    return normalize( mul(TBN, normalFromMap) );
}

void ScatteringToWorld(inout ScatteringData data)
{
    data.V = ToWorld(data.WorldNormal, data.V);
    data.sampleData.L = ToWorld(data.WorldNormal, data.sampleData.L);
    data.sampleData.H = ToWorld(data.WorldNormal, data.sampleData.H);
}

void ScatteringToLocal(inout ScatteringData data)
{
    data.V = ToLocal(data.WorldNormal, data.V);
    data.sampleData.L = ToLocal(data.WorldNormal, data.sampleData.L);
    data.sampleData.H = ToLocal(data.WorldNormal, data.sampleData.H);
}


float3 UnpackNormalRGB(float3 packedNormal, float3 scale = 1.0)
{
    float3 normal;
    normal.xyz = packedNormal.rgb * 2.0 - 1.0;
    normal.xy *= scale;
    return normal;
}

// ---------------------------------- math / bsdf utils


// this is the brightness of a color as perceived by the human eye
// we humans struggle to capture blue, then red, and we are pretty good
// at capturing green. that's why you will see that digital camera sensors
// have a grid with more green cells than red and blue
// I guess this is some sort of natural evolution to be able to distinguish better
// the tones of green in vegetation? I don't know, I'm just randomly guessing
float Luminance(float3 c)
{
    return 0.212671 * c.r + 0.715160 * c.g + 0.072169 * c.b;
}

inline float COS_THETA(in float3 w)
{
    return w.y;
}

inline float ABS_COS_THETA(in float3 w)
{
    return abs(COS_THETA(w));
}

inline float COS_THETA_2(in float3 w)
{
    return w.y * w.y;
}

inline float SIN_THETA_2(in float3 w)
{
    return max(0, 1.0 - COS_THETA_2(w));
}

inline float TAN_THETA_2(in float3 w)
{
    return 1.0 / COS_THETA_2(w) - 1.0;
}

inline float COS_D_PHI(in float3 w0, in float3 w1)
{
    return clamp(
        ( w0.x * w1.x + w0.z * w1.z ) / sqrt( (w0.x * w0.x + w0.z * w0.z)*(w1.x * w1.x + w1.z*w1.z) )
        , -1.0 , 1.0 );
}

inline float SIN_THETA(in float3 w)
{
    return sqrt(SIN_THETA_2(w));
}

inline float COS_PHI(in float3 w)
{
    float sintheta = SIN_THETA(w);
    if (sintheta == 0)
        return 1;

    return clamp(w.x / sintheta, -1.0, 1.0);
}

inline float SIN_PHI(in float3 w)
{
    float sintheta = SIN_THETA(w);
    if (sintheta == 0)
        return 0;
    
    return clamp(w.z / sintheta, -1.0, 1.0);
}

inline float SIN_PHI_2(in float3 w)
{
    const float sinphi = SIN_PHI(w);
    return sinphi * sinphi;
}

inline float COS_PHI_2(in float3 w)
{
    const float cosphi = COS_PHI(w);
    return cosphi * cosphi;
}

inline float TAN_THETA(in float3 w)
{
    return SIN_THETA(w) / COS_THETA(w);
}

inline float SQUARE(float f)
{
    return f * f;
}