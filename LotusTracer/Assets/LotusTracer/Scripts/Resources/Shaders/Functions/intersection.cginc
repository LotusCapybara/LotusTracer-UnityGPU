
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

float RayDistanceToBounds(in RenderRay ray, in BoundsBox bounds)
{
    float tmin = - INFINITY;
    float tmax = INFINITY;

    float t1;
    float t2;
            
    // x direction
    if (ray.direction.x != 0)
    {
        t1 = (bounds.min.x - ray.origin.x) / ray.direction.x;
        t2 = (bounds.max.x - ray.origin.x) / ray.direction.x;
        tmin = max(tmin, min(t1, t2));
        tmax = min(tmax, max(t1, t2));
    }
    else if (ray.origin.x < bounds.min.x || ray.origin.x > bounds.max.x)
        return -1;


    // y direction
    if (ray.direction.y != 0)
    {
        t1 = (bounds.min.y - ray.origin.y) / ray.direction.y;
        t2 = (bounds.max.y - ray.origin.y) / ray.direction.y;
        tmin = max(tmin, min(t1, t2));
        tmax = min(tmax, max(t1, t2));
    }
    else if (ray.origin.y < bounds.min.y || ray.origin.y > bounds.max.y)
        return -1;
            
    // z direction
    if (ray.direction.z != 0)
    {
        t1 = (bounds.min.z - ray.origin.z) / ray.direction.z;
        t2 = (bounds.max.z - ray.origin.z) / ray.direction.z;
        tmin = max(tmin, min(t1, t2));
        tmax = min(tmax, max(t1, t2));
    }
    else if (ray.origin.z < bounds.min.z || ray.origin.z > bounds.max.z)
        return -1;
            
    if (tmin > tmax)
        return -1;

    if (tmin < 0)
        return 0;

    return tmin;
}

bool DoesRayHitBounds(in RenderRay r, in BoundsBox b)
{
    float3 invDirection = 1.0 / r.direction;
    
    float tx1 = (b.min.x - r.origin.x) * invDirection.x;
    float tx2 = (b.max.x - r.origin.x) * invDirection.x;

    float tmin = min(tx1, tx2);
    float tmax = max(tx1, tx2);

    float ty1 = (b.min.y - r.origin.y) * invDirection.y;
    float ty2 = (b.max.y - r.origin.y) * invDirection.y;

    tmin = max(tmin, min(ty1, ty2));
    tmax = min(tmax, max(ty1, ty2));

    float tz1 = (b.min.z - r.origin.z) * invDirection.z;
    float tz2 = (b.max.z - r.origin.z) * invDirection.z;

    tmin = max(tmin, min(tz1, tz2));
    tmax = min(tmax, max(tz1, tz2));
    
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

bool GetBounceHit(inout TriangleHitInfo hitInfo, in RenderRay ray, float maxDistance, bool isFirstBounce)
{
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
            for(int ch = 0; ch < 4; ch++)
            {
                bool isTraversable =  ((node.data >> (ch + 1)) & 0x1)  == 1;                
                isTraversable = isTraversable && DoesRayHitBounds(ray, _AccelTree[node.startIndex + ch].bounds);                    
                
                if(isTraversable)
                    shortStack[++stackIndex] = node.startIndex + ch;
            }
        }       
    }    

    [branch]
    if (hittingTriangleIndex >= 0)
    {
        hitInfo.isTriangle = true;
        GetTriangleHitInfo(hittingTriangleIndex, ray, maxDistance, hitInfo);
        return true;        
    }

    if(hittingLightIndex >= 0)
    {
        hitInfo.isTriangle = false;
        hitInfo.isFrontFace = true;
        hitInfo.distance = closestDistance;
        hitInfo.normal = - ray.direction;
        hitInfo.backRayDirection = - ray.direction;
        hitInfo.position = _Lights[hittingLightIndex].position;

        hitInfo.triangleIndex = - 1;
        hitInfo.materialIndex = hittingLightIndex;
        hitInfo.textureUV = float2(0, 0);

        CreateCoordinateSystem(hitInfo.normal, hitInfo.tangent, hitInfo.biTangent);
        return true;
    }
    
    return false;
}
