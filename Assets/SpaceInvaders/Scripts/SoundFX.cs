using UnityEngine;

// Makes retro sound effects with code (no audio files needed!).
// A sound is just a list of numbers between -1 and 1 that we calculate and give to Unity.
[RequireComponent(typeof(AudioSource))]
public class SoundFX : MonoBehaviour
{
    public enum Sound { Shoot, InvaderKilled, PlayerDeath, MysteryKilled }

    [Range(0f, 1f)] public float volume = 0.25f;

    static SoundFX instance;
    const int SampleRate = 44100;

    AudioSource source;
    AudioClip shoot, invaderKilled, playerDeath, mysteryKilled;
    AudioClip[] march;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    void Awake()
    {
        instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        shoot = Tone("Shoot", 0.15f, 1400f, 300f);
        invaderKilled = Noise("InvaderKilled", 0.2f, 2, 1);
        playerDeath = Noise("PlayerDeath", 0.8f, 6, 2);
        mysteryKilled = Tone("MysteryKilled", 0.5f, 900f, 150f);
        march = new[]
        {
            Tone("March0", 0.09f, 110f, 104f),
            Tone("March1", 0.09f, 98f, 93f),
            Tone("March2", 0.09f, 87f, 83f),
            Tone("March3", 0.09f, 82f, 78f),
        };
    }

    public static void Play(Sound sound)
    {
        if (instance == null) return;
        AudioClip clip = sound switch
        {
            Sound.Shoot => instance.shoot,
            Sound.InvaderKilled => instance.invaderKilled,
            Sound.PlayerDeath => instance.playerDeath,
            _ => instance.mysteryKilled,
        };
        instance.source.PlayOneShot(clip, instance.volume);
    }

    public static void PlayMarch(int note)
    {
        if (instance == null) return;
        instance.source.PlayOneShot(instance.march[note % instance.march.Length], instance.volume);
    }

    // A "square wave" beep that slides from startFreq to endFreq and fades out.
    static AudioClip Tone(string name, float duration, float startFreq, float endFreq)
    {
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        double phase = 0;
        for (int i = 0; i < count; i++)
        {
            float progress = (float)i / count;
            phase += Mathf.Lerp(startFreq, endFreq, progress) / SampleRate;
            float wave = (phase % 1.0) < 0.5 ? 1f : -1f;
            samples[i] = wave * (1f - progress);
        }
        return MakeClip(name, samples);
    }

    // Random "crunchy" noise that fades out - sounds like an explosion.
    static AudioClip Noise(string name, float duration, int hold, int seed)
    {
        var rng = new System.Random(seed);
        int count = Mathf.CeilToInt(SampleRate * duration);
        float[] samples = new float[count];
        float value = 0f;
        for (int i = 0; i < count; i++)
        {
            if (i % (hold * 4) == 0) value = (float)(rng.NextDouble() * 2.0 - 1.0);
            float fade = 1f - (float)i / count;
            samples[i] = value * fade * fade;
        }
        return MakeClip(name, samples);
    }

    static AudioClip MakeClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
