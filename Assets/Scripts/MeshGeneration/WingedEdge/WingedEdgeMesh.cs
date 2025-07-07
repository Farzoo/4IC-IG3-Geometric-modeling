using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WingedEdge
{
    public sealed class WingedEdgeMesh
    {
        public List<Vertex> Vertices => _vertices;
        public List<WingedEdge> Edges => _edges;
        public List<Face> Faces => _faces;
        
        private readonly List<Vertex> _vertices;
        private readonly List<WingedEdge> _edges;
        private readonly List<Face> _faces;
        
        private WingedEdgeMesh(List<Vertex> vertices, List<WingedEdge> edges, List<Face> faces)
        {
            _vertices = vertices;
            _edges = edges;
            _faces = faces;
        }

        public class WingedEdgeMeshBuilder
        {
            private List<Vertex> _vertices;
            private List<WingedEdge> _edges;
            private List<Face> _faces;

            private Dictionary<(int start, int end), WingedEdge> _edgeMap;
        
            private Dictionary<(int start, int end), WingedEdge> _remainingEdges;
        
            private Dictionary<Vertex, List<WingedEdge>> incidentEdges;

            private int _nextEdgeIndex;
            
            
            public WingedEdgeMeshBuilder()
            {
            }
            
            private void Initialize(Mesh mesh)
            {
                _vertices = new List<Vertex>(mesh.vertexCount);
                _edges = new List<WingedEdge>(mesh.vertexCount * 3);
                int triangleCount = mesh.GetIndices(0).Length / 3;
                _faces = new List<Face>(triangleCount);

                _edgeMap = new Dictionary<(int, int), WingedEdge>();
                _remainingEdges = new Dictionary<(int, int), WingedEdge>();
                incidentEdges = new Dictionary<Vertex, List<WingedEdge>>();
                
                _nextEdgeIndex = 0;
            }
            
            public WingedEdgeMesh CreateFrom(Mesh mesh)
            {
                ValidateInput(mesh);

                Initialize(mesh);

                BuildVertices(mesh.vertices);

                int verticesPerFace = mesh.GetTopology(0) switch
                {
                    MeshTopology.Triangles => 3,
                    MeshTopology.Quads => 4,
                    _ => throw new ArgumentException(
                        "Unsupported mesh topology. Only Triangles and Quads are supported.")
                };

                BuildTopology(mesh.GetIndices(0), verticesPerFace);
                
                WeaveMeshBorders();
                
                return new WingedEdgeMesh(_vertices, _edges, _faces);
            }

            private static void ValidateInput(Mesh mesh)
            {
                switch (mesh.subMeshCount)
                {
                    case > 1:
                        Debug.LogWarning("Mesh contains multiple submeshes, only the first one will be processed.");
                        break;
                    case 0:
                        throw new ArgumentException("Mesh must contain at least one submesh.");
                }

                //if (mesh.GetTopology(0) != MeshTopology.Triangles)
                //throw new ArgumentException("Mesh must be of type Triangles.");
            }

            private void BuildVertices(Vector3[] positions)
            {
                for (int i = 0; i < positions.Length; i++)
                    _vertices.Add(new Vertex(i, positions[i], null));
            }

            private void BuildTopology(int[] indices, int vertsPerFace)
            {
                if (indices.Length % vertsPerFace != 0)
                    throw new ArgumentException(
                        $"La longueur des indices ({indices.Length}) n'est pas un multiple de {vertsPerFace}. " +
                        "Maillage incomplet ou mal formé.");

                (WingedEdge edge, bool isNew)[] edgesInFace = new (WingedEdge, bool)[vertsPerFace];
                int faceCount = indices.Length / vertsPerFace;

                for (int f = 0; f < faceCount; ++f)
                {
                    int baseIdx = f * vertsPerFace;
                    var face = new Face(f, null);
                    _faces.Add(face);

                    for (int i = 0; i < vertsPerFace; ++i)
                    {
                        int i0 = indices[baseIdx + i];
                        int i1 = indices[baseIdx + (i + 1) % vertsPerFace];
                        edgesInFace[i].edge = GetOrCreateEdge(
                            _vertices[i0], _vertices[i1], // start -> end (CW)
                            face,
                            out edgesInFace[i].isNew);
                    }

                    // Maillage local des wings
                    for (int i = 0; i < vertsPerFace; ++i)
                    {
                        int prev = (i - 1 + vertsPerFace) % vertsPerFace;
                        int next = (i + 1) % vertsPerFace;
                        var e = edgesInFace[i].edge;

                        if (edgesInFace[i].isNew)
                        {
                            e.startCWEdge = edgesInFace[prev].edge;
                            e.endCCWEdge = edgesInFace[next].edge;
                            face.edge ??= e;
                        }
                        else // On vient de rencontrer l’arête opposée
                        {
                            e.startCCWEdge = edgesInFace[next].edge;
                            e.endCWEdge = edgesInFace[prev].edge;
                            e.leftFace = face;
                        }
                    }
                }
            }

            private WingedEdge GetOrCreateEdge(Vertex start,
                Vertex end,
                Face rightFace,
                out bool isNewEdge)
            {
                isNewEdge = false;
                // Dans notre cas, un maillage 2-manifold ne peut pas avoir deux arêtes avec les mêmes sommets et qui ont la même direction.
                var key = (start.index, end.index);
                if (_edgeMap.TryGetValue(key, out var existingSameDir))
                {
                    throw new ArgumentException(
                        $"Problème topologique : le maillage n'est pas 2-manifold à l'arête {start.index}->{end.index} \n" +
                        "Soit un triangle est dessiné en CCW (normale inversée), soit deux triangles se superposent.");
                }

                // Vérifier l'existence dans l'autre sens.
                var revKey = (end.index, start.index);
                if (_edgeMap.TryGetValue(revKey, out var existingOpposite))
                {
                    // On a trouvé l'arête dans l'autre sens, on met à jour sa gauche.
                    if (existingOpposite.leftFace is not null)
                        throw new ArgumentException(
                            $"Problème topologique : le maillage n'est pas 2-manifold à l'arête {end.index}->{start.index} \n" +
                            "Plus de deux faces partagent cette arête.");

                    existingOpposite.leftFace = rightFace;

                    rightFace.edge ??= existingOpposite;

                    _remainingEdges.Remove(revKey);

                    return existingOpposite;
                }

                // L'arête n'existe pas, on la crée.
                var edge = new WingedEdge(_nextEdgeIndex++, start, end,
                    rightFace, null,
                    null, null, null, null);

                isNewEdge = true;

                start.edge ??= edge;

                _edgeMap[key] = edge;
                _edges.Add(edge);

                _remainingEdges[key] = edge;

                // On ajoute l'arête dans la liste des arêtes incidentes.
                if (!incidentEdges.TryGetValue(start, out var incidentList))
                {
                    incidentList = new List<WingedEdge>();
                    incidentEdges[start] = incidentList;
                }

                incidentList.Add(edge);

                return edge;
            }

            private void WeaveMeshBorders()
            {
                foreach (WingedEdge edge in _remainingEdges.Values)
                {

                    // On va récupérer l'arête qui est une bordure pour le endVertex de l'arête courante.
                    var key = edge.endVertex;
                    if (!incidentEdges.TryGetValue(key, out var incidentList))
                        throw new InvalidOperationException(
                            "Ce cas ne devrait pas arriver, cela voudrait dire que le sommet n'a pas d'arêtes incidentes alors que l'arête courante est une bordure."
                        );

                    if (incidentList.Count < 1)
                        throw new ArgumentException(
                            $"Problème topologique : le sommet {key.index} (fin de l'arête {edge.index}) n'est relié à aucune autre arête de bordure sortante. (dangling vertex)"
                        );

                    WingedEdge endVertexEdge = null;
                    foreach (var incidentEdge in incidentList)
                    {
                        if (incidentEdge.index == edge.index)
                            continue; // On ignore l'arête courante.

                        if (incidentEdge.leftFace is not null)
                            continue; // On ignore les arêtes qui ne sont pas des bordures.

                        if (endVertexEdge is not null)
                        {
                            throw new ArgumentException(
                                $"Problème topologique : Sommet de bordure non-manifold détecté au vertex {key.index}. Plus d'une arête de bordure sortante possible."
                            );
                        }

                        endVertexEdge = incidentEdge; // On a trouvé une arête qui est une bordure.
                        // On continue d'itérer pour vérifier si on tombe dans le cas non-manifold.
                    }

                    if (endVertexEdge is null)
                    {
                        throw new ArgumentException(
                            $"Problème topologique : l'arête {edge.startVertex.index}->{edge.endVertex.index} n'est pas reliée à une arête de bordure."
                        );
                    }

                    // On met à jour les arêtes de bordure.
                    edge.endCWEdge = endVertexEdge;
                    endVertexEdge.startCCWEdge = edge;
                    // A voir si on veut mettre à jour la face gauche de l'arête de bordure.
                }
            }
        }
    }
}