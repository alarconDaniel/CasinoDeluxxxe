using System.Collections;
using UnityEngine;

public class RouletteInteractable : MonoBehaviour, IInteractable
{
    [Header("Texto HUD")]
    [SerializeField] private string prompt = "Girar ruleta";
    public string Prompt => prompt;

    [Header("Qué gira")]
    public Transform wheelPivot;

    [Header("Giro")]
    public Vector3 localAxis = new Vector3(0, 1, 0); // (se mantiene, pero ahora es fallback)
    public float spinDuration = 3.0f;
    public float startSpeed = 900f; // grados/seg al inicio

    // --- NUEVO: eje automático (solución fija) ---
    [Header("Eje automático (recomendado)")]
    public bool autoDetectAxis = true;      // déjalo en true
    public bool invertDirection = false;    // si gira al revés, ponlo en true

    // --- NUEVO: centro automático (para que gire sobre su centro) ---
    [Header("Centro automático (para girar sobre el centro)")]
    public bool autoDetectCenter = true;    // déjalo en true
    public bool recacheEachSpin = false;    // si cambias cosas en runtime, ponlo true

    // --- NUEVO: audio ---
    [Header("Audio (sonido al girar)")]
    public AudioSource spinAudioSource;     // arrastra aquí un AudioSource
    public AudioClip spinClip;              // arrastra aquí tu sonido .wav/.mp3
    [Range(0f, 1f)] public float spinVolume = 1f;
    public bool loopWhileSpinning = true;
    public bool fadeOutOnStop = true;
    public float fadeOutTime = 0.15f;

    // --- NUEVO: luces (parpadeo durante el giro) ---
    [Header("Luces (parpadeo durante el giro)")]
    public RouletteLightsBlink marqueeBlink; // arrastra aquí el componente de las luces
    public bool blinkWhileSpinning = true;

    private bool spinning;
    private bool axisResolved;
    private Vector3 resolvedAxisLocal = Vector3.forward;

    // cache de centro/eje en mundo
    private bool centerResolved;
    private Vector3 spinCenterWorld;
    private Vector3 spinAxisWorld;

    public void Interact()
    {
        if (spinning) return;
        if (wheelPivot == null)
        {
            Debug.LogError("RouletteInteractable: wheelPivot no asignado.");
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

        // ✅ LUCES: iniciar parpadeo cuando empieza a girar
        if (blinkWhileSpinning && marqueeBlink != null)
            marqueeBlink.SetBlinking(true);

        // ✅ AUDIO: iniciar sonido cuando empieza a girar
        StartSpinAudio();

        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        spinning = true;

        float t = 0f;
        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / spinDuration);

            // Frenado suave (ease out)
            float speed = Mathf.Lerp(startSpeed, 0f, n * n);
            float delta = speed * Time.deltaTime;

            if (invertDirection) delta = -delta;

            // ✅ CLAVE: girar alrededor del centro real, con el eje correcto
            wheelPivot.RotateAround(spinCenterWorld, spinAxisWorld, delta);

            yield return null;
        }

        spinning = false;

        // ✅ AUDIO: detener sonido al terminar
        StopSpinAudio();

        // ✅ LUCES: detener parpadeo al terminar
        if (blinkWhileSpinning && marqueeBlink != null)
            marqueeBlink.SetBlinking(false);
    }

    private void CacheCenterAndAxisWorld()
    {
        // eje local -> eje world
        Vector3 axisLocal = (resolvedAxisLocal.sqrMagnitude < 0.0001f) ? Vector3.up : resolvedAxisLocal.normalized;
        spinAxisWorld = wheelPivot.TransformDirection(axisLocal).normalized;

        // centro world
        spinCenterWorld = wheelPivot.position;

        if (!autoDetectCenter) return;

        var renderers = wheelPivot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        spinCenterWorld = b.center;
    }

    // Calcula el eje "normal" del objeto: el de menor grosor en espacio local del wheelPivot.
    // Esto evita el problema de "moneda".
    private Vector3 ResolveAxisLocal()
    {
        // Si no quieres auto, usa el localAxis como antes
        if (!autoDetectAxis)
        {
            return (localAxis.sqrMagnitude < 0.0001f) ? Vector3.up : localAxis.normalized;
        }

        var renderers = wheelPivot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            // Fallback
            return (localAxis.sqrMagnitude < 0.0001f) ? Vector3.up : localAxis.normalized;
        }

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
                else
                {
                    localBounds.Encapsulate(pLocal);
                }
            }
        }

        Vector3 size = localBounds.size;

        if (size.x <= size.y && size.x <= size.z) return Vector3.right;   // X
        if (size.y <= size.x && size.y <= size.z) return Vector3.up;      // Y
        return Vector3.forward;                                           // Z
    }

    // ----------------- AUDIO HELPERS -----------------

    private void StartSpinAudio()
    {
        if (spinAudioSource == null || spinClip == null) return;

        spinAudioSource.clip = spinClip;
        spinAudioSource.volume = spinVolume;
        spinAudioSource.loop = loopWhileSpinning;

        if (!spinAudioSource.isPlaying)
            spinAudioSource.Play();
    }

    private void StopSpinAudio()
    {
        if (spinAudioSource == null) return;
        if (!spinAudioSource.isPlaying) return;

        if (fadeOutOnStop && fadeOutTime > 0f)
            StartCoroutine(FadeOutAndStop(spinAudioSource, fadeOutTime));
        else
            spinAudioSource.Stop();
    }

    private IEnumerator FadeOutAndStop(AudioSource src, float time)
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
        src.volume = startVol; // deja listo para la próxima
    }
}