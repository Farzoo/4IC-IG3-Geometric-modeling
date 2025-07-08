using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MeshGeneration.WingedEdge
{
    /// <summary>
    /// Affiche la géométrie et la topologie de base d'un WingedEdgeMesh.
    /// Doit être placé sur le même GameObject que le MeshBehaviour.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class WingedEdgeDebugger : MonoBehaviour
    {
        [Header("Affichage des Éléments")]
        public bool showVertices = true;
        public bool showEdges = true;
        public bool showFaces = true;

        [Header("Affichage des IDs")]
        public bool showVertexIds = true;
        public bool showEdgeIds = true;
        public bool showFaceIds = true;

        [Header("Affichage progressif des arêtes")]
        public bool stepByStepEdges = false;
        public float edgeDrawDelay = 0.5f; // Durée de la pause en secondes

        private int currentEdgeIndex = 0;
        private bool isDrawingEdges = false;

        [Header("Style Visuel")]
        public float vertexRadius = 0.03f;
        public Vector3 labelOffset = Vector3.up * 0.05f;

        [Header("Couleurs")]
        public Color vertexColor = Color.red;
        public Color edgeColor = Color.black;
        public Color faceIdColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        private WingedEdgeMesh _targetMesh;
        private GUIStyle _style;

        /// <summary>
        /// Méthode publique pour que d'autres scripts (comme MeshBehaviour) puissent fournir le maillage à debug.
        /// </summary>
        public void SetTarget(WingedEdgeMesh mesh)
        {
            _targetMesh = mesh;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (stepByStepEdges && !isDrawingEdges)
            {
                StartEdgeDrawing();
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isDrawingEdges = false;
            currentEdgeIndex = 0;
        }

        private void StartEdgeDrawing()
        {
            if (!isDrawingEdges)
            {
                isDrawingEdges = true;
                currentEdgeIndex = 0;
                UnityEditor.EditorApplication.update += EditorCoroutineStep;
            }
        }

        private void EditorCoroutineStep()
        {
            if (!stepByStepEdges || _targetMesh == null)
            {
                UnityEditor.EditorApplication.update -= EditorCoroutineStep;
                isDrawingEdges = false;
                return;
            }

            if (currentEdgeIndex < _targetMesh.Edges.Count)
            {
                currentEdgeIndex++;
                UnityEditor.SceneView.RepaintAll();
                System.Threading.Thread.Sleep((int)(edgeDrawDelay * 1000));
            }
            else
            {
                UnityEditor.EditorApplication.update -= EditorCoroutineStep;
                isDrawingEdges = false;
            }
        }

        private void OnDrawGizmos()
        {
            if (!this.isActiveAndEnabled)
                return;
            
            if (_targetMesh == null) return;

            if (stepByStepEdges && !isDrawingEdges)
            {
                StartEdgeDrawing();
            }
            
            if (_style == null)
            {
                _style = new GUIStyle
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            Matrix4x4 M = transform.localToWorldMatrix;
            
            if (showFaces && showFaceIds)
            {
                _style.normal.textColor = faceIdColor;
                foreach (var face in _targetMesh.Faces)
                {
                    Vector3 centroid = face.Centroid;
                    Handles.Label(M.MultiplyPoint3x4(centroid), face.index.ToString(), _style);
                }
            }
            
            if (showEdges)
            {
                _style.normal.textColor = edgeColor;
                int edgeCount = stepByStepEdges ? currentEdgeIndex : _targetMesh.Edges.Count;
                for (int i = 0; i < edgeCount; i++)
                {
                    var edge = _targetMesh.Edges[i];
                    Vector3 p0 = M.MultiplyPoint3x4(edge.startVertex.position);
                    Vector3 p1 = M.MultiplyPoint3x4(edge.endVertex.position);
                    Vector3 mid = (p0 + p1) * 0.5f;
                    
                    DrawLine(p0, p1, 3f, edgeColor);
                    DrawArrowHead(p0, p1);

                    if (showEdgeIds)
                    {
                        Handles.Label(mid + labelOffset, edge.index.ToString(), _style);
                    }
                }
            }
            
            if (showVertices)
            {
                _style.normal.textColor = vertexColor;
                Gizmos.color = vertexColor;
                foreach (var vertex in _targetMesh.Vertices)
                {
                    Vector3 worldPos = M.MultiplyPoint3x4(vertex.position);
                    Gizmos.DrawSphere(worldPos, vertexRadius);

                    if (showVertexIds)
                    {
                        Handles.Label(worldPos + labelOffset, vertex.index.ToString(), _style);
                    }
                }
            }
        }
        
        private void DrawArrowHead(Vector3 from, Vector3 to)
        {
            Handles.color = edgeColor;
            Vector3 dir = (from - to).normalized;
            Vector3 mid = (from + to) * 0.5f;
            Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 25, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -25, 0) * Vector3.forward;
            Handles.DrawAAPolyLine(3f, mid + right * 0.04f, mid, mid + left * 0.04f);
        }

        private static void DrawLine(Vector3 p0, Vector3 p1, float thickness, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(thickness, p0, p1);
        }
#endif
    }
}