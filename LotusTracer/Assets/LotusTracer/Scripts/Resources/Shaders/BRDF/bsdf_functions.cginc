


// used to calculate the proportion/weight of each importance sample method 
float PowerHeuristic(float a, float b)
{
    float t = a * a;
    return t / (b * b + t);
}
