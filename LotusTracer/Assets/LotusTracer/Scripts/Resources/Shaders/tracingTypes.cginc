
struct TextureData
{
    int index;
    int atlasIndex;
    int x;
    int y;
    int width;
    int height;
};

struct RenderRay
{
    int pixelIndex;
    float3 origin;
    float3 direction;
};

struct RenderTriangleVertices
{
    float3 posA;
    float3 p0p1;
    float3 p0p2;
    int flags;
};

struct RenderTriangleData
{
    float3 normalA;
    float3 normalB;
    float3 normalC;
    int materialIndex;
    float2 textureUV0;
    float2 textureUV1;
    float2 textureUV2;
    float3 tangentA;
    float3 tangentB;
    float3 tangentC;
    float3 vertexColor;
};

struct RenderLight
{
    float4 color;
    float3 position;
    float3 forward;
    float range;
    float intensity;
    float angle;
    int type;
    int castShadows;
    int receiveHits;
    float radius;
    float area;
};

struct RenderMaterial
{
    float emissiveIntensity;
    float4 color;
    float transmissionPower;
    float mediumDensity;
    float scatteringDirection;
    float roughness;
    float clearCoat;
    float clearCoatRoughness;
    float metallic;
    float anisotropic;
    float normalStrength;
        
    int albedoMapIndex;
    int albedoMapCanvasIndex;
    int normalMapIndex;
    int normalMapCanvasIndex;
    int roughMapIndex;
    int roughMapCanvasIndex;
    int metalMapIndex;
    int metalMapCanvasIndex;
    int emissionMapIndex;
    int emissionMapCanvasIndex;
    float ior;
    int flags;
};

struct BoundsBox
{
    float3 min;
    float3 max;
};

struct BVH4Node
{
    // bit 1: is Leaf?
    uint data;
    int startIndex;
    int qtyTriangles;
    BoundsBox bounds;
};

struct TriangleHitInfo
{
    bool isTriangle; // either triangle or sdf (for point lights, etc)
    bool isFrontFace;
    float distance;
    float3 normal;
    float3 tangent;
    float3 biTangent;
    float3 position;
    float3 vertexColor;
        
    // inverse direction of the ray that made this hit
    float3 backRayDirection;

    uint triangleIndex;
    int materialIndex;
    float2 textureUV;
};

struct SampleProbabilities
{
    float wDielectric;
    float wMetal;
    float wGlass;

    float prDiffuse;
    float prDielectric;
    float prMetallic;
    float prGlass;
    float prClearCoat;

    float prRange_Diffuse;
    float prRange_Dielectric;
    float prRange_Metallic;
    float prRange_Glass;
    float prRange_ClearCoat;
};

struct EvaluationVars
{
    float NoL;
    float NoV;
    float NoH;
    float VoH;
    float VoL;
    float FV; // fresnel of V
    float FL; // fresnel of L
    float squareR;
};

struct ScatteringData
{
    float3 surfacePoint;
    float3 WorldNormal;
    float3 WorldTangent;
    float3 WorldBiTangent;
    
    float3 V;
    float3 L;
    float3 H;

    float emissionPower;
    float3 color;

    float clearCoat;
    float clearCoatRoughness;
    float roughness;
    float metallic;
    float transmissionPower;
    float mediumDensity;
    float scatteringDirection;

    float3 cSpec0;
    float ax;
    float ay;
    float eta;
    float ior1;
    float ior2;

    uint flags;

    bool isReflection; // reflection or refraction?
    bool isThin;

    int sampledType;
    
    SampleProbabilities probs;
};


struct PathVertexSample
{
    uint2 pixelCoords;
    float3 surfacePoint;
    float3 wi;
    float3 throughput;
};

struct WavefrontVertexHit
{
    TriangleHitInfo hitInfo;
    int sampleIndex;
};


struct WavefrontNEEDirectLight
{
    int hitInfoIndex;
    float distance;
    int lightIndex;
};