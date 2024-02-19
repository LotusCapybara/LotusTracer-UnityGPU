
// -------------- RANDOM ---------------------------
// uint NextRandom(inout uint state)
// {
//     state = state * 747796405 + 2891336453;
//     uint result = ((state >> (int) ((state >> 28) + 4)) ^ state) * 277803737;
//     result = (result >> 22) ^ result;
//     return result;
// }

uint Hash(inout uint s)
{
    s ^= 2747636419u;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    return s;
}


float GetRandom0to1(inout uint state)
{   
    return Hash(state) / 4294967295.0; 
}

float GetRandomMin1to1(inout uint state)
{   
    return (Hash(state) / 4294967295.0) * 2.0 - 1.0; 
}

