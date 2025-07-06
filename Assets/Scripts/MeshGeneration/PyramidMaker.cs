using UnityEngine;

namespace TP1
{
    public class PyramidMaker : IMeshMaker
    {
        public PyramidMaker()
        {
        }
        
        public Mesh Create()
        {
            // On va créer une "pyramide" mais sur un plan XY mis à plat
            var mesh = new Mesh();
            
            Vector3[] vertices = {
                new Vector3(0, 0 , 0),
                new Vector3(0.5f, 0, 1),
                new Vector3(1, 0, 0),
                new Vector3(-0.5f, 0, 1),
                new Vector3(0, 0, 2),
                new Vector3(-1, 0, 0),
                
                //new Vector3(0.25f, 1, -1),
                //new Vector3(1f, 1, -1),
            };
            
            mesh.vertices = vertices;
            
            int[] indices = {
                0, 1, 2,
                1, 3, 4,
                0, 5, 3,
                0, 3, 1,
                
                // Anormal
                //6, 0, 7
            };
            
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
}