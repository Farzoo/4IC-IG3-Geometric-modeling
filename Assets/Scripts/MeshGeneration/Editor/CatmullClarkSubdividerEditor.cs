using UnityEditor;
using UnityEngine;

namespace MeshGeneration.Editor
{
    [CustomEditor(typeof(CatmullClarkSubdivider)), CanEditMultipleObjects]
    public class CatmullClarkSubdividerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            CatmullClarkSubdivider subdivider = (CatmullClarkSubdivider)target;
            
            EditorGUILayout.Space();
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30
            };
            
                        
            if (!subdivider.IsSubdivided && GUILayout.Button("Save Original Mesh", buttonStyle))
            {
                Undo.RecordObject(subdivider.GetComponent<MeshFilter>(), "Save Original Mesh");
                subdivider.SaveOriginalMesh();
            }
            
            if (!subdivider.IsSubdivided && GUILayout.Button("Apply Catmull-Clark", buttonStyle))
            {
                Undo.RecordObject(subdivider.GetComponent<MeshFilter>(), "Apply Catmull-Clark");
                subdivider.ApplySubdivision();
            }
            
            if (subdivider.IsSubdivided && GUILayout.Button("Revert to Original", buttonStyle))
            {
                Undo.RecordObject(subdivider.GetComponent<MeshFilter>(), "Revert Mesh");
                subdivider.RevertToOriginal();
            }
        }
    }
}