using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WingedEdge
{
    public class Vertex
    {
        public Vertex(int Index,
            Vector3 Position,
            WingedEdge Edge)
        {
            index = Index;
            position = Position;
            edge = Edge;
        }

        public int index { get; }
        public Vector3 position { get; set; }
        public WingedEdge edge { get; set; }
            
        public override int GetHashCode()
        {
            return index.GetHashCode();
        }
            
        public override bool Equals(object obj)
        {
            if (obj is Vertex otherVertex)
            {
                return index == otherVertex.index;
            }
            return false;
        }
        
        public bool IsBorderVertex => IncidentEdges.Any(incidentEdge => incidentEdge.IsBorderEdge);

        public int Valence => IncidentEdges.Count();
        
        public IEnumerable<WingedEdge> IncidentEdges
        {
            get
            {
                var start= edge;
                var current= start;
                
                do
                {
                    yield return current;
                    
                    current = (current.startVertex == this)
                        ? current.startCWEdge
                        : current.endCWEdge;
                }
                while (current != null && current != start);
            }
        }
    }
}