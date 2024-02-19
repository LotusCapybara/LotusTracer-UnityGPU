


// global shader data
float width;
float height;
int maxBounces;
float totalSize;
int iteration;
float indirectBoost;
int currentBounce;
uint someSeed;
// ------------------

Texture2DArray<float4> _AtlasesAlbedo;
SamplerState sampler_AtlasesAlbedo;
StructuredBuffer<TextureData> _MapDatasAlbedo;

Texture2DArray<float4> _AtlasesNormal;
SamplerState sampler_AtlasesNormal;
StructuredBuffer<TextureData> _MapDatasNormal;


Texture2DArray<float4> _AtlasesRoughness;
SamplerState sampler_AtlasesRoughness;
StructuredBuffer<TextureData> _MapDatasRoughness;

Texture2DArray<float4> _AtlasesMetallic;
SamplerState sampler_AtlasesMetallic;
StructuredBuffer<TextureData> _MapDatasMetallic;

Texture2DArray<float4> _AtlasesEmission;
SamplerState sampler_AtlasesEmission;
StructuredBuffer<TextureData> _MapDatasEmission;

RWTexture2D<float4> _DebugTexture;

// sampling buffers using linear space
RWTexture2D<float4> _SamplingBufferPrev;
RWTexture2D<float4> _SamplingBuffer;

// ldr for rendering with the color space you expect to see on screen
RWTexture2D<float4> _LDRFinalBuffer;

// these buffers contain Scene information such as geometry, BVH, Lights, etc

StructuredBuffer<RenderRay> _CameraRays;

uint totalTriangles;
StructuredBuffer<RenderTriangle> _Triangles;

uint qtyDirectLights;
StructuredBuffer<RenderLight> _Lights;

uint totalMaterials;
StructuredBuffer<RenderMaterial> _Materials;

uint treeNodesQty;
StructuredBuffer<BVH4Node> _AccelTree;
// end of Scene buffers


// end of kernel buffers





