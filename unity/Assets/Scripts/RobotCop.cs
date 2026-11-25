using System.Collections.Generic;
using UnityEngine;

// پلیس ربات: گشت، دیدن/شنیدن بازیکن، تعقیب با BFS (هسته‌ی C++)، تیراندازی
public class RobotCop : MonoBehaviour
{
    enum Mode { Patrol, Chase }

    GameManager gm;
    PlayerController player;
    Humanoid human;
    float curSpeed;
    bool aimNow;
    float hp = 70f;
    public bool IsRiot {get;private set;}
    public bool IsAlive => hp>0f;
    Mode mode = Mode.Patrol;
    readonly List<Vector2Int> path = new List<Vector2Int>();
    int idx;
    float repath, loseTimer, fireTimer, flash;
    Vector3 lastSeen;

    static readonly Color Calm = new Color(0.3f, 0.8f, 1f), Angry = new Color(1f, 0.15f, 0.1f);

    public void Init(GameManager g, PlayerController p, Humanoid h, bool riot=false)
    {
        gm = g; player = p; human = h;
        IsRiot=riot; hp = 45f + gm.Wanted * 6f + (riot?45f:0f);
        transform.rotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
        Paint(Calm);
    }

    public void TakeDamage(float d)
    {
        if(hp<=0f)return;
        hp -= d;
        flash = 0.08f;
        Paint(Color.white);
        if (player != null) { lastSeen = player.transform.position; SetChase(); loseTimer = 0f; }
        if (hp <= 0f)
        {
            if (GameAudio.I != null) GameAudio.I.CopDown(transform.position);
            foreach(var col in GetComponentsInChildren<Collider>())col.enabled=false;
            gm.CopKilled(this);
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (hp<=0f || gm == null || !gm.Running || player == null || player.health <= 0f) return;
        float dt = Time.deltaTime;
        curSpeed = 0f; aimNow = false;
        if (flash > 0f) { flash -= dt; if (flash <= 0f) Paint(mode == Mode.Chase ? Angry : Calm); }

        Vector3 pp = player.transform.position;
        float dist = GameManager.Flat(pp - transform.position).magnitude;
        bool sees = CanSee(pp, dist);
        bool heard = Time.time - gm.LastShotTime < 0.6f && GameManager.Flat(gm.LastShotPos - transform.position).magnitude < 25f;

        if (sees) { gm.NotifySeen(); lastSeen = pp; loseTimer = 0f; SetChase(); }
        else if (heard) { lastSeen = gm.LastShotPos; loseTimer = 0f; SetChase(); }
        else if (mode == Mode.Chase)
        {
            loseTimer += dt;
            if (loseTimer > 4f) { mode = Mode.Patrol; path.Clear(); idx = 0; Paint(Calm); }
        }

        if (mode == Mode.Chase)
        {
            if (sees && dist < 18f)
            {
                aimNow = true;
                Face(pp);
                fireTimer -= dt;
                if (fireTimer <= 0f) { Fire(pp, dist); fireTimer = Random.Range(0.9f, 1.6f); }
            }
            bool hold = sees && dist < 9f;
            if (!hold)
            {
                repath -= dt;
                if (repath <= 0f)
                {
                    repath = 0.4f;
                    path.Clear();
                    path.AddRange(gm.FindPath(gm.WorldToCell(transform.position), gm.WorldToCell(lastSeen)));
                    idx = 0;
                }
                Follow((3.8f + gm.Wanted * 0.3f)*(IsRiot ? .85f : 1f), !(sees && dist < 18f));
            }
        }
        else
        {
            if (idx >= path.Count)
            {
                path.Clear();
                path.AddRange(gm.FindPath(gm.WorldToCell(transform.position), gm.RandomWalkCell()));
                idx = 0;
            }
            Follow(2.4f);
        }
        if (human != null) { human.speed = curSpeed; human.aiming = aimNow; }
    }

    // آلارم مغازه: پلیس مستقیم میره سمت اون نقطه
    public void Alert(Vector3 p)
    {
        lastSeen = p; loseTimer = 0f;
        SetChase();
        repath = 0f;
    }

    void SetChase()
    {
        if (mode == Mode.Chase) return;
        mode = Mode.Chase; repath = 0f; Paint(Angry);
    }

    bool CanSee(Vector3 pp, float dist)
    {
        if (dist > 20f) return false;
        Vector3 a = transform.position + Vector3.up * 1.6f;
        Vector3 d = (pp + Vector3.up * 1.2f) - a;
        if (GameManager.RayIgnore(transform, a, d.normalized, d.magnitude, out RaycastHit h))
            return h.collider.GetComponentInParent<PlayerController>() != null || (player.Driving && h.collider.GetComponentInParent<DriveCar>()!=null);
        return true;
    }

    void Fire(Vector3 pp, float dist)
    {
        float hitChance = Mathf.Clamp(0.55f - dist / 45f, 0.12f, 0.42f);
        Vector3 muzzle = human != null ? human.MuzzlePoint(0.3f) : transform.position + Vector3.up * 1.3f;
        Vector3 target = pp + Vector3.up * 1.2f;
        bool hit = Random.value < hitChance;
        Vector3 end = hit ? target : target + Random.insideUnitSphere * 1.5f;
        gm.Tracer(muzzle, end, new Color(1f, 0.3f, 0.2f));
        if (human != null) human.Fire(0.6f, false);
        if (GameAudio.I != null) GameAudio.I.CopShot(transform.position);
        if (hit) player.TakeDamage(3f + gm.Wanted * 0.8f);
    }

    void Face(Vector3 pp)
    {
        Vector3 d = GameManager.Flat(pp - transform.position);
        if (d.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
    }

    void Follow(float speed, bool turn = true)
    {
        if (idx >= path.Count) return;
        Vector3 wp = gm.CellToWorld(path[idx]);
        Vector3 d = GameManager.Flat(wp - transform.position);
        float step = speed * Time.deltaTime;
        curSpeed = speed;
        if (d.magnitude <= step) { transform.position = wp; idx++; }
        else transform.position += d.normalized * step;
        if (turn && d.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 10f * Time.deltaTime);
    }

    void Paint(Color c)
    {
        if (human != null && human.statusLight != null) human.statusLight.material.color = c;
    }
}
