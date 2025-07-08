using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MeshGeneration.WingedEdge
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
        
        private Vertex AddVertex(Vector3 pos)
        {
            var v = new Vertex(_vertices.Count, pos, null);
            _vertices.Add(v);
            return v;
        }

        private WingedEdge AddEdge(Vertex s, Vertex t)
        {
            var e = new WingedEdge(_edges.Count, s, t,
                null, null, null,null,null,null);
            _edges.Add(e);
            return e;
        }

        private Face AddFace(WingedEdge anyEdge)
        {
            var f = new Face(_faces.Count, anyEdge);
            _faces.Add(f);
            return f;
        }
        
        public void CatmullClark(int iterations)
        {
            if (iterations is < 1 or > 100)
            {
                throw new ArgumentException("Le nombre d'itérations doit être compris entre 1 et 100.");
            }
            
            for (int i = 0; i < iterations; i++)
            {
                CatmullClark();
            }
        }
        
        public void CatmullClark()
        {
            // Création des nouveaux points
            this.CatmullClarkCreateNewPoints(out var facePoints, out var edgePoints, out var vertexPoints);
            
            // Split des edges
            foreach (var edge in this.Edges.ToList())
            {
                this.SplitEdge(edge, edgePoints[edge.index]);
            }
            
            // Split des faces
            foreach (var f in this.Faces.ToList())
                this.SplitFace(f, facePoints[f.index]);
            
            // Mise à jour des positions des sommets
            for (int i = 0; i < vertexPoints.Count; i++)
            {
                var vertex = this.Vertices[i];
                vertex.position = vertexPoints[i];
            }
        }
        
        #region Catmull-Clark-Internal
        
        private static void FixNeighbour(WingedEdge e, WingedEdge oldE, WingedEdge newE)
        {
            if (e == null) return;
            
            if (e.startCWEdge == oldE)
                e .startCWEdge  = newE;

            if (e.startCCWEdge == oldE)
                e.startCCWEdge = newE;
                
            if (e.endCWEdge == oldE)
                e.endCWEdge = newE;
                
            if (e.endCCWEdge == oldE)
                e.endCCWEdge = newE;
        }
        
        private static void FixFace(WingedEdge e, Face oldFace, Face newFace)
        {
            if (e == null) return;
            
            if (e.rightFace == oldFace)
                e.rightFace = newFace;
            else if (e.leftFace == oldFace)
                e.leftFace = newFace;
            else
                throw new ArgumentException("L'arête précédente ne fait pas partie de la face spécifiée.");
        }
        
        private void SplitFace(Face face, Vector3 splittingPoint)
        {
            Vertex centerVertex = AddVertex(splittingPoint);
            
            // On récupère les arêtes de la face

            var edges = face.FaceEdges.ToList();
            
            // On considère que les edges points corresponds au edge.endVertex avec un pas de 2
            // De plus, on sait que face.edge correspond à une edge qui en endVertex un edgepoint car 
            // Car on a split les arêtes de la face lors du splitEdge et on a recyclé l'edge existante.
            // On va quand même assert que c'est un multiple de 2
            if (edges.Count % 2 != 0)
            {
                throw new ArgumentException("La face doit avoir un nombre pair d'arêtes pour être divisée.");
            }
            
            int numberOfFaces = edges.Count / 2;
            
            WingedEdge[] newEdges = new WingedEdge[numberOfFaces];
            Face[] newFaces = new Face[numberOfFaces];
            newFaces[0] = face; // On recycle la face existante
            
            for (int i = 0; i < edges.Count; i += 2)
            {
                WingedEdge currentEdge = edges[i];
                
                WingedEdge nextEdge = face.NextEdgeAroundFace(currentEdge);
                Vertex nextVertex = face.NextVertexAroundFace(nextEdge);
                
                newEdges[i / 2] = AddEdge(nextVertex, centerVertex);
                newFaces[i / 2] ??= AddFace(newEdges[i / 2]);
            }
            
            centerVertex.edge = newEdges[0]; // On rattache le sommet central à la première arête
            
            // On connecte les nouvelles arêtes entre elles et aux nouvelles faces
            for (int i = 0; i < newEdges.Length; i++)
            {
                var currentNewEdge = newEdges[i];
                currentNewEdge.rightFace = newFaces[i];
                currentNewEdge.leftFace = newFaces[(i + 1) % newEdges.Length];
                
                currentNewEdge.endCWEdge = newEdges[(i + 1) % newEdges.Length];
                currentNewEdge.endCCWEdge = newEdges[(i - 1 + newEdges.Length) % newEdges.Length];
            }
            
            // Maintenant on met à jour les arêtes de la face originale pour se connecter aux nouveaux sommets et aux nouvelles arêtes
            for (int i = 0; i < edges.Count; i+=2)
            {
                WingedEdge currentEdge = edges[i];
                WingedEdge previousEdge = edges[(i - 1 + edges.Count) % edges.Count];
                
                WingedEdge currentNewEdge = newEdges[i / 2];
                WingedEdge previousNewEdge = newEdges[(i / 2 - 1 + newEdges.Length) % newEdges.Length];

                currentNewEdge.startCWEdge = currentEdge;
                previousNewEdge.startCCWEdge = previousEdge;
                
                if (face.IsEdgeOpposite(currentEdge))
                    currentEdge.startCCWEdge = currentNewEdge;
                else
                    currentEdge.endCCWEdge = currentNewEdge;
                
                if (face.IsEdgeOpposite(previousEdge))
                    previousEdge.endCWEdge = previousNewEdge;
                else
                    previousEdge.startCWEdge = previousNewEdge;
                
                FixFace(currentEdge, face, newFaces[i / 2]);
                FixFace(previousEdge, face, newFaces[i / 2]);
            }
        }
        
        private WingedEdge SplitEdge(WingedEdge edge, Vector3 splittingPoint)
        {
            Vertex mid = AddVertex(splittingPoint);
            
            Vertex oldEnd = edge.endVertex;
            WingedEdge oldEndCW = edge.endCWEdge;
            WingedEdge oldEndCCW = edge.endCCWEdge;

            edge.endVertex = mid;
            edge.endCWEdge = null;
            edge.endCCWEdge = null;
            
            WingedEdge newEdge = AddEdge(mid, oldEnd);
            
            newEdge.rightFace = edge.rightFace;
            newEdge.leftFace = edge.leftFace;
            
            newEdge.endCWEdge = oldEndCW;
            newEdge.endCCWEdge = oldEndCCW;
            
            FixNeighbour(oldEndCW, edge, newEdge);
            FixNeighbour(oldEndCCW, edge, newEdge);
            
            edge.endCCWEdge = newEdge;
            edge.endCWEdge = newEdge;
            newEdge.startCWEdge = edge;
            newEdge.startCCWEdge = edge;
            
            mid.edge = newEdge;
            
            // Si le sommet 'oldEnd' pointait vers l'arête que nous venons de modifier,
            // il faut le faire pointer vers la nouvelle arête qui part de lui.
            if (oldEnd.edge == edge)
            {
                oldEnd.edge = newEdge;
            }

            if (!edge.IsBorderEdge)
            {
                // On met à jour la face gauche si elle pointait vers l'ancienne arête
                // ça permettra d'itérer à partir d'un sommet qui n'est pas un edge point pour le split face
                // car le split face suppose que vertex de départ (startVertex ou endVertex selon le sens)
                // n'est pas un edge point.
                
                var leftFace = edge.leftFace;
                if (leftFace.edge == edge)
                    leftFace.edge = newEdge;
            }

            return newEdge;
        }
        
        public void CatmullClarkCreateNewPoints(out IReadOnlyList<Vector3> facePoints,
                                        out IReadOnlyList<Vector3> edgePoints,
                                        out IReadOnlyList<Vector3> vertexPoints)
        {
            int fCount = _faces.Count;
            int eCount = _edges.Count;
            int vCount = _vertices.Count;

            var fPts = new Vector3[fCount];
            var ePts = new Vector3[eCount];
            var vPts = new Vector3[vCount];

            // Facepoints
            foreach (var face in _faces)
            {
                fPts[face.index] = face.Centroid;
            }
            
             // EDGE POINTS
             // Intérieur : (V0 + V1 + F_left + F_right) / 4
             // Bord : (V0 + V1) / 2
            foreach (var edge in _edges)
            {
                Vector3 v0 = edge.startVertex.position;
                Vector3 v1 = edge.endVertex.position;

                if (edge.IsBorderEdge) // bord
                {
                    ePts[edge.index] = 0.5f * (v0 + v1);
                }
                else // intérieur
                {
                    Vector3 fR = fPts[edge.rightFace.index];
                    Vector3 fL = fPts[edge.leftFace.index];
                    ePts[edge.index] = 0.25f * (v0 + v1 + fR + fL);
                }
            }
            
             // VERTEX POINTS
             // Intérieur : (Q + 2R + (n-3)V) / n
             // Bord : (E1 + E2 + 4V) / 6
             // E1/E2 = midpoints des deux arêtes frontières incidentes.
            foreach (var v in _vertices)
            {
                Vector3 faceSum = Vector3.zero;
                Vector3 edgeMidSum = Vector3.zero;
                int faceCounter = 0;
                int edgeCounter = 0;

                HashSet<int> uniqueFaces = new(); // évite de dupliquer le même F

                foreach (var e in v.IncidentEdges)
                {
                    // Edges
                    edgeCounter++;
                    Vertex other = (e.startVertex == v) ? e.endVertex : e.startVertex;
                    edgeMidSum += 0.5f * (v.position + other.position);

                    // Faces
                    if (e.rightFace != null && uniqueFaces.Add(e.rightFace.index))
                    {
                        faceSum += fPts[e.rightFace.index];
                        faceCounter++;
                    }
                    if (e.leftFace != null && uniqueFaces.Add(e.leftFace.index))
                    {
                        faceSum += fPts[e.leftFace.index];
                        faceCounter++;
                    }
                }

                if (v.IsBorderVertex)
                {
                    // On récupère les DEUX midpoints de bord (E1, E2)
                    Vector3 E1 = Vector3.zero, E2 = Vector3.zero;
                    int k = 0;
                    foreach (var e in v.IncidentEdges)
                    {
                        if (e.leftFace == null || e.rightFace == null)
                        {
                            Vertex other = (e.startVertex == v) ? e.endVertex : e.startVertex;
                            if (k == 0) E1 = 0.5f * (v.position + other.position);
                            else if (k == 1) E2 = 0.5f * (v.position + other.position);
                            k++;
                        }
                    }
                    vPts[v.index] = (E1 + E2 + 4f * v.position) / 6f;
                }
                else
                {
                    float n = faceCounter; // valence
                    Vector3 Q = faceSum / n;
                    Vector3 R = edgeMidSum / edgeCounter;
                    vPts[v.index] = (Q + 2f * R + (n - 3f) * v.position) / n;
                }
            }
            
            facePoints = fPts;
            edgePoints = ePts;
            vertexPoints = vPts;
        }
        #endregion
        
        #region Builder
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
        #endregion
    }
}