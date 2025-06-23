using UnityEngine;

namespace TP1
{
    public class Chips : IMeshMaker
    {
        private readonly Vector3 halfSize;
        
        public Chips(Vector3 halfSize)
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
                // Face haut (y+)
                2, 7, 6, 2, 3, 7,
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