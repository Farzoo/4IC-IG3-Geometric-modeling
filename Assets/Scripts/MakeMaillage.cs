using UnityEngine;

namespace TP1
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MakeMaillage : MonoBehaviour
    {
        
        public MeshFilter meshFilter;
        private Mesh mesh;
        
        void Start()
        {
            this.meshFilter = GetComponent<MeshFilter>();
            
            if (this.meshFilter == null)
            {
                Debug.LogError("MeshFilter component is missing.");
                return;
            }
    
            this.mesh = this.MakeQuad();
            
            this.meshFilter.mesh = this.mesh;
            
            
            if (this.mesh == null)
            {
                Debug.LogError("Failed to create mesh.");
                return;
            }
            
            Debug.Log("Mesh created successfully.");
        }
    
        // Update is called once per frame
        void Update()
        {
            
        }
    
        private Mesh MakeQuad()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, 1)
            };
            
            mesh.vertices = vertices;

            int[] indices = new int[6]
            {
                2, 1, 0,
                3, 2, 0
            };
            
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
    
            return mesh;
        }

        private Mesh MakeTriangle()
        {
            Mesh mesh = new Mesh();
            
            // make plan

            Vector3[] vertices = new Vector3[3]
            {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
            };
            
            mesh.vertices = vertices;

            int[] indices = new int[3]
            {
                2, 1, 0
            };
            
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
    
            return mesh;
        }
    }
}
