using UnityEngine;

namespace MeshGeneration.SimpleMesh
{
    public class BoxMaker : IMeshMaker
    {
        private readonly Vector3 halfSize;
        
        public BoxMaker(Vector3 halfSize)
        {
            this.halfSize = halfSize;
        }
        
        public Mesh Create()
        {
            var mesh = new Mesh();

            Vector3[] vertices = {
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3(halfSize.x, halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x, halfSize.y, -halfSize.z),

                new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
                new Vector3(halfSize.x, -halfSize.y, halfSize.z),
                new Vector3(halfSize.x, halfSize.y, halfSize.z),
                new Vector3(-halfSize.x, halfSize.y, halfSize.z)
            };
            
            mesh.vertices = vertices;
            
            int[] indices = {
                // Face arrière (z-)
                0, 2, 1, 0, 3, 2,
                // Face avant (z+)
                4, 5, 6, 4, 6, 7,
                // Face bas (y-)
                0, 1, 5, 0, 5, 4,
                // Face haut (y+)
                2, 3, 7, 2, 7, 6,
                // Face gauche (x-)
                0, 4, 7, 0, 7, 3,
                // Face droite (x+)
                1, 2, 6, 1, 6, 5 
            };
            
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
}