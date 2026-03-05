using UnityEngine;

public class RouletteLightsBlink : MonoBehaviour
{
    [Header("Renderer (si lo dejas vacío, busca el primero en hijos)")]
    public Renderer targetRenderer;

    [Header("Parpadeo")]
    public float blinkPeriod = 0.08f;  // rápido
    public bool startOn = true;

    [Header("Comportamiento")]
    public bool startBlinkingOnEnable = false; // IMPORTANTE: déjalo en false
    public bool turnOffWhenStopped = true;     // al parar, se apagan

    [Header("Emisión")]
    [ColorUsage(true, true)] public Color onEmission = Color.white; // HDR 5–10
    [Range(0f, 1f)] public float offBaseDim = 0.15f;

    // glTFast
    static readonly int EmissiveFactorId = Shader.PropertyToID("emissiveFactor");
    static readonly int BaseColorFactorId = Shader.PropertyToID("baseColorFactor");

    // URP/Standard fallback
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    MaterialPropertyBlock _mpb;
    Color _baseOriginal = Color.white;
    float _t;
    bool _isOn;
    bool _blinking;

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>(true);

        _mpb = new MaterialPropertyBlock();

        if (targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            var mat = targetRenderer.sharedMaterial;
            mat.EnableKeyword("_EMISSION");

            if (mat.HasProperty(BaseColorFactorId)) _baseOriginal = mat.GetColor(BaseColorFactorId);
            else if (mat.HasProperty(BaseColorId)) _baseOriginal = mat.GetColor(BaseColorId);
            else if (mat.HasProperty(ColorId)) _baseOriginal = mat.GetColor(ColorId);
        }

        _isOn = startOn;
        _blinking = startBlinkingOnEnable;

        if (_blinking) ApplyState(_isOn);
        else ApplyState(!turnOffWhenStopped); // si turnOffWhenStopped=true => apaga
    }

    void Update()
    {
        if (!_blinking) return;
        if (targetRenderer == null) return;
        if (blinkPeriod <= 0f) return;

        _t += Time.deltaTime;
        if (_t >= blinkPeriod)
        {
            _t = 0f;
            _isOn = !_isOn;
            ApplyState(_isOn);
        }
    }

    // ✅ Esto lo llamas desde la ruleta
    public void SetBlinking(bool enable)
    {
        _blinking = enable;
        _t = 0f;

        if (_blinking)
        {
            _isOn = startOn;
            ApplyState(_isOn);
        }
        else
        {
            ApplyState(!turnOffWhenStopped); // true => queda prendido, false => queda apagado
        }
    }

    void ApplyState(bool on)
    {
        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(_mpb);

        if (on)
        {
            _mpb.SetColor(EmissiveFactorId, onEmission);
            _mpb.SetColor(EmissionColorId, onEmission);

            _mpb.SetColor(BaseColorFactorId, _baseOriginal);
            _mpb.SetColor(BaseColorId, _baseOriginal);
            _mpb.SetColor(ColorId, _baseOriginal);
        }
        else
        {
            _mpb.SetColor(EmissiveFactorId, Color.black);
            _mpb.SetColor(EmissionColorId, Color.black);

            Color dim = _baseOriginal * offBaseDim;
            _mpb.SetColor(BaseColorFactorId, dim);
            _mpb.SetColor(BaseColorId, dim);
            _mpb.SetColor(ColorId, dim);
        }

        targetRenderer.SetPropertyBlock(_mpb);
    }
}