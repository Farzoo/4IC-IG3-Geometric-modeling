using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace TP1
{
    public sealed class WingedEdgeMesh
    {
        public IReadOnlyList<WingedEdge.Vertex> Vertices => _vertices;
        public IReadOnlyList<WingedEdge> Edges => _edges;
        public IReadOnlyList<WingedEdge.Face> Faces => _faces;
        
        private readonly List<WingedEdge.Vertex> _vertices;
        private readonly List<WingedEdge> _edges;
        private readonly List<WingedEdge.Face> _faces;
        
        private readonly Dictionary<(int start, int end), WingedEdge> _edgeMap = new();
        
        private readonly Dictionary<(int start, int end), WingedEdge> _remainingEdges = new();
        
        private readonly Dictionary<WingedEdge.Vertex, List<WingedEdge>> incidentEdges = new();

        private int _nextEdgeIndex;
        
        public WingedEdgeMesh(Mesh mesh)
        {
            ValidateInput(mesh);

            _vertices = new List<WingedEdge.Vertex>(mesh.vertexCount);
            _edges = new List<WingedEdge>(mesh.vertexCount * 3);
            _faces = new List<WingedEdge.Face>(mesh.vertexCount / 3);

            BuildVertices(mesh.vertices);
            BuildTopology(mesh.GetIndices(0));
            WeaveMeshBorders();
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

            if (mesh.GetTopology(0) != MeshTopology.Triangles)
                throw new ArgumentException("Mesh must be of type Triangles.");
        }

        private void BuildVertices(Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
                _vertices.Add(new WingedEdge.Vertex(i, positions[i], null));
        }

        private void BuildTopology(int[] indices)
        {
            for (int t = 0; t < indices.Length; t += 3)
            {
                var v0 = _vertices[indices[t]];
                var v1 = _vertices[indices[t+1]];
                var v2 = _vertices[indices[t+2]];

                var face = new WingedEdge.Face(t / 3, null);
                _faces.Add(face);

                var e0 = GetOrCreateEdge(v0, v1, face, out var e0IsNew);
                var e1 = GetOrCreateEdge(v1, v2, face, out var e1IsNew);
                var e2 = GetOrCreateEdge(v2, v0, face, out var e2IsNew);

                if (e0IsNew)
                {
                    face.edge = e0;
                    // Wings locales au triangle pour l'instant
                    e0.startCWEdge = e2;
                    e0.endCCWEdge = e1;

                }
                else
                {
                    e0.startCCWEdge = e1;
                    e0.endCWEdge = e2;
                    e0.leftFace = face;
                }
                
                if (e1IsNew)
                {
                    // Wings locales au triangle pour l'instant
                    e1.startCWEdge = e0;
                    e1.endCCWEdge = e2;
                }
                else
                {
                    e1.startCCWEdge = e2;
                    e1.endCWEdge = e0;
                    e1.leftFace = face;
                }
                
                if (e2IsNew)
                {
                    // Wings locales au triangle pour l'instant
                    e2.startCWEdge = e1;
                    e2.endCCWEdge = e0;
                }
                else
                {
                    e2.startCCWEdge = e0;
                    e2.endCWEdge = e1;
                    e2.leftFace = face;
                }
            }
        }

        private WingedEdge GetOrCreateEdge(WingedEdge.Vertex start,
                                           WingedEdge.Vertex end,
                                           WingedEdge.Face rightFace,
                                           out bool isNewEdge)
        {
            isNewEdge = false;
            // Dans notre cas, un maillage 2‑manifold ne peut pas avoir deux arêtes avec les mêmes sommets et qui ont la même direction.
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
        
        
        // To string with all the properties of the WingedEdge class.
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
