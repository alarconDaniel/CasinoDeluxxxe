using System.Collections;
using System.Text;
using UnityEngine;

public class RouletteInteractable : MonoBehaviour, IInteractable
{
    [Header("UI / Texto")]
    [SerializeField] private string prompt = "Girar ruleta";
    public int requiredLevel = 1;

    [Header("Costo y premios")]
    public int spinCostCoins = 25;
    public int redCoins = 10;
    public int blueCoins = 40;
    public int orangeXP = 80;

    public string Prompt => $"{prompt} (Req. Nivel {requiredLevel}) — {spinCostCoins} monedas";

    [Header("Qué gira")]
    public Transform wheelPivot;

    [Header("Puntero y segmentos")]
    public Transform pointer;        // Plano.002_3
    public Transform segmentsRoot;   // Valores_Ruleta_4

    [Header("Mensajes")]
    public float resultMessageSeconds = 2f;
    public bool showPrizeTableWhileSpinning = true;

    [TextArea(5, 5)]
    public string prizeTableText =
        "PREMIOS DE LA RULETA\n" +
        "AZUL:    +40 monedas\n" +
        "ROJO:    +10 monedas\n" +
        "NARANJA: +80 XP\n" +
        "BLANCO:  nada";

    [Header("Raycast")]
    public float rayStartOffset = 0.25f;
    public float rayDistance = 3f;
    public bool ensureRaycastColliders = true;

    [Header("Giro")]
    public Vector3 localAxis = new Vector3(0, 1, 0);
    public float spinDuration = 3.0f;
    public float startSpeed = 900f;

    [Header("Eje automático")]
    public bool autoDetectAxis = true;
    public bool invertDirection = false;

    [Header("Centro automático")]
    public bool autoDetectCenter = true;
    public bool recacheEachSpin = false;

    [Header("Audio (sonido al girar)")]
    public AudioSource spinAudioSource;
    public AudioClip spinClip;
    [Range(0f, 1f)] public float spinVolume = 1f;
    public bool loopWhileSpinning = true;
    public bool fadeOutOnStop = true;
    public float fadeOutTime = 0.15f;

    [Header("Luces (parpadeo durante el giro)")]
    public RouletteLightsBlink marqueeBlink;
    public bool blinkWhileSpinning = true;

    HUDController hud;

    bool spinning;
    bool axisResolved;
    Vector3 resolvedAxisLocal = Vector3.forward;

    bool centerResolved;
    Vector3 spinCenterWorld;
    Vector3 spinAxisWorld;

    enum Kind { White, Red, Blue, Orange, Unknown }

    void Awake()
    {
        hud = FindObjectOfType<HUDController>();

        if (segmentsRoot == null) segmentsRoot = wheelPivot;

        if (ensureRaycastColliders && segmentsRoot != null)
            EnsureRaycastMeshColliders(segmentsRoot);
    }

    public void Interact()
    {
        if (spinning) return;

        var gm = CasinoGameManager.Instance;
        if (gm == null) return;

        if (gm.level < requiredLevel)
        {
            hud?.ShowResultTimed($"Necesitas nivel {requiredLevel}", 2f);
            return;
        }

        if (wheelPivot == null || pointer == null || segmentsRoot == null)
        {
            hud?.ShowResultTimed("ERROR en la configuracion de la ruleta", 2f);
            return;
        }

        if (!gm.TrySpendCoins(spinCostCoins))
        {
            hud?.ShowResultTimed($"No tienes {spinCostCoins} monedas", 2f);
            return;
        }

        if (!axisResolved)
        {
            resolvedAxisLocal = ResolveAxisLocal();
            axisResolved = true;
        }

        if (!centerResolved || recacheEachSpin)
        {
            CacheCenterAndAxisWorld();
            centerResolved = true;
        }

        // Mostrar tabla arriba-derecha
        if (showPrizeTableWhileSpinning && hud != null)
        {
            hud.ShowPrizeTable(SanitizeText(prizeTableText));
        }

        if (blinkWhileSpinning && marqueeBlink != null)
            marqueeBlink.SetBlinking(true);

        StartSpinAudio();

        StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
    {
        spinning = true;

        float t = 0f;
        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / spinDuration);

            float speed = Mathf.Lerp(startSpeed, 0f, n * n);
            float delta = speed * Time.deltaTime;
            if (invertDirection) delta = -delta;

            wheelPivot.RotateAround(spinCenterWorld, spinAxisWorld, delta);
            yield return null;
        }

        spinning = false;

        StopSpinAudio();

        if (blinkWhileSpinning && marqueeBlink != null)
            marqueeBlink.SetBlinking(false);

        // Ocultar tabla
        hud?.HidePrizeTable();

        ResolveAndPay();
    }

    void ResolveAndPay()
    {
        var gm = CasinoGameManager.Instance;
        if (gm == null) return;

        Kind k = DetectKindUnderPointer(out string hitName);

        int coinsWin = 0;
        int xpWin = 0;

        if (k == Kind.Red) coinsWin = redCoins;
        else if (k == Kind.Blue) coinsWin = blueCoins;
        else if (k == Kind.Orange) xpWin = orangeXP;

        if (coinsWin > 0) gm.AddCoins(coinsWin);
        if (xpWin > 0) gm.AddXP(xpWin);

        string msg = BuildFriendlyResultMessage(k, coinsWin, xpWin, hitName);
        hud?.ShowResultTimed(SanitizeText(msg), resultMessageSeconds);

                // ---- STATS (Ruleta) ----
        var stats = CasinoStatsManager.Instance;
        if (stats != null)
        {
            bool won = (k == Kind.Red || k == Kind.Blue || k == Kind.Orange); // ganó si obtuvo premio (coins o XP)
            int payoutCoins = coinsWin; // naranja da XP, payoutCoins queda 0 (ok)
            stats.RecordRouletteSpin(spinCostCoins, -1, won, payoutCoins);
        }
    }

    string BuildFriendlyResultMessage(Kind k, int coinsWin, int xpWin, string hitName)
    {
        switch (k)
        {
            case Kind.Blue:   return $"¡AZUL! HAS GANADO +{coinsWin} MONEDAS";
            case Kind.Red:    return $"¡ROJO! HAS GANADO +{coinsWin} MONEDAS";
            case Kind.Orange: return $"¡NARANJA! HAS GANADO +{xpWin} XP";
            case Kind.White:  return "SIGUE INTENTANDO";
            default:          return "SIGUE INTENTANDO";
        }
    }

    // Quita emojis y símbolos raros automáticamente (evita los recuadros)
    static string SanitizeText(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (char ch in s)
        {
            // permite ASCII + Latin-1 (acentos), saltos de línea
            if (ch == '\n' || ch == '\r' || ch == '\t' || ch <= 0x00FF)
                sb.Append(ch);
        }
        return sb.ToString();
    }

    // ---------- Detección por raycast + nombre ----------
    Kind DetectKindUnderPointer(out string hitName)
    {
        hitName = "";

        Vector3 axis = spinAxisWorld.sqrMagnitude > 0.0001f ? spinAxisWorld.normalized : wheelPivot.forward;
        Vector3 origin = pointer.position + axis * rayStartOffset;

        RaycastHit[] hits = Physics.RaycastAll(origin, -axis, rayDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            hits = Physics.RaycastAll(origin, axis, rayDistance, ~0, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        Transform bestT = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            Transform t = h.collider.transform;
            if (!IsChildOf(t, segmentsRoot)) continue;

            if (h.distance < best)
            {
                best = h.distance;
                bestT = t;
            }
        }

        if (bestT == null) return Kind.Unknown;

        // Sube hasta el hijo directo tipo Color_03_blue
        Transform p = bestT;
        while (p != null && p.parent != null && p.parent != segmentsRoot)
            p = p.parent;

        hitName = p != null ? p.name : bestT.name;

        string n = hitName.ToLowerInvariant();
        if (n.Contains("white")) return Kind.White;
        if (n.Contains("red")) return Kind.Red;
        if (n.Contains("blue")) return Kind.Blue;
        if (n.Contains("orange")) return Kind.Orange;

        return Kind.Unknown;
    }

    static bool IsChildOf(Transform t, Transform root)
    {
        if (t == null || root == null) return false;
        Transform p = t;
        while (p != null)
        {
            if (p == root) return true;
            p = p.parent;
        }
        return false;
    }

    // ---------- Centro/Eje ----------
    void CacheCenterAndAxisWorld()
    {
        Vector3 axisLocal = (resolvedAxisLocal.sqrMagnitude < 0.0001f) ? Vector3.up : resolvedAxisLocal.normalized;
        spinAxisWorld = wheelPivot.TransformDirection(axisLocal).normalized;

        spinCenterWorld = wheelPivot.position;

        if (!autoDetectCenter) return;

        var renderers = wheelPivot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        spinCenterWorld = b.center;
    }

    Vector3 ResolveAxisLocal()
    {
        if (!autoDetectAxis)
            return (localAxis.sqrMagnitude < 0.0001f) ? Vector3.up : localAxis.normalized;

        var renderers = wheelPivot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return (localAxis.sqrMagnitude < 0.0001f) ? Vector3.up : localAxis.normalized;

        bool hasAnyPoint = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (var r in renderers)
        {
            Bounds wb = r.bounds;
            Vector3 c = wb.center;
            Vector3 e = wb.extents;

            Vector3[] corners =
            {
                c + new Vector3( e.x,  e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x, -e.y, -e.z),
            };

            foreach (var pWorld in corners)
            {
                Vector3 pLocal = wheelPivot.InverseTransformPoint(pWorld);
                if (!hasAnyPoint)
                {
                    localBounds = new Bounds(pLocal, Vector3.zero);
                    hasAnyPoint = true;
                }
                else localBounds.Encapsulate(pLocal);
            }
        }

        Vector3 size = localBounds.size;
        if (size.x <= size.y && size.x <= size.z) return Vector3.right;
        if (size.y <= size.x && size.y <= size.z) return Vector3.up;
        return Vector3.forward;
    }

    // ---------- Colliders para raycast ----------
    void EnsureRaycastMeshColliders(Transform root)
    {
        var mfs = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in mfs)
        {
            if (mf == null || mf.sharedMesh == null) continue;

            var mc = mf.GetComponent<MeshCollider>();
            if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();

            mc.sharedMesh = mf.sharedMesh;
            mc.convex = false;
            mc.isTrigger = false;
        }
    }

    // ---------- Audio helpers ----------
    void StartSpinAudio()
    {
        if (spinAudioSource == null || spinClip == null) return;

        spinAudioSource.clip = spinClip;
        spinAudioSource.volume = spinVolume;
        spinAudioSource.loop = loopWhileSpinning;

        if (!spinAudioSource.isPlaying)
            spinAudioSource.Play();
    }

    void StopSpinAudio()
    {
        if (spinAudioSource == null) return;
        if (!spinAudioSource.isPlaying) return;

        if (fadeOutOnStop && fadeOutTime > 0f)
            StartCoroutine(FadeOutAndStop(spinAudioSource, fadeOutTime));
        else
            spinAudioSource.Stop();
    }

    IEnumerator FadeOutAndStop(AudioSource src, float time)
    {
        float startVol = src.volume;
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / time);
            yield return null;
        }

        src.Stop();
        src.volume = startVol;
    }
}