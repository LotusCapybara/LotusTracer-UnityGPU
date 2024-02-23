
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
    float3 origin;
    float3 direction;
    float3 invDirection;
};

struct RenderTriangle
{
    float3 posA;
    float3 p0p1;
    float3 p0p2;
    float3 normalA;
    float3 normalB;
    float3 normalC;
    float3 centerPos;
    int materialIndex;
    float2 textureUV0;
    float2 textureUV1;
    float2 textureUV2;
    float3 tangentA;
    float3 tangentB;
    float3 tangentC;
    float3 biTangentA;
    float3 biTangentB;
    float3 biTangentC;
    int flags;
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
};

struct RenderMaterial
{
    float emissiveIntensity;
    float4 color;
    float transmissionPower;
    float mediumDensity;
    float scatteringDirection;
    float maxScatteringDistance;
    float roughness;
    float clearCoat;
    float clearCoatRoughness;
    float metallic;
        
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
    int isFrontFace;
    float distance;
    float3 normal;
    float3 tangent;
    float3 biTangent;
    float3 position;
        
    // inverse direction of the ray that made this hit
    float3 backRayDirection;

    uint triangleIndex;
    int materialIndex;
    float2 textureUV;
};

struct SampleData
{
    float3 L;
    float3 H;
    float3 sampleReflectance;
    float pdf;
    float refractF;
    float3 refractH;
};

struct SampleProbabilities
{
    float weightDielectric;
    float weightMetallic;
    float weightTransmission;    
    
    float prDiffuse;
    float prSpecular;    
    float prTransmission;
    float prClearCoat;

    float prRangeDiffuse;
    float prRangeSpecular;
    float prRangeClearCoat;
    float prRangeTransmission;    
};

struct ScatteringData
{
    float3 surfacePoint;
    float3 WorldNormal;
    float3 WorldTangent;
    float3 WorldBiTangent;
    
    float3 V;    

    float emissionPower;
    float3 color;

    float clearCoat;
    float clearCoatRoughness;
    float roughness;
    float metallic;
    float transmissionPower;
    float mediumDensity;
    float scatteringDirection;
    float maxScatteringDistance;

    float3 F0;
    float ax;
    float ay;
    float eta;

    bool isReflection; // reflection or refraction?

    SampleData sampleData;
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