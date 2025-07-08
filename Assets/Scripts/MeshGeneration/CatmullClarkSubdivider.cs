using MeshGeneration.WingedEdge;
using UnityEngine;

namespace MeshGeneration
{
    [RequireComponent(typeof(MeshFilter))]
    public class CatmullClarkSubdivider : MonoBehaviour
    {
        [Tooltip("Le nombre de fois que l'algorithme de Catmull-Clark sera appliqué.")]
        [Range(1, 6)]
        public int subdivisionLevels = 1;
        
        // Debug
        [Header("Debug")]
        public WingedEdgeDebugger wingedEdgeDebugger;

        private Mesh _originalMesh;
        private Mesh _subdividedMesh;
        private WingedEdgeMesh.WingedEdgeMeshBuilder wingedEdgeMeshBuilder = new WingedEdgeMesh.WingedEdgeMeshBuilder();
        
        public bool IsSubdivided => _subdividedMesh != null;

        private MeshFilter _meshFilter;
        
        public void SaveOriginalMesh()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                Debug.LogError("MeshFilter component is missing.", this);
                return;
            }

            // On sauvegarde le maillage original
            _originalMesh = _meshFilter.sharedMesh;
            if (_originalMesh == null)
            {
                Debug.LogError("No original mesh found on MeshFilter.", this);
                return;
            }
            
            if (wingedEdgeDebugger != null)
            {
                var wingedEdgeMesh = wingedEdgeMeshBuilder.CreateFrom(_originalMesh);
                wingedEdgeDebugger.SetTarget(wingedEdgeMesh);
            }
        }

        /// <summary>
        /// Applique l'algorithme de Catmull-Clark au maillage original.
        /// </summary>
        public void ApplySubdivision()
        {
            if (_originalMesh == null)
            {
                Debug.LogError("No original mesh found to subdivide.", this);
                return;
            }

            // On part toujours du maillage original pour la subdivision
            var wingedEdgeMesh = wingedEdgeMeshBuilder.CreateFrom(_originalMesh);
            
            for (int i = 0; i < subdivisionLevels; i++)
            {
                wingedEdgeMesh.CatmullClark();
            }

            // Nettoyer l'ancien maillage subdivisé s'il existe
            if (_subdividedMesh != null)
            {
                DestroyImmediate(_subdividedMesh);
            }

            // Créer le nouveau maillage et l'assigner
            _subdividedMesh = wingedEdgeMesh.ToUnityMesh();
            _subdividedMesh.name = $"Subdivided_{gameObject.name}_L{subdivisionLevels}";
            _meshFilter.sharedMesh = _subdividedMesh;

            Debug.Log($"Applied {subdivisionLevels} level(s) of Catmull-Clark subdivision.", this);
            
            wingedEdgeDebugger?.SetTarget(wingedEdgeMesh);
        }

        /// <summary>
        /// Restaure le maillage original.
        /// </summary>
        public void RevertToOriginal()
        {
            if (_originalMesh == null)
            {
                Debug.LogWarning("No original mesh to revert to.", this);
                return;
            }

            // Nettoyer le maillage subdivisé
            if (_subdividedMesh != null)
            {
                DestroyImmediate(_subdividedMesh);
                _subdividedMesh = null;
            }

            // Réassigner le maillage original
            _meshFilter.sharedMesh = _originalMesh;
            Debug.Log("Reverted to the original mesh.", this);
            
            if (wingedEdgeDebugger != null)
            {
                var wingedEdgeMesh = wingedEdgeMeshBuilder.CreateFrom(_originalMesh);
                wingedEdgeDebugger.SetTarget(wingedEdgeMesh);
            }
        }

        /// <summary>
        /// Nettoie les maillages instanciés lorsque le composant est détruit.
        /// </summary>
        private void OnDestroy()
        {
            // S'assurer que le MeshFilter ne pointe plus vers nos maillages temporaires
            if (_meshFilter != null && (_meshFilter.sharedMesh == _originalMesh || _meshFilter.sharedMesh == _subdividedMesh))
            {
                _meshFilter.sharedMesh = null;
            }
            
            if (_subdividedMesh != null)
            {
                DestroyImmediate(_subdividedMesh);
            }
        }
    }
}