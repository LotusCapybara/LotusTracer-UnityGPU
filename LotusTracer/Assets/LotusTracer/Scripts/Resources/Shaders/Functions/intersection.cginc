


uint Unpart1by2(uint n)
{
    n &= 0x09249249;
    n = (n ^ (n >> 2)) & 0x030c30c3;
    n = (n ^ (n >> 4)) & 0x0300f00f;
    n = (n ^ (n >> 8)) & 0xff0000ff;
    n = (n ^ (n >> 16)) & 0x000003ff;
    return n;
}

uint3 MortonDecode3(in uint n)
{
    uint3 v;
        
    v.x = Unpart1by2(n);
    v.y = Unpart1by2(n >> 1);
    v.z = Unpart1by2(n >> 2);

    return v;
}

static float gridScale = 1.0 / 1023.0;

BoundsBox DecompressBounds(in uint2 morton, in BVH4Node node)
{
    uint bitMask = 0x3FF;

    uint3 qMin = uint3(             morton.x & bitMask, (morton.x >> 10) & bitMask, (morton.x >> 20) & bitMask);
            
    uint3 qMax = uint3(       morton.y & bitMask, (morton.y >> 10) & bitMask, (morton.y >> 20) & bitMask);

    BoundsBox outBounds;   
    
    outBounds.min = ((float3) qMin * gridScale ) * node.extends + node.boundsMin;
    outBounds.max = ((float3) qMax * gridScale ) * node.extends + node.boundsMin;

    if(node.precisionLoss > 0.005)
    {
        outBounds.min -= (float3) node.precisionLoss;
        outBounds.max += (float3) node.precisionLoss;
    }
    
    return outBounds;
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

bool DoesRayHitBounds(in RenderRay r, in BoundsBox b, out float dist, in float3 invDirection)
{
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

int GetTriangleHitIndex(in RenderRay ray, float maxDistance, bool isFirstBounce, out bool isTriangle)
{
    float3 invDirection = 1.0 / ray.direction;
    
    uint shortStack[BVH_STACK_SIZE];
    int stackIndex = 0;
    shortStack[stackIndex] = 0;
    
    float closestDistance = maxDistance;
    int hittingTriangleIndex = -1;

    for(int l = 0; l < qtyDirectLights; l++)
    {
        if(_Lights[l].receiveHits)
        {
            float hitDist = RayToSphere(ray, _Lights[l].position, _Lights[l].radius);
            if(hitDist > 0)
            {
                hittingTriangleIndex = l;
                closestDistance = hitDist;
                isTriangle = false;
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
                    isTriangle = true;                    
                }
            }
        }
        else
        {
            uint2 qMinMax[4];        
            qMinMax[0] = node.bb0;
            qMinMax[1] = node.bb1;
            qMinMax[2] = node.bb2;
            qMinMax[3] = node.bb3;
            
            [unroll]
            for(int ch = 0; ch < 4; ch++)
            {
                bool isTrasversable =  ((node.data >> (ch + 1)) & 0x1)  == 1;

                [branch]
                if(isTrasversable)
                {
                    float entryDist;

                    BoundsBox childBounds = DecompressBounds(qMinMax[ch], node);
                    bool intersects = DoesRayHitBounds(ray, childBounds, entryDist, invDirection);

                    // bool intersects = DoesRayHitBounds(ray,  childrenBounds[ch], entryDist, invDirection);
                    if(intersects && entryDist < closestDistance)
                    {
                        shortStack[++stackIndex] = node.startIndex + ch;
                    }
                }
                
                
            }
        }       
    }

    return  hittingTriangleIndex;
}

bool GetBounceHit(inout TriangleHitInfo hitInfo, in RenderRay ray, float maxDistance, bool isFirstBounce)
{
    bool isTriangle;
    int hittingTriangleIndex = GetTriangleHitIndex(ray, maxDistance, isFirstBounce, isTriangle);
    
    [branch]
    if (isTriangle)
    {
        hitInfo.isTriangle = true;
        GetTriangleHitInfo(hittingTriangleIndex, ray, maxDistance, hitInfo);
        return true;        
    }
    
    if(hittingTriangleIndex >= 0)
    {
        hitInfo.isTriangle = false;
        hitInfo.isFrontFace = true;
        hitInfo.distance = maxDistance;
        hitInfo.normal = - ray.direction;
        hitInfo.backRayDirection = - ray.direction;
        hitInfo.position = _Lights[hittingTriangleIndex].position;
    
        hitInfo.triangleIndex = - 1;
        hitInfo.materialIndex = hittingTriangleIndex;
        hitInfo.textureUV = float2(0, 0);
    
        CreateCoordinateSystem(hitInfo.normal, hitInfo.tangent, hitInfo.biTangent);
        return true;
    }
    
    return false;
}
