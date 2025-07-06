using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
public class MeshDebugger : MonoBehaviour
{
    public bool showVertices = true;
    public bool showEdges = true;
    public bool showVertexIds = true;
    public bool showEdgeIds = true;

    public float vertexRadius = 0.02f;
    public float arrowHeadLength = 0.04f;
    public float arrowHeadAngle = 25f;

    public Vector3 labelOffset = Vector3.up * 0.02f;
    
    GUIStyle _style;
    readonly HashSet<ulong> already = new();
    readonly Dictionary<ulong,int> edgeIndexOf = new();
    int edgeCounter;

#if UNITY_EDITOR
    void OnEnable()
    {
        InitStyle();
    }
#endif

    void InitStyle()
    {
        _style = new GUIStyle
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter
        };
        _style.normal.textColor = Color.black;
        _style.fontStyle = FontStyle.Bold;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!this.isActiveAndEnabled)
            return;
        
        InitStyle();
        
        var mf = GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] v = mesh.vertices;
        int[] t = mesh.triangles;
        Matrix4x4 M = transform.localToWorldMatrix;
        
        if (showVertices)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < v.Length; ++i)
            {
                Vector3 pw = M.MultiplyPoint3x4(v[i]);
                Gizmos.DrawSphere(pw, vertexRadius);

                if (showVertexIds)
                    Handles.Label(pw + labelOffset, i.ToString(), _style);
            }
        }

        if (!showEdges) return;

        Gizmos.color = Color.black;
        already.Clear();
        edgeIndexOf.Clear();
        edgeCounter = 0;

        for (int i = 0; i < t.Length; i += 3)
        {
            DrawEdge(t[i],   t[i+1], v, M);
            DrawEdge(t[i+1], t[i+2], v, M);
            DrawEdge(t[i+2], t[i],   v, M);
        }
    }

    void DrawEdge(int a, int b, Vector3[] v, Matrix4x4 M)
    {
        ulong key = MakeKey(a, b);
        if (!already.Add(key)) return;

        int id = edgeIndexOf.TryGetValue(key, out int cached) ? cached
                                                              : (edgeIndexOf[key] = edgeCounter++);

        Vector3 p0 = M.MultiplyPoint3x4(v[a]);
        Vector3 p1 = M.MultiplyPoint3x4(v[b]);

        DrawLine(p0, p1, 3f, Color.black);

        Vector3 dir  = (p0 - p1).normalized;
        Vector3 mid  = (p0 + p1) * 0.5f;
        Vector3 right = Quaternion.Euler(0,  arrowHeadAngle, 0) * dir;
        Vector3 left  = Quaternion.Euler(0, -arrowHeadAngle, 0) * dir;
        DrawLine(mid, mid + right * arrowHeadLength, 3f, Color.black);
        DrawLine(mid, mid + left  * arrowHeadLength, 3f, Color.black);

        if (showEdgeIds)
            Handles.Label(mid + labelOffset, id.ToString(), _style);
    }

    static void DrawLine(Vector3 p0, Vector3 p1, float thickness, Color color)
    {
        Handles.DrawBezier(p0, p1, p0, p1, color, null, thickness);
    }
#endif
    
    static ulong MakeKey(int i, int j)
    {
        uint a = (uint)Mathf.Min(i, j);
        uint b = (uint)Mathf.Max(i, j);
        return ((ulong)a << 32) | b;
    }
}
