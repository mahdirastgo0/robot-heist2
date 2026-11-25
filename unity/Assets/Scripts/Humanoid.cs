using UnityEngine;

public class GunSlot
{
    public GameObject go;
    public Transform muzzle, rGrip, lGrip;
    public Vector3 aim;      // (مربوط به آدم ساده) جای اسلحه موقع نشانه‌گیری
    public Vector3 rLocal, lLocal;   // دستگیره‌ی راست/چپ در فضای اسلحه (با اندازه‌ی واقعی)
}

// انیمیشن آدم: راه رفتن، دویدن، حالت نشانه‌گیری. مدل رو GameManager.BuildHuman می‌سازه.
public class Humanoid : MonoBehaviour
{
    public Transform body, hipL, hipR, kneeL, kneeR, shoulderL, shoulderR, elbowL, elbowR, gunMount, gun, bag;
    public GameObject vest;
    public Renderer statusLight;
    public BonePoser poser;            // اگه مدل FBX داشته باشیم، استخوان‌ها با این حرکت می‌کنن
    public GunSlot[] slots;            // مدل‌های اسلحه (برای بازیکن ۵ تا، برای پلیس فقط تپانچه)
    int cur;
    public Vector3 gunLow = new Vector3(0.22f, 0.02f, 0.2f), gunAim = new Vector3(0.18f, 0.5f, 0.45f);
    public float speed;            // سرعت حرکت (m/s) - صاحب مدل هر فریم تنظیم می‌کنه
    public float crouch, air, verticalSpeed;
    public Vector3 moveLocal;
    float landCompression, previousAir;
    public bool aiming;            // دست‌ها به سمت جلو و اسلحه بالا
    public bool handsUp;           // دست‌ها بالا (صندوقدارِ ترسیده؛ فقط آدم ساده‌ی ساخته‌شده با کد)
    public System.Action onStep;   // هر بار پا به زمین میخوره

    float phase, ampS, aimT, lastSin;

    // ---- افکت‌های اسلحه: لگد، رلود، تعویض، فلش دهانه و پوکه
    float recoil, switchT, reloadT = -1f, reloadDur, reloadBlend, flashT;
    int reloadStyle;                       // 0 خشاب، 1 شاتگان (ساچمه + پمپ)، 2 کمربند فشنگ (LMG)
    public float kickPos = 0.07f, kickRot = 6.5f;
    GameObject flashRoot, flashStar;
    float starRoll;
    Light flashLight;
    Material flashMat, starMat;
    static Material brassMat;

    // هر شلیک: لگد + فلش دهانه + پوکه
    public void Fire(float power, bool shell)
    {
        recoil = Mathf.Min(1.7f, recoil + power);
        MuzzleFlash(power);
        if (shell) EjectShell();
    }

    public void BeginReload(float duration, int style) { reloadT = 0f; reloadDur = Mathf.Max(0.3f, duration); reloadStyle = style; }
    public void CancelReload() { reloadT = -1f; }
    public bool ReloadingAnim { get { return reloadT >= 0f; } }

    void OnDestroy() { if (flashRoot != null) Destroy(flashRoot); }
    static readonly Color[] VestCol = { Color.black, new Color(0.35f, 0.4f, 0.5f), new Color(0.25f, 0.35f, 0.25f), new Color(0.1f, 0.1f, 0.12f) };

    // اسلحه‌ی شماره w رو نشون بده (مدل FBX/bytes یا مکعب ساده)
    public void ShowGun(int w)
    {
        if (slots != null && slots.Length > 0)
        {
            int prev = cur;
            cur = Mathf.Clamp(w, 0, slots.Length - 1);
            if (prev != cur) { switchT = 1f; reloadT = -1f; }
            for (int i = 0; i < slots.Length; i++) if (slots[i] != null) slots[i].go.SetActive(i == cur);
            if (gun != null) gun.gameObject.SetActive(false);
        }
        else if (gun != null) gun.gameObject.SetActive(true);
    }

    public Vector3 MuzzlePoint(float cubeLen)
    {
        if (slots != null && slots.Length > 0 && slots[cur] != null && slots[cur].muzzle != null) return slots[cur].muzzle.position;
        return gun.TransformPoint(0f, 0f, cubeLen * 0.5f + 0.03f);
    }

    public void SetVest(int level)
    {
        if (vest == null) return;
        vest.SetActive(level > 0);
        if (level > 0) vest.GetComponent<Renderer>().material.color = VestCol[Mathf.Clamp(level, 0, 3)];
    }

    public void SetBag(float fill)
    {
        if (bag != null) bag.localScale = Vector3.one * (0.8f + 0.7f * Mathf.Clamp01(fill));
    }

    Transform MuzzleTransform()
    {
        if (slots != null && slots.Length > 0 && slots[cur] != null && slots[cur].muzzle != null) return slots[cur].muzzle;
        return gun;
    }

    void BuildFlash()
    {
        flashRoot = new GameObject("MuzzleFlash");
        var tex = Resources.Load<Texture2D>("Icons/flash");
        var texStar = Resources.Load<Texture2D>("Icons/flash_star");
        var sh = Shader.Find("Sprites/Default");
        flashMat = new Material(sh); flashMat.mainTexture = tex; flashMat.color = Color.white;
        starMat = new Material(sh); starMat.mainTexture = texStar; starMat.color = Color.white;
        for (int k = 0; k < 2; k++)
        {
            var arm = new GameObject("Cross" + k).transform;
            arm.SetParent(flashRoot.transform, false);
            arm.localRotation = Quaternion.Euler(0f, 0f, 90f * k);
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(arm, false);
            q.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);        // محور x بافت = جهت لوله
            q.transform.localPosition = new Vector3(0f, 0f, 0.16f);
            q.transform.localScale = new Vector3(0.34f, 0.16f, 1f);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = flashMat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        flashStar = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(flashStar.GetComponent<Collider>());
        flashStar.transform.SetParent(flashRoot.transform, false);
        flashStar.transform.localPosition = new Vector3(0f, 0f, 0.03f);
        var sr = flashStar.GetComponent<Renderer>(); sr.sharedMaterial = starMat; sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lg = new GameObject("FlashLight");
        lg.transform.SetParent(flashRoot.transform, false);
        lg.transform.localPosition = new Vector3(0f, 0f, 0.25f);
        flashLight = lg.AddComponent<Light>();
        flashLight.type = LightType.Point; flashLight.range = 8f; flashLight.color = new Color(1f, 0.72f, 0.38f); flashLight.shadows = LightShadows.None;
        flashRoot.SetActive(false);
    }

    void MuzzleFlash(float power)
    {
        var mz = MuzzleTransform();
        if (mz == null) return;
        if (flashRoot == null) BuildFlash();
        flashRoot.SetActive(true);
        flashT = 0.055f;
        float s = Mathf.Lerp(0.8f, 1.5f, Mathf.Clamp01(power / 1.6f)) * Random.Range(0.85f, 1.2f);
        flashRoot.transform.localScale = Vector3.one * s;
        starRoll = Random.Range(0f, 360f);
        flashStar.transform.localScale = Vector3.one * Random.Range(0.28f, 0.4f);
        flashLight.intensity = Random.Range(2.2f, 3.6f) * Mathf.Lerp(0.8f, 1.4f, Mathf.Clamp01(power / 1.6f));
        PlaceFlash(mz);
    }

    void PlaceFlash(Transform mz)
    {
        Vector3 pos = mz == gun ? MuzzlePoint(0.3f) : mz.position;
        Quaternion rot = mz.rotation;
        flashRoot.transform.SetPositionAndRotation(pos, rot);
        if (flashStar != null && Camera.main != null)
        {
            // ستاره همیشه رو به دوربینه
            Vector3 toCam = Camera.main.transform.position - flashStar.transform.position;
            if (toCam.sqrMagnitude > 0.01f) flashStar.transform.rotation = Quaternion.LookRotation(-toCam.normalized) * Quaternion.Euler(0f, 0f, starRoll);
        }
    }

    void UpdateFx(float dt)
    {
        if (flashT > 0f)
        {
            flashT -= dt;
            var mz = MuzzleTransform();
            if (flashT <= 0f || mz == null) { if (flashRoot != null) flashRoot.SetActive(false); }
            else PlaceFlash(mz);
        }
        if (reloadT >= 0f)
        {
            reloadT += dt;
            if (reloadT >= reloadDur) reloadT = -1f;
        }
        float tgt = reloadT >= 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Min(reloadT / 0.14f, (reloadDur - reloadT) / 0.14f)) : 0f;
        reloadBlend = Mathf.MoveTowards(reloadBlend, tgt, dt * 7f);
    }

    void EjectShell()
    {
        var mz = MuzzleTransform();
        if (mz == null) return;
        Transform src = (slots != null && slots.Length > 0 && slots[cur] != null && slots[cur].go != null) ? slots[cur].go.transform : (gun != null ? gun : mz);
        Vector3 pos = src.position + src.up * 0.05f + src.forward * 0.02f;
        Vector3 vel = src.right * Random.Range(1.4f, 2.2f) + Vector3.up * Random.Range(1.2f, 2.0f) - src.forward * Random.Range(0.1f, 0.5f);
        ShellCasing.Spawn(pos, vel, transform.position.y);
    }

    // ---- ترکیب لگد / رلود / تعویض روی جای اسلحه (در فضای بدنه)
    static readonly float[] MagT = { 0f, 0.12f, 0.30f, 0.50f, 0.62f, 0.70f, 0.85f, 1f };
    static readonly Vector3[] MagV =
    {
        Vector3.zero, new Vector3(0f, -0.07f, -0.04f), new Vector3(-0.04f, -0.34f, -0.16f), new Vector3(-0.04f, -0.34f, -0.16f),
        new Vector3(0f, -0.07f, -0.04f), new Vector3(0f, -0.03f, -0.03f), Vector3.zero, Vector3.zero
    };
    static readonly float[] BeltT = { 0f, 0.12f, 0.28f, 0.55f, 0.72f, 0.84f, 0.92f, 1f };
    static readonly Vector3[] BeltV =
    {
        Vector3.zero, new Vector3(0f, 0.16f, -0.06f), new Vector3(0f, 0.18f, -0.08f), new Vector3(0f, -0.3f, -0.12f),
        new Vector3(0f, 0.14f, -0.06f), new Vector3(0f, 0.04f, -0.05f), new Vector3(0f, 0.05f, -0.22f), Vector3.zero
    };

    static Vector3 Keys(float p, float[] t, Vector3[] v)
    {
        for (int i = 1; i < t.Length; i++)
            if (p <= t[i]) return Vector3.Lerp(v[i - 1], v[i], Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(t[i - 1], t[i], p)));
        return v[v.Length - 1];
    }

    void ApplyGunFx(float dt, ref Vector3 pos, ref Quaternion rot, out Vector3 lExtra, out float leftForce)
    {
        lExtra = Vector3.zero; leftForce = 0f;
        float tt = Time.time;

        // تنفس و تاب خوردن آروم وقتی ایستادی
        float still = 1f - Mathf.Clamp01(speed / 3f);
        pos += new Vector3(Mathf.Sin(tt * 1.3f) * 0.003f, Mathf.Sin(tt * 1.9f) * 0.004f, 0f) * still;
        // حرکت اسلحه موقع دویدن
        float run = Mathf.Clamp01(speed / 6f) * (1f - aimT);
        pos += new Vector3(0f, Mathf.Sin(mphase * 2f) * 0.018f * run, 0f);
        rot *= Quaternion.Euler(Mathf.Sin(mphase * 2f) * 3f * run, Mathf.Sin(mphase) * 2f * run, 0f);

        // درآوردن اسلحه
        if (switchT > 0f)
        {
            switchT = Mathf.Max(0f, switchT - dt / 0.42f);
            float s = Mathf.SmoothStep(0f, 1f, switchT);
            pos += new Vector3(0.05f * s, -0.30f * s, 0.05f * s);
            rot *= Quaternion.Euler(60f * s, 0f, -18f * s);
        }

        // لگد: اسلحه عقب میاد و دهانه بالا میره
        recoil *= Mathf.Exp(-dt * 13f);
        if (recoil > 0.002f)
        {
            pos += rot * (Vector3.back * kickPos * recoil) + Vector3.up * 0.012f * recoil;
            float n = Mathf.PerlinNoise(tt * 25f, 3.3f) - 0.5f;
            rot *= Quaternion.Euler(-kickRot * recoil, n * kickRot * 0.5f * recoil, n * 4f * recoil);
        }

        // رلود
        if (reloadT >= 0f && reloadDur > 0f)
        {
            float p = Mathf.Clamp01(reloadT / reloadDur);
            float low = Mathf.SmoothStep(0f, 1f, p / 0.16f) * (1f - Mathf.SmoothStep(0f, 1f, (p - 0.84f) / 0.16f));
            float depth = reloadStyle == 2 ? 1.5f : 1f;
            pos += new Vector3(0.02f * low, -0.10f * low * depth, -0.03f * low);
            rot *= Quaternion.Euler(-18f * low * depth, 0f, -(reloadStyle == 1 ? 12f : 26f) * low);
            leftForce = 1f;
            if (reloadStyle == 1)
            {
                if (p < 0.72f)
                {
                    float c = 0.5f + 0.5f * Mathf.Sin(p / 0.72f * 3f * 2f * Mathf.PI - Mathf.PI * 0.5f);     // ۳ ساچمه
                    lExtra = Vector3.Lerp(Vector3.zero, new Vector3(0f, -0.24f, -0.12f), c);
                }
                else lExtra = new Vector3(0f, 0f, -0.18f * Mathf.Sin((p - 0.72f) / 0.28f * Mathf.PI));      // پمپ
            }
            else if (reloadStyle == 2) lExtra = Keys(p, BeltT, BeltV);
            else lExtra = Keys(p, MagT, MagV);
            // لرزش موقع جا زدن خشاب
            if (reloadStyle == 0 && p > 0.60f && p < 0.68f) pos += new Vector3(0f, Mathf.Sin((p - 0.6f) * 160f) * 0.006f, 0f);
        }
    }

    float mphase;

    // ---- مدل FBX: دویدن/راه رفتن و گرفتن اسلحه تو دست راست (و دست چپ موقع نشانه‌گیری)
    void UpdateModel(float dt)
    {
        float runT = Mathf.Clamp01((speed - 6f) / 6f);                 // 0 = دویدن آروم، 1 = اسپرینت
        float freq = Mathf.Lerp(.9f, 3.0f, Mathf.Clamp01(speed / 12f));     // چرخه در ثانیه
        float prev = Mathf.Sin(mphase);
        mphase = Mathf.Repeat(mphase + 2f * Mathf.PI * freq * dt * (speed > 0.15f && air<.5f ? 1f : 0f),2f*Mathf.PI);
        float cur2 = Mathf.Sin(mphase);
        float amp = Mathf.Clamp01(speed / 3.5f) * (1f-air);
        ampS = Mathf.MoveTowards(ampS, amp, dt * 9f);
        if (air < .1f && ampS > 0.25f && (cur2 > 0f) != (prev > 0f) && onStep != null) onStep();

        GunSlot slot = (slots != null && slots.Length > 0) ? slots[cur] : null;
        Vector3 sh = poser.ShoulderMid; float L = Mathf.Max(0.3f, poser.ArmLen);
        float aimEff = Mathf.Max(aimT, reloadBlend);
        Vector3 aimPos = new Vector3(0f, sh.y - 0.08f, 0.66f * L);
        if (slot != null) aimPos.z = Mathf.Min(0.66f * L, 0.93f * L - Mathf.Abs(slot.lLocal.z));
        Vector3 carryPos = new Vector3(sh.x + 0.30f * poser.RightSign, sh.y - 0.55f, 0.30f);
        Vector3 gunPos = Vector3.Lerp(carryPos, aimPos, aimEff);
        gunPos.y -= .40f*crouch + landCompression;
        gunPos += new Vector3(Mathf.Sin(mphase)*.025f,Mathf.Cos(2*mphase)*.018f,0)*ampS*(1f-.75f*aimEff);
        Quaternion gunRot = Quaternion.Slerp(Quaternion.Euler(35f, 0f, 0f), Quaternion.identity, aimEff);
        Vector3 lExtra; float leftForce;
        ApplyGunFx(dt, ref gunPos, ref gunRot, out lExtra, out leftForce);
        gunMount.localPosition = gunPos;
        gunMount.localRotation = gunRot;

        Vector3 rl = slot != null ? slot.rLocal : Vector3.zero, ll = slot != null ? slot.lLocal + lExtra : Vector3.zero;
        poser.Pose(mphase, ampS, runT, aimEff, slot != null, gunPos, gunRot, rl, ll, leftForce, crouch, air, verticalSpeed, landCompression, moveLocal);
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        aimT = Mathf.MoveTowards(aimT, aiming ? 1f : 0f, dt * 12f);
        if(previousAir>.3f && air<previousAir) landCompression=Mathf.Max(landCompression,.10f*previousAir);
        landCompression=Mathf.MoveTowards(landCompression,0,dt*.55f); previousAir=air;
        UpdateFx(dt);
        if (poser != null) { UpdateModel(dt); return; }
        ampS = Mathf.MoveTowards(ampS, Mathf.Clamp01(speed / 6f)*(1f-air), dt * 6f);
        phase += speed * dt * 1.2f;

        float sn = Mathf.Sin(phase);
        if (air < .1f && ampS > 0.25f && (sn > 0f) != (lastSin > 0f) && onStep != null) onStep();
        lastSin = sn;
        float swing = sn * ampS * 45f;

        // پاها (زاویه‌ی مثبت = به عقب، منفی = به جلو)
        hipL.localRotation = Quaternion.Euler(swing, 0f, 0f);
        hipR.localRotation = Quaternion.Euler(-swing, 0f, 0f);
        kneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, -swing) * 1.1f, 0f, 0f);
        kneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, swing) * 1.1f, 0f, 0f);

        float tuck = Mathf.Lerp(20f,45f,Mathf.InverseLerp(-8f,6f,verticalSpeed));
        float drop=.40f*crouch+landCompression;
        hipL.localPosition=new Vector3(hipL.localPosition.x,.92f-drop,hipL.localPosition.z);
        hipR.localPosition=new Vector3(hipR.localPosition.x,.92f-drop,hipR.localPosition.z);
        hipL.localRotation=Quaternion.Euler(Mathf.Lerp(swing,-55f+swing*.3f,crouch)-tuck*air,0,0);
        hipR.localRotation=Quaternion.Euler(Mathf.Lerp(-swing,-55f-swing*.3f,crouch)-tuck*.6f*air,0,0);
        kneeL.localRotation=Quaternion.Euler(Mathf.Lerp(Mathf.Max(0f,-swing)*1.1f,100f,crouch)+65f*air,0,0);
        kneeR.localRotation=Quaternion.Euler(Mathf.Lerp(Mathf.Max(0f,swing)*1.1f,100f,crouch)+45f*air,0,0);
        // دست‌ها: تاب خوردن مخالف پا، یا نشانه‌گیری
        Quaternion idleL = Quaternion.Euler(-swing * 0.8f, 0f, 4f);
        Quaternion idleR = Quaternion.Euler(swing * 0.8f, 0f, -4f);
        shoulderL.localRotation = Quaternion.Slerp(idleL, Quaternion.Euler(-72f, 28f, 0f), aimT);
        shoulderR.localRotation = Quaternion.Slerp(idleR, Quaternion.Euler(-82f, -4f, 0f), aimT);
        float bend = -(8f + ampS * 30f);
        elbowL.localRotation = Quaternion.Slerp(Quaternion.Euler(bend, 0f, 0f), Quaternion.Euler(-12f, 0f, 0f), aimT);
        elbowR.localRotation = Quaternion.Slerp(Quaternion.Euler(bend, 0f, 0f), Quaternion.Euler(-8f, 0f, 0f), aimT);
        if (handsUp)
        {
            shoulderL.localRotation = Quaternion.Euler(-165f, 0f, 14f);
            shoulderR.localRotation = Quaternion.Euler(-165f, 0f, -14f);
            elbowL.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            elbowR.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        }

        // بالاتنه: نوسان و کمی خم شدن به جلو موقع دویدن
        body.localPosition = new Vector3(0f, 0.92f + Mathf.Abs(sn) * 0.04f * ampS-drop, 0f);
        body.localRotation = Quaternion.Euler(ampS * 5f+15f*crouch+8f*air, 0f, 0f);

        // اسلحه: پایین کنار کمر ←→ بالا جلوی سینه
        float aimEff2 = Mathf.Max(aimT, reloadBlend);
        Vector3 gp = Vector3.Lerp(gunLow, gunAim, aimEff2);
        Quaternion gr = Quaternion.Euler(Mathf.Lerp(50f, 0f, aimEff2), 0f, 0f);
        Vector3 lx; float lf;
        ApplyGunFx(dt, ref gp, ref gr, out lx, out lf);
        gunMount.localPosition = gp;
        gunMount.localRotation = gr;
    }
}


// پوکه‌ی فشنگ: پرتاب میشه، رو زمین پرش میکنه و صدای ریز میده
public class ShellCasing : MonoBehaviour
{
    Vector3 vel, spin;
    float groundY, life;
    int bounces;
    static Material mat;
    static Mesh mesh;

    public static void Spawn(Vector3 pos, Vector3 vel, float groundY)
    {
        if (mat == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh = t.GetComponent<MeshFilter>().sharedMesh;
            mat = new Material(t.GetComponent<Renderer>().sharedMaterial);
            Destroy(t);
            mat.color = new Color(0.88f, 0.66f, 0.24f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.9f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.7f);
        }
        var go = new GameObject("Shell");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.012f, 0.022f, 0.012f);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        var sc = go.AddComponent<ShellCasing>();
        sc.vel = vel; sc.groundY = groundY + 0.015f;
        sc.spin = new Vector3(Random.Range(-720f, 720f), Random.Range(-720f, 720f), Random.Range(-720f, 720f));
        Destroy(go, 3f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        life += dt;
        if (bounces >= 3) return;
        vel.y -= 9.8f * dt;
        transform.position += vel * dt;
        transform.Rotate(spin * dt, Space.Self);
        if (transform.position.y <= groundY && vel.y < 0f)
        {
            var p = transform.position; p.y = groundY; transform.position = p;
            vel = new Vector3(vel.x * 0.45f, -vel.y * 0.38f, vel.z * 0.45f);
            spin *= 0.5f;
            if (bounces == 0 && GameAudio.I != null) GameAudio.I.Shell(p);
            bounces++;
            if (vel.y < 0.4f) bounces = 3;
        }
    }
}
