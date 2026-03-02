using System;
using System.Collections;
using UnityEngine;

public class DiceRoller : MonoBehaviour
{
    public BoxCollider throwArea;
    public DiceActor[] dice;

    [Header("Throw feel")]
    public float upForce = 6.5f;
    public float sideForce = 2.5f;
    public float torque = 12f;

    [Header("Settle detection")]
    public float minRollTime = 0.6f;
    public float velThreshold = 0.08f;
    public float angThreshold = 0.08f;
    public float stableTimeNeeded = 0.35f;

    public IEnumerator Roll(bool[] holdMask, Action<int[]> onDone)
    {
        if (throwArea == null || dice == null || dice.Length == 0)
        {
            Debug.LogWarning("DiceRoller: falta throwArea o dice[].");
            onDone?.Invoke(Array.Empty<int>());
            yield break;
        }

        if (holdMask == null || holdMask.Length != dice.Length)
            holdMask = new bool[dice.Length];

        Bounds b = throwArea.bounds;

        // 1) Reset + launch
        for (int i = 0; i < dice.Length; i++)
        {
            if (dice[i] == null || dice[i].rb == null) continue;
            if (holdMask[i]) continue;

            Vector3 p = new Vector3(
                UnityEngine.Random.Range(b.min.x, b.max.x),
                b.max.y + 0.15f,
                UnityEngine.Random.Range(b.min.z, b.max.z)
            );

            Quaternion r = UnityEngine.Random.rotation;
            dice[i].HardReset(p, r);

            Vector3 force = Vector3.up * upForce +
                            new Vector3(UnityEngine.Random.Range(-sideForce, sideForce), 0f, UnityEngine.Random.Range(-sideForce, sideForce));

            dice[i].rb.AddForce(force, ForceMode.Impulse);

            Vector3 tq = new Vector3(
                UnityEngine.Random.Range(-torque, torque),
                UnityEngine.Random.Range(-torque, torque),
                UnityEngine.Random.Range(-torque, torque)
            );
            dice[i].rb.AddTorque(tq, ForceMode.Impulse);
        }

        // 2) Wait minimum time
        float t = 0f;
        while (t < minRollTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // 3) Wait until stable for stableTimeNeeded
        float stableT = 0f;
        while (stableT < stableTimeNeeded)
        {
            bool allStable = true;

            for (int i = 0; i < dice.Length; i++)
            {
                if (dice[i] == null || dice[i].rb == null) continue;

                // Si está hold, lo ignoramos (ya estaba quieto)
                if (holdMask[i]) continue;

                if (dice[i].rb.linearVelocity.magnitude > velThreshold ||
                    dice[i].rb.angularVelocity.magnitude > angThreshold)
                {
                    allStable = false;
                    break;
                }
            }

            stableT = allStable ? (stableT + Time.deltaTime) : 0f;
            yield return null;
        }

        // 4) Read values
        int[] values = new int[dice.Length];
        for (int i = 0; i < dice.Length; i++)
        {
            values[i] = (dice[i] != null) ? dice[i].GetTopValue() : 0;
        }

        onDone?.Invoke(values);
    }
}