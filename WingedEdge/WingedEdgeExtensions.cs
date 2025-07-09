using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshGeneration.WingedEdge
{
    public static class WingedEdgeExtensions
    {
        public static Mesh ToUnityMesh(this WingedEdgeMesh wingedEdgeMesh)
        {
            var mesh = new Mesh();
            
            if (wingedEdgeMesh.Vertices.Count == 0) return mesh;

            var finalVertices = new List<Vector3>(wingedEdgeMesh.Vertices.Count);
            var vertexIndexMap = new Dictionary<int, int>(wingedEdgeMesh.Vertices.Count);
            foreach (var vertex in wingedEdgeMesh.Vertices)
            {
                vertexIndexMap[vertex.index] = finalVertices.Count;
                finalVertices.Add(vertex.position);
            }
            
            mesh.vertices = finalVertices.ToArray();
            
            if (wingedEdgeMesh.Faces.Count == 0) return mesh;
            
            bool allQuads = wingedEdgeMesh.Faces.All(f => f.FaceVertices.Count() == 4);

            if (allQuads)
            {
                // Quads
                var quads = new List<int>(wingedEdgeMesh.Faces.Count * 4);
                foreach (var face in wingedEdgeMesh.Faces)
                {
                    foreach (var vertex in face.FaceVertices)
                    {
                        quads.Add(vertexIndexMap[vertex.index]);
                    }
                }
                mesh.SetIndices(quads.ToArray(), MeshTopology.Quads, 0);
            }
            else
            {
                // CAS GENERAL : On triangule tout
                var triangles = new List<int>();
                foreach (var face in wingedEdgeMesh.Faces)
                {
                    var faceVertices = face.FaceVertices.ToList();
                    if (faceVertices.Count < 3) continue;

                    // Triangulation en éventail
                    int rootIndex = vertexIndexMap[faceVertices[0].index];
                    for (int i = 1; i < faceVertices.Count - 1; i++)
                    {
                        int indexA = vertexIndexMap[faceVertices[i].index];
                        int indexB = vertexIndexMap[faceVertices[i + 1].index];
                        
                        triangles.Add(rootIndex);
                        triangles.Add(indexB);
                        triangles.Add(indexA);
                    }
                }
                mesh.SetIndices(triangles.ToArray(), MeshTopology.Triangles, 0);
            }
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();

            return mesh;
        }
    }
}