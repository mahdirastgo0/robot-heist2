using UnityEngine;

// بازیکن سوم‌شخص: حرکت، دوربین پشت شونه، تیراندازی، جلیقه و سلامتی
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float health = 100f, armor, maxArmor, hurtTime;
    public int weapon;
    public bool Reloading => reloadTimer > 0f;
    public int Ammo => ammo[weapon];
    public Humanoid Human { get; private set; }

    GameManager gm;
    CharacterController cc;
    float yaw, pitch, vy, fireTimer, reloadTimer, aimTimer, sinceHit = 99f;
    readonly int[] ammo = new int[NativeCore.WeaponCount];
    float recoilPitch, recoilYaw;

    // لگد هر اسلحه: ضربه‌ی ظاهری، بالا رفتن دوربین (درجه)، سبک رلود (0 خشاب، 1 شاتگان، 2 کمربند)
    static readonly float[] KickPow = { 1.0f, 0.42f, 1.65f, 0.6f, 0.55f };
    static readonly float[] CamKick = { 1.5f, 0.55f, 4.8f, 0.95f, 0.8f };
    static readonly int[] ReloadStyle = { 0, 0, 1, 0, 2 };

    static readonly float[] GunLen = { 0.28f, 0.4f, 0.6f, 0.7f, 0.8f };
    static readonly Color[] GunCol =
    {
        new Color(0.25f, 0.25f, 0.28f), new Color(0.2f, 0.2f, 0.2f), new Color(0.45f, 0.3f, 0.15f),
        new Color(0.15f, 0.2f, 0.15f), new Color(0.1f, 0.1f, 0.12f)
    };

    public void Init(GameManager g, float startYaw)
    {
        gm = g; yaw = startYaw; pitch = 12f;
        cc = GetComponent<CharacterController>();
        Human = GetComponentInChildren<Humanoid>();
        Human.onStep = () => { if (GameAudio.I != null && gm.State != GameState.Menu) GameAudio.I.Step(); };
        Human.SetVest(NativeCore.Profile(NativeCore.Vest));
        maxArmor = NativeCore.ArmorOf(NativeCore.Profile(NativeCore.Vest));
        health = 100f; armor = maxArmor;
        weapon = 0;
        for (int i = 0; i < ammo.Length; i++) ammo[i] = (int)NativeCore.Weapons[i].mag;
        UpdateGunVisual();
    }

    public void RefreshLoadout()
    {
        maxArmor = NativeCore.ArmorOf(NativeCore.Profile(NativeCore.Vest));
        armor = maxArmor;
        if (Human != null) Human.SetVest(NativeCore.Profile(NativeCore.Vest));
    }

    void UpdateGunVisual()
    {
        if (Human == null) return;
        float len = GunLen[weapon];
        Human.gun.localScale = new Vector3(0.07f, 0.1f, len);
        Human.gun.localPosition = new Vector3(0f, 0f, len * 0.5f);
        Human.gun.GetComponent<Renderer>().material.color = GunCol[weapon];
        Human.ShowGun(weapon);
    }

    public void TakeDamage(float d)
    {
        if (health <= 0f || gm.State != GameState.City) return;
        hurtTime = 0.35f;
        sinceHit = 0f;
        if (GameAudio.I != null) GameAudio.I.Hurt();
        if (armor > 0f)
        {
            float a = Mathf.Min(armor, d);      // جلیقه همه‌ی ضربه رو می‌گیره، ولی ۲۵٪ ش رد میشه
            armor -= a;
            health -= (d - a) + a * 0.15f;
        }
        else health -= d;
        if (health <= 0f) { health = 0f; gm.PlayerDied(); }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (hurtTime > 0f) hurtTime -= dt;
        if (aimTimer > 0f) aimTimer -= dt;
        sinceHit += dt;
        if (gm != null && gm.State == GameState.City && health > 0f && health < 100f && sinceHit > 4f) health = Mathf.Min(100f, health + 6f * dt);   // سلامتی خودبه‌خود برمیگرده
        if (gm == null || gm.InputBlocked) { if (Human != null) { Human.speed = 0f; Human.aiming = false; } return; }

        // دوربین
        Vector2 look = GameInput.Look();
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -25f, 60f);
        recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, dt * (3f + recoilPitch * 5f));
        recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, dt * 4f);

        // حرکت
        Vector2 mv = GameInput.Move();
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(mv.x, 0f, mv.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        bool firing = GameInput.Fire() && gm.State == GameState.City;
        float speed = (GameInput.Sprint() && !firing) ? 9f : 5.5f;
        if (cc.isGrounded) vy = -1f; else vy -= 20f * dt;
        Vector3 vel = dir * speed; vel.y = vy;
        cc.Move(vel * dt);
        if (firing) aimTimer = 0.7f;
        if (Human != null)
        {
            Human.speed = GameManager.Flat(cc.velocity).magnitude;
            Human.aiming = aimTimer > 0f;
        }

        // چرخش بدن
        if (firing) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, yaw, 0f), 20f * dt);
        else if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * dt);

        // اسلحه
        int key = GameInput.WeaponKeyDown();
        if (key > 0) TrySwitch(key - 1);

        var w = NativeCore.Weapons[weapon];
        fireTimer -= dt;
        if (reloadTimer > 0f)
        {
            reloadTimer -= dt;
            if (reloadTimer <= 0f) ammo[weapon] = (int)w.mag;
        }
        else
        {
            if (GameInput.ReloadDown() && ammo[weapon] < (int)w.mag) StartReload(w);
            else if (firing && fireTimer <= 0f)
            {
                if (ammo[weapon] > 0) Shoot(w);
                else { if (GameAudio.I != null) GameAudio.I.Empty(); StartReload(w); }
            }
        }
    }

    void StartReload(NativeCore.WeaponStats w)
    {
        reloadTimer = w.reload;
        if (Human != null) Human.BeginReload(w.reload, ReloadStyle[weapon]);
        if (GameAudio.I != null) GameAudio.I.Reload(weapon);
    }

    void TrySwitch(int i)
    {
        if (i == weapon || i < 0 || i >= NativeCore.WeaponCount || !NativeCore.OwnsWeapon(i)) return;
        weapon = i; reloadTimer = 0f; fireTimer = 0.3f;
        if (Human != null) Human.CancelReload();
        if (GameAudio.I != null) { GameAudio.I.ReloadStop(); GameAudio.I.Equip(); }
        UpdateGunVisual();
    }

    void Shoot(NativeCore.WeaponStats w)
    {
        ammo[weapon]--;
        fireTimer = w.interval;
        Transform ct = gm.Cam.transform;
        Vector3 muzzle = Human != null ? Human.MuzzlePoint(GunLen[weapon]) : transform.position + Vector3.up * 1.4f;
        int pellets = Mathf.Max(1, (int)w.pellets);
        bool impactPlayed = false;

        for (int i = 0; i < pellets; i++)
        {
            Quaternion q = ct.rotation * Quaternion.Euler(Random.Range(-w.spread, w.spread), Random.Range(-w.spread, w.spread), 0f);
            Vector3 d = q * Vector3.forward;
            Vector3 end = ct.position + d * (w.range + 5f);
            if (GameManager.RayIgnore(transform, ct.position, d, w.range + 5f, out RaycastHit h))
            {
                end = h.point;
                var cop = h.collider.GetComponentInParent<RobotCop>();
                if (cop != null)
                {
                    bool head = h.point.y > cop.transform.position.y + 1.55f;   // تیر به سر = ضربه‌ی دو برابر
                    cop.TakeDamage(head ? w.damage * 2f : w.damage);
                    if (!impactPlayed && GameAudio.I != null) { GameAudio.I.ImpactBody(h.point); impactPlayed = true; }
                }
                else if (!impactPlayed && GameAudio.I != null)
                {
                    GameAudio.I.ImpactWall(h.point);
                    if (Random.value < 0.2f) GameAudio.I.Ricochet(h.point);
                    impactPlayed = true;
                }
            }
            gm.Tracer(muzzle, end, new Color(1f, 0.9f, 0.4f));
        }
        if (GameAudio.I != null) GameAudio.I.Shot(weapon);
        if (Human != null) { Human.Fire(KickPow[weapon], weapon != 2); }   // شاتگان پوکه رو موقع پمپ نمیندازه
        recoilPitch = Mathf.Min(9f, recoilPitch + CamKick[weapon] * Random.Range(0.85f, 1.15f));
        recoilYaw += Random.Range(-0.35f, 0.35f) * CamKick[weapon] * 0.4f;
        gm.OnPlayerShot(transform.position);
    }

    void LateUpdate()
    {
        if (gm == null || gm.Cam == null) return;
        Quaternion rot = Quaternion.Euler(pitch - recoilPitch, yaw + recoilYaw, 0f);
        Vector3 pivot = transform.position + Vector3.up * 1.6f + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 0.55f;
        Vector3 back = rot * Vector3.back;
        float dist = 4.2f;
        if (GameManager.RayIgnore(transform, pivot, back, dist, out RaycastHit h)) dist = Mathf.Max(0.6f, h.distance - 0.25f);
        gm.Cam.transform.SetPositionAndRotation(pivot + back * dist, rot);
    }
}
