using UnityEngine;
using UnityEngine.Rendering;

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
    
            this.mesh = this.MakeQuadLowLevel();
            
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

        private Mesh MakeQuadLowLevel()
        {
            Mesh.MeshDataArray meshData = Mesh.AllocateWritableMeshData(1);
            
            Mesh.MeshData meshData0 = meshData[0];
            
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, 1)
            };

            int[] indices = new int[6]
            {
                2, 1, 0,
                3, 2, 0
            };

            meshData0.SetVertexBufferParams(vertices.Length,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3));

            meshData0.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);

            meshData0.GetVertexData<Vector3>().CopyFrom(vertices);
            meshData0.GetIndexData<int>().CopyFrom(indices);
            
            meshData0.subMeshCount = 1;
            meshData0.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length, MeshTopology.Triangles));
            
            Mesh mesh = new Mesh();
            
            Mesh.ApplyAndDisposeWritableMeshData(meshData, mesh);
            
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
