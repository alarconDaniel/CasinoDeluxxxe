using UnityEngine;

public class DiceActor : MonoBehaviour
{
    public Rigidbody rb;

    [Tooltip("6 markers: index 0->cara 1, index 5->cara 6")]
    public Transform[] faceMarkers = new Transform[6];

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    public void HardReset(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            rb.WakeUp();
        }
    }

    public int GetTopValue()
    {
        // El marker más alto en Y = cara de arriba
        int best = 0;
        float bestY = float.NegativeInfinity;

        for (int i = 0; i < faceMarkers.Length; i++)
        {
            var m = faceMarkers[i];
            if (m == null) continue;

            float y = m.position.y;
            if (y > bestY)
            {
                bestY = y;
                best = i;
            }
        }

        return best + 1; // index 0 => valor 1
    }
}