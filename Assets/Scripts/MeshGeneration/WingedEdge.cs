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
            int triangleCount = mesh.GetIndices(0).Length / 3;
            _faces = new List<WingedEdge.Face>(triangleCount);

            BuildVertices(mesh.vertices);

            int verticesPerFace = mesh.GetTopology(0) switch
            {
                MeshTopology.Triangles => 3,
                MeshTopology.Quads => 4,
                _ => throw new ArgumentException("Unsupported mesh topology. Only Triangles and Quads are supported.")
            };

            BuildTopology(mesh.GetIndices(0), verticesPerFace);
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

            //if (mesh.GetTopology(0) != MeshTopology.Triangles)
                //throw new ArgumentException("Mesh must be of type Triangles.");
        }

        private void BuildVertices(Vector3[] positions)
        {
            for (int i = 0; i < positions.Length; i++)
                _vertices.Add(new WingedEdge.Vertex(i, positions[i], null));
        }

        private void BuildTopology(int[] indices, int vertsPerFace)
        {
            if (indices.Length % vertsPerFace != 0)
                throw new ArgumentException($"La longueur des indices ({indices.Length}) n'est pas un multiple de {vertsPerFace}. " +
                                            "Maillage incomplet ou mal formé.");
            
            (WingedEdge edge, bool isNew)[] edgesInFace = new (WingedEdge, bool)[vertsPerFace];
            int faceCount = indices.Length / vertsPerFace;

            for (int f = 0; f < faceCount; ++f)
            {
                int baseIdx = f * vertsPerFace;
                var face = new WingedEdge.Face(f, null);
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

        private WingedEdge GetOrCreateEdge(WingedEdge.Vertex start,
                                           WingedEdge.Vertex end,
                                           WingedEdge.Face rightFace,
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
        
        #region Catmull-Clark Subdivision

        /// <summary>
        /// Applique une itération de l'algorithme de subdivision de Catmull-Clark.
        /// </summary>
        /// <returns>Un nouveau WingedEdgeMesh représentant le maillage subdivisé.</returns>
        public WingedEdgeMesh SubdivideCatmullClark()
        {
            // Étape 1: Calculer la position de tous les nouveaux points sans modifier la topologie.
            CatmullClarkCreateNewPoints(
                out var facePoints, 
                out var edgePoints, 
                out var newVertexPositions);

            // Étape 2 & 3: Construire la nouvelle topologie (nouveaux sommets, faces et arêtes).
            // La stratégie la plus simple est de créer un nouveau Mesh Unity et de l'utiliser
            // pour construire un nouveau WingedEdgeMesh.

            var subdividedMesh = new Mesh { name = "Subdivided_" + (_faces.Count > 0 ? _faces[0].GetType().Name : "Mesh") };

            var newVertices = new List<Vector3>();
            var newIndices = new List<int>();

            // Les nouveaux sommets sont composés des anciens sommets (déplacés),
            // des edge points et des face points.
            // On garde une trace des indices de départ pour chaque type de point.
            int originalVertexCount = _vertices.Count;
            int edgePointVertexStartIndex = originalVertexCount;
            int facePointVertexStartIndex = edgePointVertexStartIndex + _edges.Count;

            newVertices.AddRange(newVertexPositions);
            newVertices.AddRange(edgePoints);
            newVertices.AddRange(facePoints);

            // Pour chaque face originale, on crée N nouvelles faces (où N est le nombre de sommets de la face).
            // Chaque nouvelle face est un quad.
            foreach (var face in _faces)
            {
                var verticesOfFace = GetVerticesOfFace(face);
                int faceVertexCount = verticesOfFace.Count;
                if (faceVertexCount < 3) continue;

                int facePointIndex = facePointVertexStartIndex + face.index;

                for (int i = 0; i < faceVertexCount; i++)
                {
                    var currentVertex = verticesOfFace[i];
                    var nextVertex = verticesOfFace[(i + 1) % faceVertexCount];

                    // On trouve les arêtes qui relient les sommets de la face
                    var edgeAfter = FindEdge(currentVertex, nextVertex);
                    var edgeBefore = FindEdge(verticesOfFace[(i - 1 + faceVertexCount) % faceVertexCount], currentVertex);

                    if (edgeAfter == null || edgeBefore == null)
                    {
                        Debug.LogError("Could not find edge for face construction. Topology might be corrupted.");
                        continue;
                    }
                    
                    int edgePointAfterIndex = edgePointVertexStartIndex + edgeAfter.index;
                    int edgePointBeforeIndex = edgePointVertexStartIndex + edgeBefore.index;

                    // Création du nouveau quad. L'ordre est crucial pour avoir une normale correcte.
                    // Sommet -> EdgePoint d'après -> FacePoint -> EdgePoint d'avant
                    newIndices.Add(currentVertex.index);
                    newIndices.Add(edgePointAfterIndex);
                    newIndices.Add(facePointIndex);
                    newIndices.Add(edgePointBeforeIndex);
                }
            }

            subdividedMesh.SetVertices(newVertices);
            subdividedMesh.SetIndices(newIndices, MeshTopology.Quads, 0);
            subdividedMesh.RecalculateNormals();
            subdividedMesh.RecalculateBounds();

            // On crée le WingedEdgeMesh final à partir du maillage Unity qu'on vient de construire.
            return new WingedEdgeMesh(subdividedMesh);
        }

        /// <summary>
        /// Calcule les positions des face points, edge points et des nouveaux sommets
        /// selon les règles de Catmull-Clark.
        /// </summary>
        private void CatmullClarkCreateNewPoints(
            out List<Vector3> facePoints, 
            out List<Vector3> edgePoints, 
            out List<Vector3> newVertexPositions)
        {
            // Initialisation des listes de sortie
            facePoints = new List<Vector3>(_faces.Count);
            edgePoints = new List<Vector3>(_edges.Count);
            newVertexPositions = new List<Vector3>(_vertices.Count);

            // 1. Calculer les "Face Points" (centroïde de chaque face)
            foreach (var face in _faces)
            {
                var verticesOfFace = GetVerticesOfFace(face);
                Vector3 centroid = Vector3.zero;
                foreach (var v in verticesOfFace)
                {
                    centroid += v.position;
                }
                facePoints.Add(centroid / verticesOfFace.Count);
            }

            // 2. Calculer les "Edge Points"
            foreach (var edge in _edges)
            {
                // Cas d'une arête de bordure
                if (edge.leftFace == null)
                {
                    edgePoints.Add((edge.startVertex.position + edge.endVertex.position) * 0.5f);
                }
                // Cas d'une arête intérieure
                else
                {
                    Vector3 edgePoint = (edge.startVertex.position + edge.endVertex.position +
                                         facePoints[edge.rightFace.index] + facePoints[edge.leftFace.index]) * 0.25f;
                    edgePoints.Add(edgePoint);
                }
            }

            // 3. Calculer les nouvelles positions des sommets originaux
            foreach (var vertex in _vertices)
            {
                var incidentEdges = GetIncidentEdges(vertex);
                int n = incidentEdges.Count;
                if (n == 0)
                {
                    newVertexPositions.Add(vertex.position); // Sommet isolé
                    continue;
                }

                // Détecter si le sommet est sur une bordure
                bool isBoundaryVertex = incidentEdges.Exists(e => e.leftFace == null);

                if (isBoundaryVertex)
                {
                    Vector3 newPos = Vector3.zero;
                    int boundaryEdgeCount = 0;
                    foreach (var edge in incidentEdges)
                    {
                        if (edge.leftFace == null) // C'est une arête de bordure
                        {
                            newPos += (edge.startVertex.position + edge.endVertex.position) * 0.5f; // Midpoint
                            boundaryEdgeCount++;
                        }
                    }
                    
                    if (boundaryEdgeCount == 2) // Cas standard pour un sommet de bordure
                    {
                        newPos = (newPos + vertex.position) / 3.0f;
                    }
                    else // Cas d'un "coin" ou autre situation complexe, on se contente de ne pas le bouger
                    {
                        newPos = vertex.position;
                    }
                    newVertexPositions.Add(newPos);
                }
                else // Cas d'un sommet intérieur
                {
                    // Q = moyenne des face points des faces adjacentes
                    var adjacentFaces = GetAdjacentFaces(vertex);
                    Vector3 Q = Vector3.zero;
                    foreach (var face in adjacentFaces)
                    {
                        Q += facePoints[face.index];
                    }
                    Q /= adjacentFaces.Count;

                    // R = moyenne des mid-points des arêtes incidentes
                    Vector3 R = Vector3.zero;
                    foreach (var edge in incidentEdges)
                    {
                        R += (edge.startVertex.position + edge.endVertex.position) * 0.5f;
                    }
                    R /= n;

                    // Appliquer la formule
                    Vector3 V = vertex.position;
                    newVertexPositions.Add((Q + 2.0f * R + (n - 3.0f) * V) / n);
                }
            }
        }

        // --- Méthodes utilitaires pour la topologie ---

        public List<WingedEdge.Vertex> GetVerticesOfFace(WingedEdge.Face face)
        {
            var vertices = new List<WingedEdge.Vertex>();
            if (face.edge == null) return vertices;

            WingedEdge startEdge = face.edge;
            WingedEdge currentEdge = startEdge;
            do
            {
                if (currentEdge.rightFace == face)
                {
                    vertices.Add(currentEdge.startVertex);
                    currentEdge = currentEdge.endCCWEdge;
                }
                else // La face est à gauche
                {
                    vertices.Add(currentEdge.endVertex);
                    currentEdge = currentEdge.startCCWEdge;
                }
                if (currentEdge == null) break; // Bordure
            } while (currentEdge != startEdge);
            
            return vertices;
        }

        public List<WingedEdge> GetIncidentEdges(WingedEdge.Vertex vertex)
        {
            var edges = new List<WingedEdge>();
            if (vertex.edge == null) return edges;

            WingedEdge startEdge = vertex.edge;
            WingedEdge currentEdge = startEdge;
            
            // Parcourir les arêtes autour du sommet dans un sens (CCW)
            do
            {
                edges.Add(currentEdge);
                currentEdge = (currentEdge.startVertex == vertex) ? currentEdge.startCCWEdge : currentEdge.endCCWEdge;
            } while (currentEdge != startEdge && currentEdge != null);

            // Si on a atteint une bordure, il faut repartir dans l'autre sens (CW)
            if (currentEdge == null)
            {
                currentEdge = (startEdge.startVertex == vertex) ? startEdge.startCWEdge : startEdge.endCWEdge;
                while (currentEdge != null)
                {
                    edges.Add(currentEdge);
                    currentEdge = (currentEdge.startVertex == vertex) ? currentEdge.startCWEdge : currentEdge.endCWEdge;
                }
            }
            return edges;
        }

        public List<WingedEdge.Face> GetAdjacentFaces(WingedEdge.Vertex vertex)
        {
            var faces = new List<WingedEdge.Face>();
            foreach (var edge in GetIncidentEdges(vertex))
            {
                if (edge.rightFace != null && !faces.Contains(edge.rightFace))
                    faces.Add(edge.rightFace);
                if (edge.leftFace != null && !faces.Contains(edge.leftFace))
                    faces.Add(edge.leftFace);
            }
            return faces;
        }

        private WingedEdge FindEdge(WingedEdge.Vertex v1, WingedEdge.Vertex v2)
        {
            if (_edgeMap.TryGetValue((v1.index, v2.index), out var edge))
                return edge;
            if (_edgeMap.TryGetValue((v2.index, v1.index), out edge))
                return edge;
            return null;
        }

        #endregion
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
