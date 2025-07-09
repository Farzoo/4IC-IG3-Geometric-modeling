using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshGeneration.WingedEdge
{
    public class WingedEdge
    {
        public int index { get; set; }
        public Vertex startVertex { get; set; }
        public Vertex endVertex { get; set; }
        public Face rightFace { get; set; }
        public Face leftFace { get; set; }
        public WingedEdge startCCWEdge { get; set; }
        public WingedEdge startCWEdge { get; set; }
        public WingedEdge endCCWEdge { get; set; }
        public WingedEdge endCWEdge { get; set; }
        
        public WingedEdge(int Index,
        
            Vertex StartVertex,
            Vertex EndVertex,
            
            Face RightFace,
            Face LeftFace,
        
            WingedEdge StartCcwEdge,
            WingedEdge StartCwEdge,
        
            WingedEdge EndCcwEdge,
            WingedEdge EndCwEdge)
        {
            index = Index;
            startVertex = StartVertex;
            endVertex = EndVertex;
            rightFace = RightFace;
            leftFace = LeftFace;
            startCCWEdge = StartCcwEdge;
            startCWEdge = StartCwEdge;
            endCCWEdge = EndCcwEdge;
            endCWEdge = EndCwEdge;
        }

        public bool IsBorderEdge => this.leftFace is null;
        
        public Vertex OppositeVertex(Vertex vertex)
        {
            if (vertex == startVertex)
            {
                return endVertex;
            }

            if (vertex == endVertex)
            {
                return startVertex;
            }

            throw new ArgumentException("The provided vertex is not part of this edge.");
        }
        
        public override string ToString()
        {
            return $"WingedEdge {{ index: {index}, " +
                   $"startVertex: {startVertex.index}, endVertex: {endVertex.index}, " +
                   $"rightFace: {(rightFace?.index.ToString() ?? "null")}, leftFace: {(leftFace?.index.ToString() ?? "null")}, " +
                   $"startCCWEdge: {(startCCWEdge?.index.ToString() ?? "null")}, startCWEdge: {(startCWEdge?.index.ToString() ?? "null")}, " +
                   $"endCCWEdge: {(endCCWEdge?.index.ToString() ?? "null")}, endCWEdge: {(endCWEdge?.index.ToString() ?? "null")} }}";
        }
    }
}
