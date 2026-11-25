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
    DriveCar car;
    public bool Driving => car != null;
    public float VehicleSpeedRatio => car!=null?car.SpeedRatio:0f;
    public float VehicleThrottle => car!=null?car.Throttle:0f;
    public int VehicleType => car!=null?car.Type:0;
    bool crouched;
    Vector3 lastDry, moveVelocity;
    float posture, jumpBuffer, groundGrace, cameraDistance = 4.2f;
    public bool IsCrouched => crouched;

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
        gm = g; yaw = startYaw; pitch = 12f; lastDry = transform.position;
        cc = GetComponent<CharacterController>();
        Human = GetComponentInChildren<Humanoid>();
        Human.onStep = () => { if (GameAudio.I != null && !Driving && !gm.InputBlocked && cc.enabled && cc.isGrounded) GameAudio.I.Step(); };
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
        if (gm == null || gm.InputBlocked) { if (Human != null) { Human.speed = 0f; Human.aiming = false; Human.moveLocal=Vector3.zero; } moveVelocity=Vector3.zero; jumpBuffer=groundGrace=0; return; }

        // دوربین
        Vector2 look = GameInput.Look();
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -25f, 60f);
        recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, dt * (3f + recoilPitch * 5f));
        recoilYaw = Mathf.MoveTowards(recoilYaw, 0f, dt * 4f);

        if (gm.State == GameState.City && GameInput.CarDown())
        {
            if (Driving)
            {
                if (Mathf.Abs(car.Speed)<2f && car.TryExit(out Vector3 exit))
                {
                    car=null; transform.position=exit; cc.enabled=true; Human.gameObject.SetActive(true); vy=-1f; lastDry=exit; moveVelocity=Vector3.zero;
                }
                return;
            }
            if (gm.personalCar!=null && gm.personalCar.Enter(this))
            {
                car=gm.personalCar; if(GameAudio.I!=null)GameAudio.I.StopSteps(); Human.speed=0f; Human.moveLocal=Vector3.zero; cc.enabled=false; Human.gameObject.SetActive(false); crouched=false; cc.height=1.8f; cc.center=Vector3.up*.9f; cc.stepOffset=.3f;
                posture=0; jumpBuffer=groundGrace=0; moveVelocity=Vector3.zero; reloadTimer=0; Human.CancelReload(); Human.crouch=0; Human.air=0; if(GameAudio.I!=null)GameAudio.I.ReloadStop();
                yaw=car.transform.eulerAngles.y; pitch=12f;
            }
        }
        if (Driving)
        {
            if(GameInput.HornDown()&&GameAudio.I!=null)GameAudio.I.VehicleHorn();
            float before=car.transform.eulerAngles.y;
            car.Drive(GameInput.Move(),GameInput.Brake(),dt);
            yaw+=Mathf.DeltaAngle(before,car.transform.eulerAngles.y);
            transform.SetPositionAndRotation(car.transform.position,car.transform.rotation);
            return;
        }
        // حرکت
        Vector2 mv = GameInput.Move();
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(mv.x, 0f, mv.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        bool firing = GameInput.Fire() && gm.State == GameState.City;
        if(GameInput.CrouchDown() && cc.isGrounded)
        {
            if(!crouched) crouched=true;
            else if(CanStand()) crouched=false;
        }
        bool aiming = gm.State==GameState.City && (GameInput.Aim() || firing || aimTimer>0f);
        bool sprint = GameInput.Sprint() && !aiming && !crouched;
        float speed = crouched ? 3.6f : aiming ? 5.8f : GameInput.Walk() ? 3.2f : sprint ? 12f : 8f;
        posture=Mathf.MoveTowards(posture,crouched?1f:0f,dt*6f);
        cc.height=Mathf.Lerp(1.8f,1.10f,posture); cc.center=Vector3.up*(cc.height*.5f); cc.stepOffset=Mathf.Lerp(.3f,.18f,posture);
        bool grounded=cc.isGrounded;
        jumpBuffer=GameInput.JumpDown()? .14f : Mathf.Max(0,jumpBuffer-dt);
        groundGrace=grounded && vy<=0 ? .10f : Mathf.Max(0,groundGrace-dt);
        if(grounded && vy<=0) vy=-2f; else vy-=23f*dt;
        if(jumpBuffer>0 && groundGrace>0 && !crouched && posture<.1f)
        { vy=7.4f; jumpBuffer=groundGrace=0; }
        float accel=grounded?(dir.sqrMagnitude<.01f?55f:48f):19f;
        moveVelocity=Vector3.MoveTowards(moveVelocity,dir*speed,accel*dt);
        Vector3 vel=moveVelocity; vel.y=vy;
        CollisionFlags flags=cc.Move(vel*dt);
        if((flags&CollisionFlags.Above)!=0 && vy>0)vy=0;
        if(gm.State==GameState.City)
        {
            if(cc.isGrounded && gm.DryLand(transform.position,.5f))lastDry=transform.position;
            if(transform.position.y<-.9f){cc.enabled=false; transform.position=lastDry+Vector3.up*.1f; cc.enabled=true; vy=-1f;moveVelocity=Vector3.zero;}
        }
        if (firing) aimTimer = 0.7f;
        if (Human != null)
        {
            Human.speed = GameManager.Flat(cc.velocity).magnitude;
            Human.aiming = aiming; Human.crouch=posture; Human.air=Mathf.MoveTowards(Human.air,cc.isGrounded?0f:1f,dt*8f); Human.verticalSpeed=vy;
        }

        // چرخش بدن
        if (aiming) transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0f,yaw,0f),1080f*dt);
        else if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(dir),900f*dt);

        if(Human!=null)Human.moveLocal=transform.InverseTransformDirection(GameManager.Flat(cc.velocity));

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

    bool CanStand()
    {
        Vector3 p=transform.position;
        foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.42f,p+Vector3.up*1.39f,.38f,~0,QueryTriggerInteraction.Ignore))
            if(!c.transform.IsChildOf(transform))return false;
        return true;
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
        if (GameAudio.I != null) GameAudio.I.Shot(weapon);
        Transform ct = gm.Cam.transform;
        Vector3 muzzle = Human != null ? Human.MuzzlePoint(GunLen[weapon]) : transform.position + Vector3.up * 1.4f;
        int pellets = Mathf.Max(1, (int)w.pellets);
        bool impactPlayed = false;

        for (int i = 0; i < pellets; i++)
        {
            Quaternion q = ct.rotation * Quaternion.Euler(Random.Range(-w.spread, w.spread), Random.Range(-w.spread, w.spread), 0f);
            Vector3 d = q * Vector3.forward;
            Vector3 end = ct.position + d * (w.range + 5f);
            if (GameManager.RayIgnore(transform, ct.position, d, w.range + 5f, out RaycastHit cameraHit)) end=cameraHit.point;
            // Camera chooses the target; muzzle ray determines the actual hit and range.
            Vector3 shot=end-muzzle;
            float shotRange=Mathf.Min(w.range,shot.magnitude+.03f);
            if(shot.sqrMagnitude>.001f)end=muzzle+shot.normalized*shotRange;
            if (shot.sqrMagnitude>.001f && GameManager.RayIgnore(transform,muzzle,shot.normalized,shotRange,out RaycastHit h))
            {
                end=h.point;
                var cop = h.collider.GetComponentInParent<RobotCop>();
                if (cop != null)
                {
                    bool head = h.point.y > cop.transform.position.y + 1.55f;   // تیر به سر = ضربه‌ی دو برابر
                    float damage=w.damage;
                    if(weapon==2) { float distance=Vector3.Distance(muzzle,h.point); damage*=Mathf.Lerp(1.85f,.55f,Mathf.InverseLerp(3f,25f,distance)); }
                    cop.TakeDamage(head ? damage * 2f : damage);
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
        if (Human != null) { Human.Fire(KickPow[weapon], weapon != 2); }   // شاتگان پوکه رو موقع پمپ نمیندازه
        recoilPitch = Mathf.Min(9f, recoilPitch + CamKick[weapon] * Random.Range(0.85f, 1.15f));
        recoilYaw += Random.Range(-0.35f, 0.35f) * CamKick[weapon] * 0.4f;
        gm.OnPlayerShot(transform.position);
    }

    void LateUpdate()
    {
        if (gm == null || gm.Cam == null) return;
        Quaternion rot = Quaternion.Euler(pitch - (GameOptions.CameraKick?recoilPitch:0f), yaw + (GameOptions.CameraKick?recoilYaw:0f), 0f);
        Vector3 pivot = transform.position + Vector3.up * (Driving?2.0f:Mathf.Lerp(1.6f,1.03f,posture)) + Quaternion.Euler(0f,yaw,0f)*Vector3.right*(Driving?0f:.55f);
        Vector3 back = rot * Vector3.back;
        bool aimView=!Driving && (GameInput.Aim() || aimTimer>0f) && gm.State==GameState.City;
        float dist = Driving?7.2f:aimView?3.15f:4.5f;
        float targetFov=Driving?GameOptions.Fov+5f:aimView?GameOptions.Fov-11f:Mathf.Lerp(GameOptions.Fov,GameOptions.Fov+8f,Mathf.InverseLerp(8f,12f,moveVelocity.magnitude));
        gm.Cam.fieldOfView=Mathf.Lerp(gm.Cam.fieldOfView,targetFov,1f-Mathf.Exp(-9f*Time.deltaTime));
        if (GameManager.RayIgnore(Driving?car.transform:transform, pivot, back, dist, out RaycastHit h)) dist = Mathf.Max(0.15f, h.distance - 0.25f);
        cameraDistance=dist<cameraDistance?dist:Mathf.Lerp(cameraDistance,dist,1f-Mathf.Exp(-12f*Time.deltaTime));
        gm.Cam.transform.SetPositionAndRotation(pivot + back * cameraDistance, rot);
    }
}
