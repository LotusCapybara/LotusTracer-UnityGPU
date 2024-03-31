


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

BoundsBox DecompressBounds(in uint2 qMinMax, in BVH4Node node)
{
    uint bitMask = 0x3FF;

    uint3 qMin = uint3( qMinMax.x & bitMask, (qMinMax.x >> 10) & bitMask, (qMinMax.x >> 20) & bitMask);
            
    uint3 qMax = uint3( qMinMax.y & bitMask, (qMinMax.y >> 10) & bitMask, (qMinMax.y >> 20) & bitMask);

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
    
    // I'll call to this uint2: node group, which is
    // the data of the node and its children
    // the x contains the index of the node in the _AccelTree array
    // while the y contains a bit mask of the children that need to be visited
    // for instance 0000 0011 means there are still 1 children to be visited
    // the tree is constructed in a way that each node contains the children as contiguous nodes
    // in the array of nodes.
    // This is based on multiple reads you can find online (papers, etc)
    uint2 shortStack[BVH_STACK_SIZE];
    for(int i = 0; i < BVH_STACK_SIZE; i++)
        shortStack[i] = (uint2) 0.0;
    int stackIndex = 0;
    uint startMask =  0xff;
    shortStack[stackIndex] = uint2 (0, startMask); // change the y mask based on the amount of children    

    while (stackIndex >= 0)
    {
        uint2 nodeGroup = shortStack[stackIndex--];

        // if there are still children to be visit in this node group
        if(nodeGroup.y & 0xff)
        {
            int chBit = firstbitlow(nodeGroup.y);

            nodeGroup.y &= ~ (1 << chBit);

            if( (nodeGroup.y & 0xff) != 0)
            {
                shortStack[++stackIndex] = nodeGroup;
            }            
            
            int chIndex = _AccelTree[nodeGroup.x].firstElementIndex + chBit;

            const BVH4Node childNode = _AccelTree[chIndex];

            int qtyElements = (childNode.data >> 9) & 0xf;             
            int childLeavesMask = (childNode.data >> 13) & 0xff;
            int leavesHitMask = 0;

            if(qtyElements > 0)
            {
                // uint2 qMinMax[8];
                // qMinMax[0] = childNode.bb01.xy;
                // qMinMax[1] = childNode.bb01.zw;
                // qMinMax[2] = childNode.bb23.xy;
                // qMinMax[3] = childNode.bb23.zw;
                // qMinMax[4] = childNode.bb45.xy;
                // qMinMax[5] = childNode.bb45.zw;
                // qMinMax[6] = childNode.bb67.xy;
                // qMinMax[7] = childNode.bb67.zw;

                BoundsBox bb[8];
                bb[0] =  DecompressBounds(childNode.bb01.xy, childNode);
                bb[1] =  DecompressBounds(childNode.bb01.zw, childNode);
                bb[2] =  DecompressBounds(childNode.bb23.xy, childNode);
                bb[3] =  DecompressBounds(childNode.bb23.zw, childNode);
                bb[4] =  DecompressBounds(childNode.bb45.xy, childNode);
                bb[5] =  DecompressBounds(childNode.bb45.zw, childNode);
                bb[6] =  DecompressBounds(childNode.bb67.xy, childNode);
                bb[7] =  DecompressBounds(childNode.bb67.zw, childNode);

                uint hitMask = 0;
                
                for(int ch = 0; ch < qtyElements; ch++)
                {
                    bool isTrasversable = ((childNode.data >> (ch + 1)) & 0x1)  == 1;
            
                    [branch]
                    if(isTrasversable)
                    {
                        float entryDist;
                        bool intersects = DoesRayHitBounds(ray, bb[ch], entryDist, invDirection);
                
                        if(intersects && entryDist < closestDistance)
                        {
                            bool isLeaf = ((childLeavesMask >> ch) & 0x1) == 1;

                            if(isLeaf)
                            {
                                leavesHitMask |= (1 << ch);
                            }
                            else
                            {                            
                                hitMask = hitMask | (1 << ch);                               
                            }
                        }
                    }
                }            

                // if the ray hit against any of the children of this child,
                // we should add to the stack this child as a new group 
                if(hitMask & 0xff)
                {
                    shortStack[++stackIndex] = uint2((uint) chIndex, hitMask);
                }       
            }

            for(int ch = 0; ch < qtyElements; ch++)
            {
                if( (leavesHitMask >> ch) & 1 )
                {
                    int childIndex = childNode.firstElementIndex + ch;
                
                    BVH4Node leafNode = _AccelTree[childIndex];
                    int qtyTriangles = (leafNode.data >> 9) & 0xf;
                    for (int tIndex = leafNode.firstElementIndex; tIndex < (leafNode.firstElementIndex + qtyTriangles); tIndex++)
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
            }      
        }
    }

    return  hittingTriangleIndex;
}

int GetTriangleHitIndex2(in RenderRay ray, float maxDistance, bool isFirstBounce, out bool isTriangle)
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

        int qtyElements = (node.data >> 9) & 0xf;        
        
        int childLeavesMask = (node.data >> 13) & 0xff;
        int leavesHitMask = 0;
        
        [branch]
        if (qtyElements > 0)
        {
            uint2 qMinMax[8];        
            qMinMax[0] = node.bb01.xy;
            qMinMax[1] = node.bb01.zw;
            qMinMax[2] = node.bb23.xy;
            qMinMax[3] = node.bb23.zw;
            qMinMax[4] = node.bb45.xy;
            qMinMax[5] = node.bb45.zw;
            qMinMax[6] = node.bb67.xy;
            qMinMax[7] = node.bb67.zw;
            
            for(int ch = 0; ch < qtyElements; ch++)
            {
                float entryDist;

                BoundsBox childBounds = DecompressBounds(qMinMax[ch], node);
                bool intersects = DoesRayHitBounds(ray, childBounds, entryDist, invDirection);

                int childIndex = node.firstElementIndex + ch;

                if(intersects && entryDist < closestDistance)
                {
                    bool isLeaf = ((childLeavesMask >> ch) & 0x1) == 1;
                        
                    if(!isLeaf)
                    {
                        shortStack[++stackIndex] =  childIndex;
                    }
                    else
                    {
                        leavesHitMask |= (1 << ch);
                    }
                }
            }

            for(int ch = 0; ch < qtyElements; ch++)
            {
                if( (leavesHitMask >> ch) & 1 )
                {
                    int childIndex = node.firstElementIndex + ch;
                
                    BVH4Node leafNode = _AccelTree[childIndex];
                    int qtyTriangles = (leafNode.data >> 9) & 0xf;
                    for (int tIndex = leafNode.firstElementIndex; tIndex < (leafNode.firstElementIndex + qtyTriangles); tIndex++)
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
