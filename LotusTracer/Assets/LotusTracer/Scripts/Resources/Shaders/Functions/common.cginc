#include "random.cginc"

// ---------------------------------------------------

inline float MaxComponent(in float3 v)
{
    return max(v.x, max(v.y, v.z));
}

float3 SLERP(float3 a, float3 b, float t)
{
    return a * ( 1.0 - t ) + b * t;
}

inline bool SameGlobalHemiSphere(float3 a, float3 b)
{
    return dot(a, b) > 0;
}

float3 SphericalToVector( float theta , float phi )
{
    float x = sin( theta ) * cos( phi );
    float y = sin( theta ) * sin( phi );
    float z = cos( theta );
    
    return float3(x, z, y);
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


float3 SphericalUniformSample(inout uint randState)
{
    float theta = acos(1.0 - 2.0 * GetRandom0to1(randState));
    float phi = 2.0 * PI * GetRandom0to1(randState);
    
    return SphericalToVector(theta, phi);
}


// ----------------------  world to local and that


void CreateCoordinateSystem(in float3 normal, inout float3 tangent, inout float3 biTangent)
{
    float3 up = abs(normal.y) < 0.9999999 ? float3(0, 1, 0) : float3(1, 0, 0);
    tangent = normalize(cross(up, normal));
    biTangent = normalize( cross(normal, tangent) * -1);
}

float3 ToWorld(float3 T, float3 B, float3 N, float3 v)
{
    return normalize( v.x * T + v.z * B + v.y * N);
}

float3 ToLocal(float3 T, float3 B, float3 N, float3 v)
{
    return normalize( float3(dot(v, T), dot(v, N), dot(v, B)));
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