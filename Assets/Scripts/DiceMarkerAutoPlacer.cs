using UnityEngine;

public class DiceMarkerAutoPlacer : MonoBehaviour
{
    public float outwardOffset = 0.02f;

    [ContextMenu("Create Axis Markers (Centered)")]
    void CreateAxisMarkers()
    {
        var bc = GetComponent<BoxCollider>();
        if (bc == null)
        {
            Debug.LogWarning("Ponle BoxCollider al dado para usar esto.");
            return;
        }

        Vector3 size = bc.size;
        Vector3 c = bc.center;

        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float hz = size.z * 0.5f;
        float o = outwardOffset;

        CreateOrMove("PosX", c + new Vector3( hx + o, 0, 0));
        CreateOrMove("NegX", c + new Vector3(-hx - o, 0, 0));
        CreateOrMove("PosY", c + new Vector3(0,  hy + o, 0));
        CreateOrMove("NegY", c + new Vector3(0, -hy - o, 0));
        CreateOrMove("PosZ", c + new Vector3(0, 0,  hz + o));
        CreateOrMove("NegZ", c + new Vector3(0, 0, -hz - o));

        Debug.Log("Markers recreados usando BoxCollider.center ✅");
    }

    void CreateOrMove(string name, Vector3 localPos)
    {
        Transform t = transform.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            t = go.transform;
        }

        t.localPosition = localPos;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }
}