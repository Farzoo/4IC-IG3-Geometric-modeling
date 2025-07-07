using UnityEngine;

namespace TP1
{
    public class QuadBoxMaker : IMeshMaker
    {
        private readonly Vector3 halfSize;

        public QuadBoxMaker(Vector3 halfSize)
        {
            this.halfSize = halfSize;
        }

        public Mesh Create()
        {
            var mesh = new Mesh
            {
                name = "QuadBox"
            };
            
            Vector3[] vertices =
            {
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), // 0
                new Vector3( halfSize.x, -halfSize.y, -halfSize.z), // 1
                new Vector3( halfSize.x,  halfSize.y, -halfSize.z), // 2
                new Vector3(-halfSize.x,  halfSize.y, -halfSize.z), // 3
                new Vector3(-halfSize.x, -halfSize.y,  halfSize.z), // 4
                new Vector3( halfSize.x, -halfSize.y,  halfSize.z), // 5
                new Vector3( halfSize.x,  halfSize.y,  halfSize.z), // 6
                new Vector3(-halfSize.x,  halfSize.y,  halfSize.z)  // 7
            };
            mesh.vertices = vertices;
            
            int[] indices =
            {
                // Arrière (z-)
                0, 3, 2, 1,
                // Avant (z+)
                4, 5, 6, 7,
                // Bas (y-)
                0, 1, 5, 4,
                // Haut (y+)
                2, 3, 7, 6,
                // Gauche (x-)
                0, 4, 7, 3,
                // Droite (x+)
                1, 2, 6, 5
            };
            mesh.SetIndices(indices, MeshTopology.Quads, 0);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}