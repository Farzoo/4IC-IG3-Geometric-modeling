using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshGeneration.WingedEdge
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
                const int maxEdges = 1000;
                int count = 0;
                
                if (edge.startVertex != this && edge.endVertex != this)
                {
                    Debug.LogWarning($"Edge {edge} does not connect to vertex {this.index}.");
                }
                
                do
                {
                    if (count >= maxEdges)
                    {
                        Debug.LogWarning($"Infinite loop detected in IncidentEdges enumeration for {this.index} vertex.");
                        yield break;
                    }
                    
                    yield return current;
                    count++;
                    
                    current = (current.startVertex == this)
                        ? current.startCWEdge
                        : current.endCWEdge;
                }
                while (current != null && current != start);
            }
        }
    }
}