using UnityEngine;

namespace MeshGeneration.SimpleMesh
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
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y,  halfSize.z) 
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