using System.Collections.Generic;
using UnityEngine;

public class CasinoAudioSfx : MonoBehaviour
{
    public static CasinoAudioSfx Instance { get; private set; }

    [Header("UI Clips")]
    public AudioClip click_001;
    public AudioClip maximize_006;
    public AudioClip error_003;
    public AudioClip minimize_006;
    public AudioClip switch_001;

    [Header("Jingles")]
    public AudioClip lose_jingles_SAX07;
    public AudioClip win_270545;

    [Header("Dice Throws (7 clips)")]
    public List<AudioClip> diceThrows = new List<AudioClip>();

    [Header("Volumes")]
    [Range(0f, 1f)] public float uiVolume = 0.9f;
    [Range(0f, 1f)] public float diceVolume = 0.85f;
    [Range(0f, 1f)] public float jingleVolume = 1.0f;

    [Header("Credits Music")]
    public AudioClip creditsMusic;
    [Range(0f, 1f)] public float creditsMusicVolume = 0.6f;

    AudioSource musicSrc;

    AudioSource src;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        src = GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f; // 2D

        musicSrc = gameObject.AddComponent<AudioSource>();
        musicSrc.playOnAwake = false;
        musicSrc.loop = true;
        musicSrc.spatialBlend = 0f; // 2D
    }

    void PlayOneShot(AudioClip clip, float vol)
    {
        if (clip == null || src == null) return;
        src.PlayOneShot(clip, vol);
    }

    public void PlayPickCategory() => PlayOneShot(click_001, uiVolume);
    public void PlayHoldToggle() => PlayOneShot(switch_001, uiVolume);

    public void PlayScoreMax() => PlayOneShot(maximize_006, uiVolume);

    public void PlayScoreZero()
    {
        var clip = (Random.value < 0.5f) ? error_003 : minimize_006;
        PlayOneShot(clip, uiVolume);
    }

    public void PlayWin() => PlayOneShot(win_270545, jingleVolume);
    public void PlayLose() => PlayOneShot(lose_jingles_SAX07, jingleVolume);

    public void PlayDiceThrow(float delay = 0f)
    {
        if (diceThrows == null || diceThrows.Count == 0) return;
        var c = diceThrows[Random.Range(0, diceThrows.Count)];
        if (c == null) return;

        if (delay <= 0f) PlayOneShot(c, diceVolume);
        else StartCoroutine(Delayed(c, delay));
    }

    System.Collections.IEnumerator Delayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayOneShot(clip, diceVolume);
    }

    public void PlayCreditsMusic()
{
    if (creditsMusic == null || musicSrc == null) return;

    if (musicSrc.isPlaying && musicSrc.clip == creditsMusic) return;

    musicSrc.clip = creditsMusic;
    musicSrc.volume = creditsMusicVolume;
    musicSrc.loop = true;
    musicSrc.Play();
}

public void StopCreditsMusic()
{
    if (musicSrc == null) return;
    musicSrc.Stop();
    musicSrc.clip = null;
}
}