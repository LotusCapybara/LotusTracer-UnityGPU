


// Compact bits (opposite of Part1By2)
uint Compact1By2(uint mortonHigh, uint mortonLow, uint shift)
{
    uint x = shift == 2 ? mortonHigh : (shift == 1 ? ((mortonHigh << 1) | (mortonLow >> 31)) : mortonLow);
    
    x &= 0x09249249; // Mask: binary 1001001001001001001001001
    x = (x ^ (x >> 2)) & 0x030C30C3; // binary: 11000011000011000011
    x = (x ^ (x >> 4)) & 0x0300F00F; // binary: 11000000111100000011
    x = (x ^ (x >> 8)) & 0x030000FF; // binary: 11000000000000000011111111
    x = (x ^ (x >> 16)) & 0x000FFFFF; // binary: 11111111111111111111
    
    return x;
}

uint3 DecodeMorton3(uint2 morton)
{
    uint3 r = uint3(0, 0, 0);
    
    r.x = Compact1By2(morton.y, morton.x, 2); // Shift right twice, compact bits for x
    r.y = Compact1By2(morton.y, morton.x, 1); // Shift right once, compact bits for y
    r.z = Compact1By2(morton.y, morton.x, 0); // No shift needed, compact bits for z
    
    return r;
}



    

void Decompress(inout BoundsBox outBounds[8], in BVH4Node node)
{
    uint2 mortonMins[8];
    uint2 mortonMaxs[8];
        
    mortonMins[0] = uint2( node.bb0.x , node.bb0.y);
    mortonMaxs[0] = uint2( node.bb0.z , node.bb0.w);

    mortonMins[1] = uint2( node.bb1.x , node.bb1.y);
    mortonMaxs[1] = uint2( node.bb1.z , node.bb1.w);

    mortonMins[2] = uint2( node.bb2.x , node.bb2.y);
    mortonMaxs[2] = uint2( node.bb2.z , node.bb2.w);

    mortonMins[3] = uint2( node.bb3.x , node.bb3.y);
    mortonMaxs[3] = uint2( node.bb3.z , node.bb3.w);

    mortonMins[4] = uint2( node.bb4.x , node.bb4.y);
    mortonMaxs[4] = uint2( node.bb4.z , node.bb4.w);

    mortonMins[5] = uint2( node.bb5.x , node.bb5.y);
    mortonMaxs[5] = uint2( node.bb5.z , node.bb5.w);

    mortonMins[6] = uint2( node.bb6.x , node.bb6.y);
    mortonMaxs[6] = uint2( node.bb6.z , node.bb6.w);

    mortonMins[7] = uint2( node.bb7.x , node.bb7.y);
    mortonMaxs[7] = uint2( node.bb7.z , node.bb7.w);
    
    
    for (int i = 0; i < 8; i++)
    {
        uint3 qMin = DecodeMorton3(mortonMins[i]);
        uint3 qMax = DecodeMorton3(mortonMaxs[i]);

        outBounds[i].min = ((float3) qMin / 1023.0 ) * node.extends + node.boundsMin;
        outBounds[i].max = ((float3) qMax / 1023.0 ) * node.extends + node.boundsMin;
    }
}

float RayToSphere(in RenderRay ray, in float3 center, float radius)
{
    // solutions for t if the ray intersects
    float t0, t1; 

    float radius2 = radius * radius;
    
    // geometric solution
    float3 L = center - ray.origin;
    float tca = dot(L, ray.direction);
    float d2 = dot(L, L) - tca * tca;    

    if (d2 > radius2)
        return -1.0;
    
    float thc = sqrt(radius2 - d2);
    t0 = tca - thc;
    t1 = tca + thc;
    
    if (t0 > t1)
    {
        float temp = t1;
        t1 = t0;
        t0 = temp;
    }

    if (t0 < 0)
    {
        t0 = t1; // if t0 is negative, let's use t1 instead
        if (t0 < 0)
            return -1; // both t0 and t1 are negative
    }
    
    return t0; 
}

bool DoesRayHitBounds(in RenderRay r, in BoundsBox b, out float dist)
{
    float3 invDirection = 1.0 / r.direction;

    float3 t1 = (b.min - r.origin) * invDirection;
    float3 t2 = (b.max - r.origin) * invDirection;
    
    float3 tmin3 = min(t1, t2);
    float3 tmax3 = max(t1, t2);
    
    float tmin = max(max(tmin3.x, tmin3.y), tmin3.z);
    float tmax = min(min(tmax3.x, tmax3.y), tmax3.z);

    dist = tmin;
    
    return tmax >= tmin;
}


float FastIntersectRayWithTriangle(int triIndex, in RenderRay ray, float maxDistance, bool isFirstBounce)
{
    RenderTriangleVertices tri = _TriangleVertices[triIndex];

    if(isFirstBounce && (tri.flags & 0x1) == 1)
       return 0;
    
    float3 pVec = cross(ray.direction, tri.p0p2);
    float det = dot(tri.p0p1, pVec);
        
    if (abs(det) < 1E-8)
        return 0;
        
    float invDet = 1.0 / det;

    float3 tVec = ray.origin - tri.posA;
    float u = dot(tVec, pVec) * invDet;
        
    if (u < 0 || u > 1)
        return 0;
        
    float3 qVec = cross(tVec, tri.p0p1);
        
    float v = dot(ray.direction, qVec) * invDet;
        
    if (v < 0 || (u + v) > 1)
        return 0;
        
    float distance = dot(tri.p0p2, qVec) * invDet;
        
    if (distance <= 0 || distance > maxDistance)
        return 0;
        
    return distance;
}


bool GetTriangleHitInfo(int triIndex, in RenderRay ray, float maxDistance, inout TriangleHitInfo hitInfo)
{
    RenderTriangleVertices triV = _TriangleVertices[triIndex];
    RenderTriangleData tri = _TriangleDatas[triIndex];
            
    float3 pVec = cross(ray.direction, triV.p0p2);
    const float det = dot(triV.p0p1, pVec);
        
    if (abs(det) < 1E-8)
        return false;
        
    float invDet = 1.0 / det;

    float3 tVec = ray.origin - triV.posA;
    float u = dot(tVec, pVec) * invDet;
        
    if (u < 0 || u > 1)
        return false;
        
    float3 qVec = cross(tVec, triV.p0p1);
        
    float v = dot(ray.direction, qVec) * invDet;
        
    if (v < 0 || (u + v) > 1)
        return false;
        
    float distance = dot(triV.p0p2, qVec) * invDet;
        
    if (distance <= 0 || distance > maxDistance)
        return false;

    hitInfo.isFrontFace = det > 0 ? 1 : 0;
    hitInfo.backRayDirection = -ray.direction;
    hitInfo.distance = distance;
    hitInfo.position = ray.origin + ray.direction * distance;
    hitInfo.materialIndex = tri.materialIndex;
    
    hitInfo.textureUV = tri.textureUV0 * ( 1 - u - v) + tri.textureUV1 * u + tri.textureUV2 * v;
    hitInfo.triangleIndex = triIndex;

    hitInfo.normal = normalize(  tri.normalA * ( 1 - u - v) + tri.normalB * u + tri.normalC * v );
    hitInfo.tangent = normalize( tri.tangentA * ( 1 - u - v) + tri.tangentB * u + tri.tangentC * v );

    hitInfo.vertexColor = tri.vertexColor;
    
    float crossSign = -1;
    float3 biTangentA = normalize(crossSign  * cross(tri.normalA, tri.tangentA));
    float3 biTangentB = normalize(crossSign  * cross(tri.normalB, tri.tangentB));
    float3 biTangentC = normalize(crossSign  * cross(tri.normalC, tri.tangentC));
    
    hitInfo.biTangent = normalize( biTangentA * ( 1 - u - v) + biTangentB * u + biTangentC * v );

    int normalMapIndex = _Materials[hitInfo.materialIndex].normalMapIndex;
    if(normalMapIndex >= 0)
    {
        // Sample the normal map
        TextureData texture_data = _MapDatasNormal[normalMapIndex];
        float2 targetUV = PackedUV(texture_data, hitInfo.textureUV);        
        int atlasIndex = texture_data.atlasIndex;

        float3 packednormal = _AtlasesNormal
            .SampleLevel(sampler_AtlasesNormal, float3(targetUV.x, targetUV.y, atlasIndex) , 0).rgb;

        if( ((_Materials[hitInfo.materialIndex].flags >> 12) & 0x1) == 1 )
            packednormal.g = 1.0 - packednormal.g;
        
        // remapping from 0to1 to -1 to 1. This is how normal maps usually work
        packednormal.xyz = packednormal.xyz * 2.0 - 1.0;
        

        // coordinates transformation (textures have z up, and they are right handed)
        packednormal.xyz = float3(packednormal.y, - packednormal.z, packednormal.x);
        packednormal.xz *= _Materials[hitInfo.materialIndex].normalStrength;
        
        // tangent space of map to world space of triangle hit
        float3x3 tbn = float3x3(hitInfo.tangent, hitInfo.biTangent, hitInfo.normal);
        float3 perturbedNormal = mul(packednormal, tbn);

        float viewAngle = max(dot(hitInfo.backRayDirection, perturbedNormal), 0);
        float blendFactor = pow(1.0 - viewAngle, 2);

        hitInfo.normal = normalize(lerp(perturbedNormal, hitInfo.normal, blendFactor));             
    }
    else
    {
        CreateCoordinateSystem(hitInfo.normal, hitInfo.tangent, hitInfo.biTangent);
    }

    hitInfo.normal = normalize(hitInfo.normal);
    
    return true;
}

int GetTriangleHitIndex(in RenderRay ray, float maxDistance, bool isFirstBounce)
{
    float3 invDirection = 1.0 / ray.direction;
    
    uint shortStack[BVH_STACK_SIZE];
    int stackIndex = 0;
    shortStack[stackIndex] = 0;
    
    float closestDistance = maxDistance;
    int hittingTriangleIndex = -1;
    int hittingLightIndex = -1;

    for(int l = 0; l < qtyDirectLights; l++)
    {
        if(_Lights[l].receiveHits)
        {
            float hitDist = RayToSphere(ray, _Lights[l].position, _Lights[l].radius);
            if(hitDist > 0)
            {
                hittingLightIndex = l;
                closestDistance = hitDist;
            }    
        }
    }
    
    while (stackIndex >= 0 && stackIndex < BVH_STACK_SIZE)
    {
        int ptr = shortStack[stackIndex--];
    
        BVH4Node node = _AccelTree[ptr];
        
        bool isLeaf = (node.data & 0x1) == 1;

        [branch]
        if (isLeaf)
        {
            for (int tIndex = node.startIndex; tIndex < (node.startIndex + node.qtyTriangles); tIndex++)
            {
                float hitDistance = FastIntersectRayWithTriangle( tIndex, ray, closestDistance, isFirstBounce);
                
                if (hitDistance > 0 &&  hitDistance < closestDistance)
                {
                    closestDistance = hitDistance;
                    hittingTriangleIndex = tIndex;
                }
            }
        }
        else
        {
            BoundsBox childrenBounds[8];

            Decompress(childrenBounds, node);
            
            for(int ch = 0; ch < 8; ch++)
            {
                bool isTrasversable =  ((node.data >> (ch + 1)) & 0x1)  == 1;

                float entryDist;
                
                isTrasversable =DoesRayHitBounds(ray, childrenBounds[ch], entryDist);
                
                if(isTrasversable)
                {
                    shortStack[++stackIndex] = node.startIndex + ch;
                }
            }
        }       
    }

    return  hittingTriangleIndex;
}

bool GetBounceHit(inout TriangleHitInfo hitInfo, in RenderRay ray, float maxDistance, bool isFirstBounce)
{
    int hittingTriangleIndex = GetTriangleHitIndex(ray, maxDistance, isFirstBounce);

    [branch]
    if (hittingTriangleIndex >= 0)
    {
        hitInfo.isTriangle = true;
        GetTriangleHitInfo(hittingTriangleIndex, ray, maxDistance, hitInfo);
        return true;        
    }

    // if(hittingLightIndex >= 0)
    // {
    //     hitInfo.isTriangle = false;
    //     hitInfo.isFrontFace = true;
    //     hitInfo.distance = closestDistance;
    //     hitInfo.normal = - ray.direction;
    //     hitInfo.backRayDirection = - ray.direction;
    //     hitInfo.position = _Lights[hittingLightIndex].position;
    //
    //     hitInfo.triangleIndex = - 1;
    //     hitInfo.materialIndex = hittingLightIndex;
    //     hitInfo.textureUV = float2(0, 0);
    //
    //     CreateCoordinateSystem(hitInfo.normal, hitInfo.tangent, hitInfo.biTangent);
    //     return true;
    // }
    
    return false;
}
