using System.Collections.Generic;

namespace CapyTracerCore.Core
{

    public class BVHNode
    {
        public static readonly int QTY_SPLITS = 3;
        
        public BoundsBox bounds;
        public List<BVHNode> children;
        public int depth;
        public int firstChildIndex;
        public bool expanded = false;
        public bool isLeaf;
        public List<int> triangleIndices;

        public BVHNode(BoundsBox bounds, int trianglesCount)
        {
            this.bounds = bounds;
            children = null;
            depth = 0;
            triangleIndices = new List<int>();
            
            for(int i = 0; i < trianglesCount; i++)
                triangleIndices.Add(i);
            
            isLeaf = true;
        }

        public void FinishGeneration(RenderTriangle[] allTriangles)
        {
            bounds = BoundsBox.AS_SHRINK;
            
            if (isLeaf)
            {
                expanded = triangleIndices.Count > 0;
                
                foreach(int tIndex in triangleIndices)
                {
                    bounds.ExpandWithPoint(allTriangles[tIndex].posA);
                    bounds.ExpandWithPoint(allTriangles[tIndex].posA + allTriangles[tIndex].p0p1);
                    bounds.ExpandWithPoint(allTriangles[tIndex].posA + allTriangles[tIndex].p0p2);
                }
            }
            else
            {
                expanded = true;
                
                for(int i = 0; i < children.Count; i++)
                    children[i].FinishGeneration(allTriangles);
        
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].expanded)
                    {
                        bounds.ExpandWithBounds(children[i].bounds);    
                    }
                }

                if (float.IsInfinity(bounds.min.x))
                {
                    isLeaf = true;
                    expanded = false;
                }
                    
            }
        }
        
        public void GetAllNodesSorted(List<BVHNode> nodes, bool addSelf)
        {
            if(addSelf)
                nodes.Add(this);
        
            if (!isLeaf)
            {
                firstChildIndex = nodes.Count;
                
                for (int i = 0; i < children.Count; i++)
                {
                    nodes.Add(children[i]);
                }
                
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].GetAllNodesSorted(nodes, false);
                }
            }
        }
    }
}