using System;
using UnityEngine;

namespace TP1
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshBehaviour : MonoBehaviour
    {
        public MeshType meshTypeToCreate = MeshType.Box;
        private MeshType previousMeshType;
        
        public Vector3 boxHalfSize = Vector3.one * 0.5f;

        private MeshFilter meshFilter;
        private IMeshMaker currentMeshMaker;

        void Start()
        {
            this.meshFilter = GetComponent<MeshFilter>();
            if (this.meshFilter == null)
            {
                Debug.LogError("MeshFilter component is missing.");
                return;
            }

            GenerateMesh();
        }

        private void GenerateMesh()
        {
            currentMeshMaker = meshTypeToCreate.CreateMeshMaker(boxHalfSize);
            
            if (currentMeshMaker == null) return;
            
            Mesh newMesh = currentMeshMaker.Create();
            if (newMesh != null)
            {
                if (meshFilter.sharedMesh != null &&
                    meshFilter.sharedMesh.name.StartsWith(meshTypeToCreate.ToString()))
                {
                    Destroy(meshFilter.sharedMesh);
                }

                meshFilter.mesh = newMesh;
                Debug.Log(meshTypeToCreate + " mesh created successfully.");
            }
            else
            {
                Debug.LogError("Failed to create mesh with " + meshTypeToCreate + " maker.");
            }
        }

        private void OnValidate()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }
            
            if (meshFilter == null || previousMeshType == meshTypeToCreate) return;

            GenerateMesh();
            previousMeshType = meshTypeToCreate;
        }
    }
}