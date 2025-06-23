using System.Collections.Generic;
using UnityEngine;

namespace TP1
{
    public class WingedEdgeMesh
    {
        public List<WingedEdge.Vertex> Vertices { get; }
        public List<WingedEdge> Edges { get; }
        public List<WingedEdge.Face> Faces { get; }

        public WingedEdgeMesh(Mesh mesh)
        {
            if (mesh.GetTopology(0) != MeshTopology.Triangles)
            {
                throw new System.ArgumentException("Mesh must be of type Triangles.");
            }
            
            if (mesh.subMeshCount > 1)
            {
                Debug.LogWarning("Mesh contains multiple submeshes, only the first one will be processed.");
            }
            
            // On espère que la topologie est correcte 🙏
            this.Vertices = new List<WingedEdge.Vertex>(mesh.vertexCount);
            this.Edges = new List<WingedEdge>(mesh.vertexCount * 3);
            this.Faces = new List<WingedEdge.Face>(mesh.vertexCount / 3);
            
            var meshIndices = mesh.GetIndices(0);
            Vector3[] meshVertices = mesh.vertices;

            for (int vertexIndex = 0; vertexIndex < mesh.vertexCount; vertexIndex++)
            {
                var vertexPosition = meshVertices[vertexIndex];
                var vertex = new WingedEdge.Vertex(vertexIndex, vertexPosition, null);
                this.Vertices.Add(vertex);
            }
            
            Dictionary<(int vertex0, int vertex1), WingedEdge> edgeMap = new Dictionary<(int, int), WingedEdge>();
            
            WingedEdge GetOrCreateEdge(int index, WingedEdge.Vertex startVertex, WingedEdge.Vertex endVertex, WingedEdge.Face rightFace)
            {
                var key = (startVertex.index, endVertex.index);
                if (edgeMap.TryGetValue(key, out var edge)) return edge;
                
                edge = new WingedEdge(index, startVertex, endVertex, rightFace, null, null, null, null, null);
                edgeMap[key] = edge;
                return edge;
            }
            
            for (int index = 0; index < meshIndices.Length; index += 3)
            {
                var vertex0 = this.Vertices[meshIndices[index]];
                var vertex1 = this.Vertices[meshIndices[index + 1]];
                var vertex2 = this.Vertices[meshIndices[index + 2]];
                
                var face = new WingedEdge.Face(index / 3, null);
                this.Faces.Add(face);
                
                var edge0 = GetOrCreateEdge(index, vertex0, vertex1, face);
                var edge1 = GetOrCreateEdge(index + 1, vertex1, vertex2, face);
                var edge2 = GetOrCreateEdge(index + 2, vertex2, vertex0, face);
                
                this.Edges.Add(edge0);
                this.Edges.Add(edge1);
                this.Edges.Add(edge2);
                

                vertex0.edge = edge0;
                vertex1.edge = edge1;
                vertex2.edge = edge2;

                face.edge = edge0;
                edge0.rightFace = face;
                edge1.rightFace = face;
                edge2.rightFace = face;

                edge0.startCWEdge = edge2;
                edge0.endCCWEdge = edge1;
                
                edge1.startCWEdge = edge0;
                edge1.endCCWEdge = edge2;
                
                edge2.startCWEdge = edge1;
                edge2.endCCWEdge = edge0;
            }
        }
    }

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

            public int index { get; set ; }
            public Vector3 position { get; set; }
            public WingedEdge edge { get; set; }
        }

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
        }
    }
}
