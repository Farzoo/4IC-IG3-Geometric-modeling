using System;
using MeshGeneration.SimpleMesh;
using MeshGeneration.WingedEdge;
using UnityEngine;

namespace MeshGeneration
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshBehaviour : MonoBehaviour
    {
        public MeshType meshTypeToCreate = MeshType.Box;
        private MeshType previousMeshType;
        
        public Vector3 boxHalfSize = Vector3.one * 0.5f;
        
        public WingedEdgeDebugger wingedEdgeDebugger;

        private MeshFilter meshFilter;
        private IMeshMaker currentMeshMaker;

        private void Init()
        {
            this.meshFilter = GetComponent<MeshFilter>();
            if (this.meshFilter == null)
            {
                Debug.LogError("MeshFilter component is missing.");
                return;
            }
            
            GenerateMesh();
        }
        
        void Start()
        {
            Init();
        }

        public void GenerateMesh()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    Debug.LogError("MeshFilter component is missing.");
                    return;
                }
            }
            
            currentMeshMaker = meshTypeToCreate.CreateMeshMaker(boxHalfSize);
            
            if (currentMeshMaker == null) return;
            
            Mesh newMesh = currentMeshMaker.Create();
            if (newMesh == null)
            {
                Debug.LogError("Failed to create mesh with " + meshTypeToCreate + " maker.");
                return;
            }

            if (meshFilter.sharedMesh != null &&
                meshFilter.sharedMesh.name.StartsWith(meshTypeToCreate.ToString()))
            {
                DestroyImmediate(meshFilter.sharedMesh);
            }

            meshFilter.sharedMesh = newMesh;
            Debug.Log(meshTypeToCreate + " mesh created successfully.");
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