using System.Collections.Generic;

namespace WingedEdge
{
    public class Face
    {
        public Face(int Index,
            WingedEdge Edge)
        {
            index = Index;
            edge = Edge;
        }

        public int index { get; set; }
        public WingedEdge edge { get; set; }
        
        public IEnumerable<WingedEdge> FaceEdges
        {
            get 
            {
                var start = this.edge;
                var current = start;

                do
                {
                    yield return current;

                    current = (current.rightFace == this)
                        ? current.endCCWEdge
                        : current.startCCWEdge;
                } while (current != null && current != start);
            }
        }
        
        public IEnumerable<Vertex> FaceVertices
        {
            get 
            {
                var start = this.edge;
                var current = start;

                do
                {
                    yield return (current.rightFace == this)
                        ? current.startVertex
                        : current.endVertex;

                    current = (current.rightFace == this)
                        ? current.endCCWEdge
                        : current.startCCWEdge;
                } while (current != null && current != start);
            }
        }
    }
}