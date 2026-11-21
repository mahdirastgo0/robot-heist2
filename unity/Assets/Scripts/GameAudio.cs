using System.Collections;
using UnityEngine;

// همه‌ی صداها با کد ساخته میشن (بدون فایل صوتی): تیراندازی، آلارم، پول و موزیک ملو (lo-fi)
public class GameAudio : MonoBehaviour
{
    public static GameAudio I { get; private set; }
    const int SR = 44100;

    AudioSource sfx, musicBase, musicCity;
    readonly AudioClip[] shot = new AudioClip[NativeCore.WeaponCount];
    AudioClip copShot, click, empty, cash, pickup, hurt, alarm, step, thud, coin;
    float baseTarget, cityTarget;
    bool musicOn = true;

    // نمونه‌های واقعی‌تر (فایل‌های WAV تو Resources/Audio). اگه نباشن، صدای ساخته‌شده با کد استفاده میشه.
    AudioClip[][] shotV = new AudioClip[NativeCore.WeaponCount][];
    AudioClip[] farV, stepV, wallV;
    AudioClip reloadPistol, reloadRifle, reloadShotgun, emptyW, bodyHit, ricochetW, hurtW, copDownW;
    readonly AudioSource[] pool = new AudioSource[40];
    AudioSource reloadSrc;
    readonly AudioClip[] reloadW = new AudioClip[NativeCore.WeaponCount];
    AudioClip equipW;
    AudioClip[] shellV;
    static readonly float[] ShotVol = { 0.9f, 0.78f, 1f, 0.95f, 1f };
    int poolI;

    public bool MusicOn => musicOn;

    void Awake()
    {
        I = this;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false; sfx.spatialBlend = 0f;
        reloadSrc = gameObject.AddComponent<AudioSource>();
        reloadSrc.playOnAwake = false; reloadSrc.spatialBlend = 0f;
        musicBase = NewMusicSource();
        musicCity = NewMusicSource();
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false; pool[i].spatialBlend = 0f;
        }
        BuildSfx();
        LoadSamples();
        StartCoroutine(BuildMusic());
    }

    AudioSource NewMusicSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = true; s.playOnAwake = false; s.volume = 0f; s.spatialBlend = 0f;
        return s;
    }

    void Update()
    {
        float step = Time.unscaledDeltaTime * 0.6f;
        musicBase.volume = Mathf.MoveTowards(musicBase.volume, musicOn ? baseTarget : 0f, step);
        musicCity.volume = Mathf.MoveTowards(musicCity.volume, musicOn ? cityTarget : 0f, step);
    }

    public void SetMood(float baseVol, float cityVol) { baseTarget = baseVol; cityTarget = cityVol; }
    public void ToggleMusic() { musicOn = !musicOn; }

    // ---------------------------------------------------------------- Samples
    static AudioClip[] LoadSet(string prefix, int n)
    {
        var l = new System.Collections.Generic.List<AudioClip>();
        for (int i = 1; i <= n; i++) { var c = Resources.Load<AudioClip>(prefix + i); if (c != null) l.Add(c); }
        return l.Count > 0 ? l.ToArray() : null;
    }

    void LoadSamples()
    {
        for (int w = 0; w < NativeCore.WeaponCount; w++) shotV[w] = LoadSet("Audio/shot_" + w + "_", 3);
        farV = LoadSet("Audio/shotfar_", 2);
        for (int w = 0; w < NativeCore.WeaponCount; w++) reloadW[w] = Resources.Load<AudioClip>("Audio/reload_w" + w);
        equipW = Resources.Load<AudioClip>("Audio/equip");
        shellV = LoadSet("Audio/shell_", 2);
        stepV = LoadSet("Audio/step_", 3);
        wallV = LoadSet("Audio/impact_wall_", 3);
        reloadPistol = Resources.Load<AudioClip>("Audio/reload_pistol");
        reloadRifle = Resources.Load<AudioClip>("Audio/reload_rifle");
        reloadShotgun = Resources.Load<AudioClip>("Audio/reload_shotgun");
        emptyW = Resources.Load<AudioClip>("Audio/empty");
        bodyHit = Resources.Load<AudioClip>("Audio/impact_body");
        ricochetW = Resources.Load<AudioClip>("Audio/ricochet");
        hurtW = Resources.Load<AudioClip>("Audio/hurt");
        copDownW = Resources.Load<AudioClip>("Audio/copdown");
    }

    static AudioClip Pick(AudioClip[] a) { return (a != null && a.Length > 0) ? a[Random.Range(0, a.Length)] : null; }

    // با pitch تصادفی کوچیک تا صداها تکراری نشن
    void Play(AudioClip c, float vol, float pitch = 1f)
    {
        if (c == null) return;
        var s = FreeSource();
        s.Stop(); s.clip = c; s.volume = vol; s.pitch = pitch;
        s.Play();
    }

    // یه منبع صدای آزاد؛ اگه همه مشغولن، اونی که بیشتر پخش شده (نزدیک تموم شدن) رو میگیره. دیگه شلیک وسط صدای قبلی قطع نمیشه.
    AudioSource FreeSource()
    {
        for (int k = 0; k < pool.Length; k++)
        {
            int i = (poolI + k) % pool.Length;
            if (!pool[i].isPlaying) { poolI = (i + 1) % pool.Length; return pool[i]; }
        }
        int best = 0; float bp = -1f;
        for (int i = 0; i < pool.Length; i++)
        {
            float prog = pool[i].clip != null && pool[i].clip.length > 0f ? pool[i].time / pool[i].clip.length : 1f;
            if (prog > bp) { bp = prog; best = i; }
        }
        return pool[best];
    }

    static float Jit() { return Random.Range(0.97f, 1.03f); }

    // ---------------------------------------------------------------- Play
    public void Shot(int weapon)
    {
        int wi = Mathf.Clamp(weapon, 0, shotV.Length - 1);
        var c = Pick(shotV[wi]);
        if (c != null) Play(c, ShotVol[wi] * Random.Range(0.92f, 1f), Jit());
        else sfx.PlayOneShot(shot[Mathf.Clamp(weapon, 0, shot.Length - 1)], 0.9f);
    }

    // تیر پلیس: نزدیک = تپانچه‌ی واضح، دور = صدای خفه با پژواک بیشتر
    public void CopShot(Vector3 pos)
    {
        float dist = 0f;
        var cam = Camera.main;
        if (cam != null) dist = Vector3.Distance(cam.transform.position, pos);
        var c = dist > 35f ? Pick(farV) : Pick(shotV[0]);
        if (c == null) c = copShot;
        AudioSource.PlayClipAtPoint(c, pos, dist > 35f ? 1f : 0.9f);
    }

    public void Reload(int weapon)
    {
        AudioClip c = reloadW[Mathf.Clamp(weapon, 0, reloadW.Length - 1)];
        if (c == null) c = weapon == 0 ? reloadPistol : weapon == 2 ? reloadShotgun : reloadRifle;
        if (c == null) { sfx.PlayOneShot(click, 0.7f); return; }
        reloadSrc.Stop(); reloadSrc.clip = c; reloadSrc.volume = 0.85f; reloadSrc.pitch = 1f; reloadSrc.Play();
    }

    public void ReloadStop() { if (reloadSrc != null) reloadSrc.Stop(); }
    public void Equip() { if (equipW != null) Play(equipW, 0.7f, Jit()); }
    public void Shell(Vector3 pos)
    {
        var c = Pick(shellV);
        if (c != null) AudioSource.PlayClipAtPoint(c, pos, 0.35f);
    }

    public void Click() { sfx.PlayOneShot(click, 0.7f); }
    public void Empty() { if (emptyW != null) Play(emptyW, 0.7f, Jit()); else sfx.PlayOneShot(empty, 0.6f); }
    public void Cash() { sfx.PlayOneShot(cash, 0.8f); }
    public void Coin() { sfx.PlayOneShot(coin, 0.7f); }
    public void Pickup() { sfx.PlayOneShot(pickup, 0.8f); }
    public void Hurt() { if (hurtW != null) Play(hurtW, 0.9f, Jit()); else sfx.PlayOneShot(hurt, 0.9f); }
    public void Alarm() { sfx.PlayOneShot(alarm, 0.55f); }
    public void Step() { var c = Pick(stepV); if (c != null) Play(c, 0.35f, Random.Range(0.9f, 1.1f)); else sfx.PlayOneShot(step, 0.25f); }
    public void Thud(Vector3 pos) { AudioSource.PlayClipAtPoint(bodyHit != null ? bodyHit : thud, pos, 0.8f); }
    public void ImpactBody(Vector3 pos) { AudioSource.PlayClipAtPoint(bodyHit != null ? bodyHit : thud, pos, 0.9f); }
    public void ImpactWall(Vector3 pos) { var c = Pick(wallV); if (c != null) AudioSource.PlayClipAtPoint(c, pos, 0.8f); }
    public void Ricochet(Vector3 pos) { if (ricochetW != null) AudioSource.PlayClipAtPoint(ricochetW, pos, 0.6f); }
    public void CopDown(Vector3 pos) { AudioSource.PlayClipAtPoint(copDownW != null ? copDownW : thud, pos, 0.9f); }

    // ---------------------------------------------------------------- SFX synthesis
    static AudioClip Make(string name, float[] d)
    {
        var c = AudioClip.Create(name, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    void BuildSfx()
    {
        //                 dur   noise lp    thumpHz decay noiseDecay
        shot[0] = Make("pistol",  Burst(0.25f, 1.0f, 0.6f, 150f, 22f, 28f));
        shot[1] = Make("smg",     Burst(0.16f, 0.9f, 0.7f, 180f, 30f, 40f));
        shot[2] = Make("shotgun", Burst(0.60f, 1.3f, 0.35f, 90f, 9f, 10f));
        shot[3] = Make("rifle",   Burst(0.30f, 1.1f, 0.5f, 110f, 16f, 20f));
        shot[4] = Make("lmg",     Burst(0.22f, 1.0f, 0.4f, 80f, 20f, 24f));
        copShot = Make("copshot", Burst(0.22f, 0.7f, 0.8f, 130f, 26f, 32f));
        hurt = Make("hurt",       Burst(0.20f, 0.7f, 0.3f, 110f, 20f, 25f));
        thud = Make("thud",       Burst(0.18f, 0.5f, 0.25f, 90f, 25f, 30f));
        click = Make("click",     Tick(0.09f, 90f, 0.7f, 0.9f));
        empty = Make("empty",     Tick(0.05f, 140f, 0.6f, 0.9f));
        step = Make("step",       Tick(0.08f, 55f, 0.7f, 0.18f));
        cash = Make("cash",       Chime(new[] { 1046f, 1568f }, 0.09f, 0.7f, 7f, 0.35f));
        coin = Make("coin",       Chime(new[] { 880f, 1318f }, 0.06f, 0.5f, 9f, 0.35f));
        pickup = Make("pickup",   Chime(new[] { 523f, 659f, 784f }, 0.07f, 0.35f, 10f, 0.3f));
        alarm = Make("alarm",     Siren(1.6f));
    }

    // انفجار نویز + ضربه‌ی کم‌فرکانس (شلیک)
    static float[] Burst(float dur, float noiseGain, float lp, float thumpHz, float thumpDecay, float noiseDecay)
    {
        int n = (int)(dur * SR);
        var d = new float[n];
        float low = 0f, ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            low += lp * ((Random.value * 2f - 1f) - low);
            ph += 2f * Mathf.PI * (thumpHz * Mathf.Exp(-t * 8f) + 35f) / SR;
            float s = low * noiseGain * Mathf.Exp(-t * noiseDecay) + Mathf.Sin(ph) * 0.9f * Mathf.Exp(-t * thumpDecay);
            d[i] = (float)System.Math.Tanh(s * 1.6f) * 0.9f;
        }
        return d;
    }

    // تیک کوتاه (خشاب، قدم)
    static float[] Tick(float dur, float decay, float gain, float lp)
    {
        int n = (int)(dur * SR);
        var d = new float[n];
        float low = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            low += lp * ((Random.value * 2f - 1f) - low);
            d[i] = low * Mathf.Exp(-t * decay) * gain;
        }
        return d;
    }

    // زنگ (پول، خرید، برداشتن غنیمت)
    static float[] Chime(float[] freqs, float gap, float len, float decay, float gain)
    {
        int n = (int)((gap * freqs.Length + len) * SR);
        var d = new float[n];
        for (int k = 0; k < freqs.Length; k++)
        {
            int start = (int)(k * gap * SR);
            for (int i = 0; start + i < n && i < (int)(len * SR); i++)
            {
                float t = i / (float)SR;
                d[start + i] += Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * Mathf.Exp(-t * decay) * gain;
            }
        }
        return d;
    }

    static float[] Siren(float dur)
    {
        int n = (int)(dur * SR);
        var d = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float f = 850f + 250f * Mathf.Sin(2f * Mathf.PI * 1.4f * t);
            ph += 2f * Mathf.PI * f / SR;
            float env = Mathf.Min(1f, t * 20f) * Mathf.Min(1f, (dur - t) * 10f);
            d[i] = (Mathf.Sin(ph) + 0.3f * Mathf.Sin(3f * ph)) * 0.35f * env;
        }
        return d;
    }

    // ---------------------------------------------------------------- Music (lo-fi, mellow)
    IEnumerator BuildMusic()
    {
        // آکوردها به‌صورت نت MIDI
        int[][] calm = { new[] { 48, 52, 55, 59 }, new[] { 45, 48, 52, 55 }, new[] { 50, 53, 57, 60 }, new[] { 43, 47, 50, 53 } };  // Cmaj7 Am7 Dm7 G7
        int[][] city = { new[] { 45, 48, 52, 55 }, new[] { 41, 45, 48, 52 }, new[] { 48, 52, 55, 59 }, new[] { 40, 44, 47, 50 } };  // Am7 Fmaj7 Cmaj7 E7

        // آهنگ‌های ازقبل‌ساخته‌شده (Resources/Audio/music_base و music_city)
        var mb = Resources.Load<AudioClip>("Audio/music_base");
        var mc = Resources.Load<AudioClip>("Audio/music_city");
        if (mb != null && mc != null)
        {
            musicBase.clip = mb; musicBase.Play();
            musicCity.clip = mc; musicCity.Play();
            yield break;
        }

        AudioClip a = null, b = null;
        yield return Compose("calm", calm, 72f, c => a = c);
        musicBase.clip = a; musicBase.Play();
        yield return Compose("city", city, 80f, c => b = c);
        musicCity.clip = b; musicCity.Play();
    }

    IEnumerator Compose(string name, int[][] chords, float bpm, System.Action<AudioClip> done)
    {
        float beat = 60f / bpm;
        int chordSamples = (int)(beat * 4f * SR);
        int N = chordSamples * chords.Length;
        var buf = new float[N];
        var rng = new System.Random(7);
        int[] pat = { 0, 1, 2, 3, 2, 1, 3, 2 };

        for (int c = 0; c < chords.Length; c++)
        {
            int cs = c * chordSamples;
            var ch = chords[c];

            // پد نرم
            foreach (int n in ch) AddPad(buf, cs, Freq(n + 12), beat * 4.1f, 0.05f);
            yield return null;

            // بیس و کیک و هت
            AddBass(buf, cs, Freq(ch[0] - 12), beat * 2f, 0.22f);
            AddBass(buf, cs + (int)(beat * 2f * SR), Freq(ch[0] - 12), beat * 1.8f, 0.16f);
            AddKick(buf, cs, 0.35f);
            AddKick(buf, cs + (int)(beat * 2f * SR), 0.3f);
            AddSnare(buf, cs + (int)(beat * 1f * SR), 0.1f);
            AddSnare(buf, cs + (int)(beat * 3f * SR), 0.1f);
            for (int e = 0; e < 8; e++)
            {
                float swing = (e % 2 == 1) ? 0.06f : 0f;     // سوینگ lo-fi
                int st = cs + (int)((e * 0.5f + swing) * beat * SR);
                AddHat(buf, st, e % 2 == 0 ? 0.03f : 0.045f, rng);
            }

            // آرپژ پیانوی الکتریکی
            for (int e = 0; e < 8; e++)
            {
                float swing = (e % 2 == 1) ? 0.06f : 0f;
                int st = cs + (int)((e * 0.5f + swing) * beat * SR);
                AddEP(buf, st, Freq(ch[pat[e]] + 12), 1.0f, e % 4 == 0 ? 0.18f : 0.13f);
            }
            yield return null;
        }

        // خش‌خش صفحه، فیلتر نرم، نرمال‌سازی
        for (int i = 0; i < N; i++)
            if (rng.NextDouble() < 0.0004) buf[i] += ((float)rng.NextDouble() * 2f - 1f) * 0.04f;
        float lp = 0f, peak = 0.0001f;
        for (int i = 0; i < N; i++)
        {
            lp += 0.28f * (buf[i] - lp);
            buf[i] = lp;
            peak = Mathf.Max(peak, Mathf.Abs(lp));
        }
        float g = 0.65f / peak;
        for (int i = 0; i < N; i++) buf[i] *= g;

        done(Make(name, buf));
    }

    static float Freq(int midi) { return 440f * Mathf.Pow(2f, (midi - 69) / 12f); }

    static void AddEP(float[] buf, int start, float f, float dur, float amp)
    {
        int len = (int)(dur * SR), N = buf.Length;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            float env = (1f - Mathf.Exp(-t * 250f)) * Mathf.Exp(-t * 3f);
            float w = 2f * Mathf.PI * f * t;
            float v = Mathf.Sin(w) + 0.35f * Mathf.Sin(2f * w) * Mathf.Exp(-t * 4f);
            buf[(start + i) % N] += v * env * amp * (1f + 0.06f * Mathf.Sin(2f * Mathf.PI * 5f * t));
        }
    }

    static void AddPad(float[] buf, int start, float f, float dur, float amp)
    {
        int len = (int)(dur * SR), N = buf.Length;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            float s = Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / dur));
            float w = 2f * Mathf.PI * f * t;
            buf[(start + i) % N] += (Mathf.Sin(w) + 0.7f * Mathf.Sin(w * 1.004f)) * s * s * amp;
        }
    }

    static void AddBass(float[] buf, int start, float f, float dur, float amp)
    {
        int len = (int)(dur * SR), N = buf.Length;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            float env = (1f - Mathf.Exp(-t * 80f)) * Mathf.Exp(-t * 1.8f);
            float w = 2f * Mathf.PI * f * t;
            buf[(start + i) % N] += (Mathf.Sin(w) + 0.2f * Mathf.Sin(2f * w)) * env * amp;
        }
    }

    static void AddKick(float[] buf, int start, float amp)
    {
        int len = (int)(0.3f * SR), N = buf.Length;
        float ph = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            ph += 2f * Mathf.PI * (45f + 75f * Mathf.Exp(-t * 25f)) / SR;
            buf[(start + i) % N] += Mathf.Sin(ph) * Mathf.Exp(-t * 14f) * amp;
        }
    }

    static void AddSnare(float[] buf, int start, float amp)
    {
        int len = (int)(0.2f * SR), N = buf.Length;
        float low = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            low += 0.6f * ((Random.value * 2f - 1f) - low);
            buf[(start + i) % N] += (low * Mathf.Exp(-t * 22f) + 0.5f * Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 30f)) * amp;
        }
    }

    static void AddHat(float[] buf, int start, float amp, System.Random rng)
    {
        int len = (int)(0.06f * SR), N = buf.Length;
        float prev = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            float n = (float)rng.NextDouble() * 2f - 1f;
            buf[(start + i) % N] += (n - prev) * Mathf.Exp(-t * 90f) * amp;
            prev = n;
        }
    }
}
