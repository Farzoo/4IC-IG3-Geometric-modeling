using UnityEngine;
using UnityEditor;

namespace MeshGeneration.Editor
{
    [CustomEditor(typeof(MeshBehaviour)), CanEditMultipleObjects]
    public class MeshBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            MeshBehaviour meshBehaviour = (MeshBehaviour)target;
            
            if (GUILayout.Button("Refresh Mesh", GUILayout.Height(30)))
            {
                meshBehaviour.GenerateMesh();
                EditorUtility.SetDirty(meshBehaviour);
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("Utilisez le bouton 'Refresh Mesh' pour régénérer le mesh manuellement.", MessageType.Info);
        }
    }
}

