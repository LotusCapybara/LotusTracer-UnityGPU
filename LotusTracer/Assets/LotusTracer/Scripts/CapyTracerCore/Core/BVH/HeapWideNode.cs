using System.Collections.Generic;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    
    public class HeapWideNode
    {
        public int depth;
        public bool isLeaf;
        public BoundsBox bounds;
        public List<HeapWideNode> children= new List<HeapWideNode>();
        public List<int> geoBoxes = new List<int>();
        public int indexFirstChild;

        public static void SortWideNodes(List<HeapWideNode> allWideNodes, HeapWideNode nextNode, List<GeoBox> allBoxes)
        {
            if (nextNode.children == null ||  nextNode.children.Count <= 0)
            {
                nextNode.indexFirstChild = -1;
                nextNode.isLeaf = true;
            }
            else
            {
                nextNode.indexFirstChild = allWideNodes.Count;
                allWideNodes.AddRange(nextNode.children);
            
                foreach (var nextNodeChild in nextNode.children)
                {
                    SortWideNodes(allWideNodes, nextNodeChild, allBoxes);
                }
            }
        }
    }
}