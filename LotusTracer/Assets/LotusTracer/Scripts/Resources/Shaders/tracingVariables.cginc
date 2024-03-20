


// global shader data
int _depthDiffuse = 3;
int _depthSpecular = 3;
int _depthTransmission = 12;
bool _isCameraMoving;
float width;
float height;
float cameraFOV;
float aspectRatio;
float4 cameraPos;
float4 cameraForward;
float4 cameraUp;
float4 cameraRight;
float totalSize;
int iteration;
float indirectBoost;
int currentBounce;
uint someSeed;
float4 ambientLightColor;
float ambientLightPower;
int _HasCubeMap;
int _IgnoreCubeInImage;
SamplerState sampler_CubeMap;
TextureCube<float4> _CubeMap;
// ------------------
StructuredBuffer<BoundsBox> _SceneBounds;

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

// these buffers contain Scene information such as geometry, BVH, Lights, etc

uint totalTriangles;
StructuredBuffer<RenderTriangleVertices> _TriangleVertices;
StructuredBuffer<RenderTriangleData> _TriangleDatas;

uint qtyDirectLights;
StructuredBuffer<RenderLight> _Lights;

uint totalMaterials;
StructuredBuffer<RenderMaterial> _Materials;

uint treeNodesQty;
StructuredBuffer<BVH4Node> _AccelTree;
// end of Scene buffers


// end of kernel buffers





