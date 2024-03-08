


// used to calculate the proportion/weight of each importance sample method 
float PowerHeuristic(float a, float b)
{
    float t = a * a;
    return t / (b * b + t);
}


RenderRay GetCameraRay(uint3 id, inout uint randState)
{
    RenderRay pathRay ;
    pathRay.origin = cameraPos.xyz;

    float aspectRatio = (float) width / height;
    float u = (2.0 * id.x / width - 1.0) * aspectRatio;
    float v = 1.0 - 2.0 * id.y / height;

    // Calculate the tangent of the half field of view
    float tanHalfFov = tan(cameraFOV * PI / 180.0 * 0.5);
    float3 direction = cameraForward + cameraRight * u * tanHalfFov - cameraUp * v * tanHalfFov;
    direction = normalize(direction);


    pathRay.direction = direction;
    pathRay.direction.x += GetRandomMin1to1(randState) * 0.0001;
    pathRay.direction.y += GetRandomMin1to1(randState) * 0.0001;
    pathRay.direction.z += GetRandomMin1to1(randState) * 0.0001;
    pathRay.direction = normalize(pathRay.direction);

    return pathRay;
}

float2 PackedUV(in TextureData texture_data, in float2 uv)
{
    float wScale = (float)texture_data.width / 4096.0;
    float hScale = texture_data.height / 4096.0;

    float startU = texture_data.x / 4096.0;
    float startV = texture_data.y / 4096.0;

    return float2(startU + uv.x * wScale, startV + uv.y * hScale);
}



void ScatteringToWorld(inout ScatteringData data)
{
    data.V = ToWorld(data.WorldTangent, data.WorldBiTangent, data.WorldNormal, data.V);
    data.L = ToWorld(data.WorldTangent, data.WorldBiTangent, data.WorldNormal, data.L);
    data.H = ToWorld(data.WorldTangent, data.WorldBiTangent, data.WorldNormal, data.H);
}

void ScatteringToLocal(inout ScatteringData data)
{
    data.V = ToLocal(data.WorldTangent, data.WorldBiTangent, data.WorldNormal, data.V);
    data.L = ToLocal(data.WorldTangent, data.WorldBiTangent, data.WorldNormal, data.L);
    data.H = ToLocal(data.WorldTangent, data.WorldBiTangent, data.WorldNormal, data.H);
}