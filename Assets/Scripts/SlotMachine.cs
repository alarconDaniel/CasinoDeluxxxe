using System.Collections;
using TMPro;
using UnityEngine;

public class SlotMachine : CasinoMachineBase
{
    [System.Serializable]
    public class SymbolWeight
    {
        public string symbolName;
        public int weight = 1;
        public int payoutMultiplier = 2;
    }

    [Header("Symbols")]
    public SymbolWeight[] symbols;

    [Header("Reels (actual cylinder transforms)")]
    public Transform[] reels; // Cylinder.001, .002, .003

    [Header("Spin Axis (pick one)")]
    public Vector3 localSpinAxis = Vector3.right; // X

    [Header("Symbol angles (calibration)")]
    public float angleSeven = 0f;
    public float angleClover = 90f;
    public float angleCherry = 180f;
    public float angleDiamond = 270f;
    public float angleOffset = 0f;     // si todo cae corrido, ajusta aquí
    public bool reverseSpin = false;   // si va al revés

    [Header("Spin")]
    public float spinSpeedDegPerSec = 900f;
    public float spinTime = 1.2f;
    public float stopTime = 0.6f;
    public int extraFullSpins = 3;

    [Header("Lever")]
    public Transform lever;                 // arrastra el objeto de la palanca (palo o grupo)
    public Vector3 leverAxis = Vector3.forward; // prueba Z (forward). Si no, X o Y.
    public float leverPullDelta = -130f;    // -90 -> -220 = -130 grados
    public float leverPullTime = 0.12f;     // bajada rápida
    public float leverReturnTime = 1.8f;    // regreso lento (se ajusta automáticamente si quieres)

    [Header("UI (TextMeshPro 3D on panel)")]
    public TextMeshPro resultText;

    [Header("Bulbs (NO parenting needed)")]
    public Renderer[] bulbs; // arrastra aquí todas las bombillitas (arco + panel)
    public float winBlinkDuration = 2.0f;
    public float winBlinkHz = 14f;

    bool isSpinning;

    // Reel rotation is tracked internally (no Euler chaos)
    Quaternion[] reelBaseRot;
    float[] reelAngle;
    Vector3 spinAxisUnit;
    int spinAxisIndex;

    // Lever
    Quaternion leverRestRot;
    Vector3 leverAxisUnit;

    // Lever state (fix)
    int leverAxisIdx;
    Vector3 leverRestEuler;
    float leverRestSignedAngle;
    float leverT = 0f; // 0..1, se guarda entre animaciones

    // Bulbs restore
    MaterialPropertyBlock mpb;
    int emissionId;
    bool[] bulbOriginalEnabled;
    bool[] bulbHasEmission;
    Color[] bulbOriginalEmission;

    // Audio

    [Header("Audio")]
    public AudioClip spinClip;        // Arrastra: slot.mp3
    public AudioClip reelStopClip;    // (Opcional) un click corto (si luego lo recortas)
    public AudioClip jackpotClip;     // Arrastra: jackpot.mp3

    [Range(0f, 1f)] public float spinVolume = 0.7f;
    [Range(0f, 1f)] public float stopClickVolume = 0.9f;
    [Range(0f, 1f)] public float jackpotVolume = 1.0f;

    AudioSource spinSource;
    AudioSource sfxSource;

    public float spinFadeOut = 0.08f; // fade out chiquito pa que no corte feo

    void Awake()
    {
        // --- axis setup (snap to X/Y/Z based on strongest component) ---
        spinAxisIndex = AxisIndex(localSpinAxis);
        spinAxisUnit = AxisVector(spinAxisIndex);
        leverAxisUnit = AxisVector(AxisIndex(leverAxis));

        // --- cache reel base rotations ---
        if (reels != null && reels.Length > 0)
        {
            reelBaseRot = new Quaternion[reels.Length];
            reelAngle = new float[reels.Length];
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] == null) continue;
                reelBaseRot[i] = reels[i].localRotation;
                reelAngle[i] = 0f;
            }
        }

        // --- lever rest rotation ---
        if (lever != null)
            leverRestRot = lever.localRotation;

        if (lever != null)
        {
            leverAxisIdx = AxisIndex(leverAxis);           // 0=X,1=Y,2=Z
            leverRestEuler = lever.localEulerAngles;       // guarda los euler originales
            float restAxisAngle =
                (leverAxisIdx == 0) ? leverRestEuler.x :
                (leverAxisIdx == 1) ? leverRestEuler.y :
                                      leverRestEuler.z;
            leverRestSignedAngle = Mathf.DeltaAngle(0f, restAxisAngle); // lo vuelve -180..180
            leverT = 0f;
        }

        // --- bulbs cache original state ---
        mpb = new MaterialPropertyBlock();
        emissionId = Shader.PropertyToID("_EmissionColor");

        if (bulbs != null && bulbs.Length > 0)
        {
            bulbOriginalEnabled = new bool[bulbs.Length];
            bulbHasEmission = new bool[bulbs.Length];
            bulbOriginalEmission = new Color[bulbs.Length];

            for (int i = 0; i < bulbs.Length; i++)
            {
                var r = bulbs[i];
                if (r == null) continue;

                bulbOriginalEnabled[i] = r.enabled;

                var mat = r.sharedMaterial;
                bool has = (mat != null && mat.HasProperty(emissionId));
                bulbHasEmission[i] = has;

                if (has)
                {
                    // read original from material (baseline)
                    bulbOriginalEmission[i] = mat.GetColor(emissionId);
                    mat.EnableKeyword("_EMISSION");
                }
                else
                {
                    bulbOriginalEmission[i] = Color.black;
                }
            }
        }

        // --- audio sources (2D) ---
        spinSource = gameObject.AddComponent<AudioSource>();
        spinSource.playOnAwake = false;
        spinSource.loop = false;
        spinSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    protected override void PlayOnce()
    {
        if (isSpinning) return;

        if (reels == null || reels.Length < 3)
        {
            Debug.LogError("SlotMachine: Asigna 3 reels en el Inspector.");
            return;
        }

        var gm = CasinoGameManager.Instance;
        int bet = gm.bet;

        if (!gm.TrySpendCoins(bet))
        {
            SetLocalText("No tienes monedas.");
            return;
        }

        var a = PickSymbol();
        var b = PickSymbol();
        var c = PickSymbol();

        StartCoroutine(SpinAndResolve(bet, a, b, c));
    }

    IEnumerator SpinAndResolve(int bet, SymbolWeight a, SymbolWeight b, SymbolWeight c)
    {
        isSpinning = true;

        StartSpinSfx();

        SetLocalText("Girando...");

        // Lever: pull down quick
        Coroutine leverReturn = null;
        if (lever != null)
        {
            yield return AnimateLever(1f, leverPullTime); // to pulled (t=1)
            // start slow return while reels spin
            float totalSpinDuration = spinTime + stopTime * 3f;
            float returnDuration = Mathf.Max(leverReturnTime, totalSpinDuration);
            leverReturn = StartCoroutine(AnimateLever(0f, returnDuration)); // back to rest (t=0)
        }

        // Free spin
        float dir = reverseSpin ? -1f : 1f;
        float t = 0f;
        while (t < spinTime)
        {
            t += Time.deltaTime;
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] == null) continue;
                reelAngle[i] += dir * spinSpeedDegPerSec * Time.deltaTime;
                ApplyReelRotation(i);
            }
            yield return null;
        }

        // Stop one by one
        yield return StopReelToSymbol(0, a.symbolName);
        yield return StopReelToSymbol(1, b.symbolName);
        yield return StopReelToSymbol(2, c.symbolName);

        PlayStopClick();

        // Make sure lever returns to rest
        if (lever != null)
        {
            if (leverReturn != null) StopCoroutine(leverReturn);
            yield return AnimateLever(0f, 0.25f);
        }

        // Resolve win
        var gm = CasinoGameManager.Instance;

        string na = Norm(a.symbolName);
        string nb = Norm(b.symbolName);
        string nc = Norm(c.symbolName);

        bool triple = (na == nb && nb == nc);

        int payout = 0;
        if (triple) payout = bet * a.payoutMultiplier;

        // --- registra stats (slots) ---
        var stats = CasinoStatsManager.Instance;
        if (stats != null)
        {
            // guarda símbolos tal cual (o normalizados). Yo los mando normalizados pa que se vea bonito en CSV.
            stats.RecordSlotsSpin(bet, na, nb, nc, triple, payout);
        }

        if (triple)
        {
            gm.AddCoins(payout);
            gm.AddXP(bet);

            SetLocalText("¡GANASTE!");

            StopSpinSfx();
            PlayJackpot();

            if (bulbs != null && bulbs.Length > 0)
                yield return BlinkBulbsRestore(winBlinkDuration, winBlinkHz);
        }
        else
        {
            gm.AddXP(bet);
            SetLocalText("¡Una vez más!");
        }

        yield return StopSpinSfx();

        isSpinning = false;
    }

    IEnumerator StopReelToSymbol(int reelIndex, string symbolName)
    {
        if (reels[reelIndex] == null) yield break;

        float target = AngleForSymbol(symbolName) + angleOffset;
        target = Mathf.Repeat(target, 360f);

        float start = Mathf.Repeat(reelAngle[reelIndex], 360f);

        float delta = Mathf.Repeat(target - start, 360f);
        float end = reelAngle[reelIndex] + extraFullSpins * 360f + (reverseSpin ? -delta : delta);

        float elapsed = 0f;
        float from = reelAngle[reelIndex];

        while (elapsed < stopTime)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / stopTime);
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            reelAngle[reelIndex] = Mathf.Lerp(from, end, eased);
            ApplyReelRotation(reelIndex);
            yield return null;
        }

        // snap exact to target keeping continuity
        float cycles = Mathf.Floor(reelAngle[reelIndex] / 360f) * 360f;
        reelAngle[reelIndex] = cycles + target * (reverseSpin ? -1f : 1f);
        ApplyReelRotation(reelIndex);
    }

    float AngleForSymbol(string nameRaw)
    {
        string name = Norm(nameRaw);

        if (name == "seven") return angleSeven;
        if (name == "clover") return angleClover;
        if (name == "cherry") return angleCherry;
        if (name == "diamond") return angleDiamond;

        // fallback
        return angleSeven;
    }

    IEnumerator AnimateLever(float targetT, float duration)
    {
        if (lever == null) yield break;

        float startT = leverT;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);

            leverT = Mathf.Lerp(startT, targetT, eased);

            // rest (-90) + delta (-130*t) => -220 cuando t=1
            float signedAngle = leverRestSignedAngle + leverPullDelta * leverT;

            // Unity guarda eulers 0..360
            float wrapped = Mathf.Repeat(signedAngle, 360f);

            var e = leverRestEuler;
            if (leverAxisIdx == 0) e.x = wrapped;
            else if (leverAxisIdx == 1) e.y = wrapped;
            else e.z = wrapped;

            lever.localEulerAngles = e;
            yield return null;
        }

        leverT = targetT;

        float finalSigned = leverRestSignedAngle + leverPullDelta * leverT;
        float finalWrapped = Mathf.Repeat(finalSigned, 360f);

        var ef = leverRestEuler;
        if (leverAxisIdx == 0) ef.x = finalWrapped;
        else if (leverAxisIdx == 1) ef.y = finalWrapped;
        else ef.z = finalWrapped;

        lever.localEulerAngles = ef;
    }

    float CurrentLeverT()
    {
        // Best-effort: we assume lever currently between rest and pulled.
        // If you never rotate lever elsewhere, this works fine.
        return 0f;
    }

    void ApplyReelRotation(int i)
    {
        // base * spin about chosen axis
        reels[i].localRotation = reelBaseRot[i] * Quaternion.AngleAxis(reelAngle[i], spinAxisUnit);
    }

    IEnumerator BlinkBulbsRestore(float duration, float hz)
    {
        float start = Time.time;
        bool on = true;

        while (Time.time - start < duration)
        {
            on = !on;

            for (int i = 0; i < bulbs.Length; i++)
            {
                var r = bulbs[i];
                if (r == null) continue;

                if (bulbHasEmission[i])
                {
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(emissionId, on ? (Color.white * 8f) : Color.black);
                    r.SetPropertyBlock(mpb);
                }
                else
                {
                    r.enabled = on;
                }
            }

            yield return new WaitForSeconds(1f / hz);
        }

        // RESTORE original state
        for (int i = 0; i < bulbs.Length; i++)
        {
            var r = bulbs[i];
            if (r == null) continue;

            if (bulbHasEmission[i])
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor(emissionId, bulbOriginalEmission[i]);
                r.SetPropertyBlock(mpb);
            }
            r.enabled = bulbOriginalEnabled[i];
        }
    }

    void SetLocalText(string msg)
    {
        if (resultText != null) resultText.text = msg;
    }

    private SymbolWeight PickSymbol()
    {
        int total = 0;
        foreach (var s in symbols) total += Mathf.Max(0, s.weight);

        int r = Random.Range(0, total);
        int acc = 0;

        foreach (var s in symbols)
        {
            acc += Mathf.Max(0, s.weight);
            if (r < acc) return s;
        }
        return symbols[0];
    }

    static string Norm(string s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();

    static int AxisIndex(Vector3 axis)
    {
        axis = axis.normalized;
        float ax = Mathf.Abs(axis.x);
        float ay = Mathf.Abs(axis.y);
        float az = Mathf.Abs(axis.z);
        if (ax >= ay && ax >= az) return 0;
        if (ay >= az) return 1;
        return 2;
    }

    static Vector3 AxisVector(int idx)
    {
        if (idx == 0) return Vector3.right;
        if (idx == 1) return Vector3.up;
        return Vector3.forward;
    }

    void StartSpinSfx()
    {
        if (spinClip == null || spinSource == null) return;

        spinSource.Stop();
        spinSource.clip = spinClip;
        spinSource.volume = spinVolume;
        spinSource.time = 0f;
        spinSource.Play();
    }

    IEnumerator StopSpinSfx()
    {
        if (spinSource == null) yield break;
        if (!spinSource.isPlaying) yield break;

        float startVol = spinSource.volume;
        float t = 0f;

        while (t < spinFadeOut)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / spinFadeOut);
            spinSource.volume = Mathf.Lerp(startVol, 0f, k);
            yield return null;
        }

        spinSource.Stop();
        spinSource.volume = startVol;
    }

    void PlayStopClick()
    {
        if (reelStopClip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(reelStopClip, stopClickVolume);
    }

    void PlayJackpot()
    {
        if (jackpotClip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(jackpotClip, jackpotVolume);
    }
}