using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MeshGeneration.WingedEdge
{
    public class Face
    {
        public Face(int Index,
            WingedEdge Edge,
            Color color = default)
        {
            index = Index;
            edge = Edge;
        }

        public int index { get; set; }
        public WingedEdge edge { get; set; }
        
        
        // Renvoie les edges dans le sens horaire autour de la face
        public IEnumerable<WingedEdge> FaceEdges
        {
            get 
            {
                var start = this.edge;
                var current = start;
                
                const int maxEdges = 1000;
                int count = 0;

                do
                {
                    if (count >= maxEdges)
                    {
                        Debug.LogWarning($"Infinite loop detected in FaceEdges enumeration for face edge {start}.");
                        yield break;
                    }
                    
                    yield return current;
                    count++;
                    
                    current = NextEdgeAroundFace(current); 
                } while (current != null && current != start);
            }
        }
        
        public IEnumerable<Vertex> FaceVertices
        {
            get 
            {
                var start = this.edge;
                var current = start;
                const int maxVertices = 1000;
                int count = 0;
                do
                {
                    yield return NextVertexAroundFace(current);
                    
                    count++;
                    if (count >= maxVertices)
                    {
                        Debug.LogWarning($"Infinite loop detected in FaceVertices enumeration for face edge {start}.");
                        yield break;
                    }

                    current = NextEdgeAroundFace(current);
                } while (current != null && current != start);
            }
        }
        
        public bool IsEdgeOpposite(WingedEdge e)
        {
            return e.leftFace == this;
        }
        
        public Vector3 Centroid
        {
            get
            {
                Vector3 centroid = Vector3.zero;

                var faceVertices = FaceVertices.ToList();

                foreach (var v in faceVertices)
                {
                    centroid += v.position;
                }
                return centroid / faceVertices.Count;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WingedEdge NextEdgeAroundFace(WingedEdge e)
            => (e.rightFace == this) ? e.endCCWEdge : e.startCCWEdge;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vertex NextVertexAroundFace(WingedEdge e)
        {
            return (e.rightFace == this) ? e.startVertex : e.endVertex;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNextEdgeAroundFace(WingedEdge e, WingedEdge nextEdge)
        {
            if (e.rightFace == this)
            {
                e.endCCWEdge = nextEdge;
            }
            else
            {
                e.startCCWEdge = nextEdge;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPreviousEdgeAroundFace(WingedEdge e, WingedEdge previousEdge)
        {
            if (e.rightFace == this)
            {
                e.startCWEdge = previousEdge;
            }
            else
            {
                e.endCWEdge = previousEdge;
            }
        }
    }
}