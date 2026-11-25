using System.Collections.Generic;
using UnityEngine;

// نام استخوان‌های هر مدل
public class RigMap
{
    public string pelvis, head;
    public string[] thigh = new string[2], calf = new string[2], foot = new string[2], toe = new string[2];   // 0 = چپ، 1 = راست
    public string[] upper = new string[2], lower = new string[2], hand = new string[2];

    public static RigMap Survival()
    {
        return new RigMap
        {
            pelvis = "pelvis", head = "head",
            thigh = new[] { "thigh_l", "thigh_r" }, calf = new[] { "calf_l", "calf_r" },
            foot = new[] { "foot_l", "foot_r" }, toe = new[] { "ball_l", "ball_r" },
            upper = new[] { "upperarm_l", "upperarm_r" }, lower = new[] { "lowerarm_l", "lowerarm_r" }, hand = new[] { "hand_l", "hand_r" }
        };
    }

    public static RigMap Police()
    {
        return new RigMap
        {
            pelvis = "Benjamin_Hips", head = "Benjamin_Head",
            thigh = new[] { "Benjamin_L_Thigh", "Benjamin_R_Thigh" }, calf = new[] { "Benjamin_L_Calf", "Benjamin_R_Calf" },
            foot = new[] { "Benjamin_L_Foot", "Benjamin_R_Foot" }, toe = new[] { "Benjamin_L_Toe", "Benjamin_R_Toe" },
            upper = new[] { "Benjamin_L_UpperArm", "Benjamin_R_UpperArm" }, lower = new[] { "Benjamin_L_Forearm", "Benjamin_R_Forearm" },
            hand = new[] { "Benjamin_L_Hand", "Benjamin_R_Hand" }
        };
    }
}

// حرکت دادن استخوان‌های مدل بدون انیمیشن آماده (این مدل‌ها هیچ انیمیشن راه رفتنی ندارن).
// هر استخوان با «جهت» تنظیم میشه (نه محورهای محلی خودش)، پس برای هر اسکلتی جواب میده.
// دست‌ها با IK دو‌استخوانی روی دستگیره‌های اسلحه میشینن.
public class BonePoser
{
    class B
    {
        public Transform t;
        public Quaternion rest;   // جهت استخوان در حالت اولیه (نسبت به بدنه)
        public Vector3 dir;       // جهت استخوان به سمت بچه‌ش
        public Vector3 pos;
        public float len;         // طول استخوان
        public float side;        // +1 راست، -1 چپ
    }

    Transform wr;
    B pelvis;
    readonly B[] thigh = new B[2], calf = new B[2], foot = new B[2], upper = new B[2], lower = new B[2];
    public Vector3 ShoulderMid { get; private set; }
    public float ArmLen { get; private set; }
    public float RightSign { get; private set; }
    public int FoundCount { get; private set; }
    public bool Ok { get; private set; }

    public static Dictionary<string, Transform> Index(Transform model)
    {
        var d = new Dictionary<string, Transform>();
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (!d.ContainsKey(t.name)) d[t.name] = t;
        return d;
    }

    // اول اسم دقیق، بعد جستجوی تقریبی (حروف کوچک/بزرگ، _l/_r/left/right)
    static Transform Resolve(Dictionary<string, Transform> d, string exact, int side, params string[] keywords)
    {
        Transform t;
        if (exact != null && d.TryGetValue(exact, out t)) return t;
        foreach (var kv in d)
        {
            string k = kv.Key.ToLowerInvariant();
            if (k.Contains("twist") || k.Contains("corrective") || k.StartsWith("ik_") || k.Contains("floor") || k.Contains("weapon")) continue;
            bool kw = false;
            foreach (var w in keywords) if (k.Contains(w)) { kw = true; break; }
            if (!kw) continue;
            if (side < 0) return kv.Value;
            bool left = k.EndsWith("_l") || k.EndsWith(".l") || k.Contains("_l_") || k.Contains("left");
            bool right = k.EndsWith("_r") || k.EndsWith(".r") || k.Contains("_r_") || k.Contains("right");
            if ((side == 0 && left) || (side == 1 && right)) return kv.Value;
        }
        return null;
    }

    B Make(Transform t, Transform child)
    {
        if (t == null) return null;
        FoundCount++;
        var b = new B { t = t };
        b.rest = Quaternion.Inverse(wr.rotation) * t.rotation;
        b.pos = wr.InverseTransformPoint(t.position);
        b.side = b.pos.x >= 0f ? 1f : -1f;
        if (child != null)
        {
            Vector3 v = wr.InverseTransformVector(child.position - t.position);
            b.len = v.magnitude;
            b.dir = v.normalized;
        }
        return b;
    }

    // جهت جلوی مدل از روی انگشت پا (مچ پا → انگشت). true یعنی باید ۱۸۰ درجه چرخونده بشه
    public static bool NeedsFlip(Transform wrapper, Transform model, RigMap m)
    {
        var d = Index(model);
        Transform foot = Resolve(d, m.foot[0], 0, "foot"), toe = Resolve(d, m.toe[0], 0, "ball", "toe");
        if (foot != null && toe != null)
        {
            Vector3 v = toe.position - foot.position; v.y = 0f;
            if (v.sqrMagnitude > 1e-8f) return Vector3.Dot(v.normalized, wrapper.forward) < 0f;
        }
        return false;
    }

    public bool Init(Transform wrapper, Transform model, RigMap m)
    {
        wr = wrapper; FoundCount = 0;
        var d = Index(model);
        pelvis = Make(Resolve(d, m.pelvis, -1, "pelvis", "hips"), null);
        for (int i = 0; i < 2; i++)
        {
            Transform th = Resolve(d, m.thigh[i], i, "thigh", "upleg", "upperleg");
            Transform ca = Resolve(d, m.calf[i], i, "calf", "shin", "lowerleg", "lowleg");
            Transform fo = Resolve(d, m.foot[i], i, "foot");
            Transform up = Resolve(d, m.upper[i], i, "upperarm", "uparm");
            Transform lo = Resolve(d, m.lower[i], i, "lowerarm", "forearm", "lowarm");
            Transform ha = Resolve(d, m.hand[i], i, "hand");
            thigh[i] = Make(th, ca); calf[i] = Make(ca, fo); foot[i] = Make(fo, null);
            upper[i] = Make(up, lo); lower[i] = Make(lo, ha);
        }
        // اندازه‌ی بدن: وسط شونه‌ها و طول دست (برای جای دادن اسلحه)
        if (upper[0] != null && upper[1] != null)
        {
            ShoulderMid = (upper[0].pos + upper[1].pos) * 0.5f;
            ArmLen = 0.5f * (upper[0].len + lower[0].len + upper[1].len + lower[1].len);
            RightSign = upper[1].side;
        }
        else { ShoulderMid = new Vector3(0f, 1.45f, 0f); ArmLen = 0.6f; RightSign = 1f; }
        Ok = true;
        for (int i = 0; i < 2; i++) Ok &= thigh[i] != null && calf[i] != null && upper[i] != null && lower[i] != null;
        if (!Ok) Debug.LogWarning("[BonePoser] bones found: " + FoundCount + "/11 - some limbs will not animate.");
        return Ok;
    }

    // حرکت: phase = فاز چرخه‌ی دویدن (رادیان)، amp = 0..1، runT = 0 (دو آروم) تا 1 (اسپرینت)، aimT = نشانه‌گیری
    // gunPos/gunRot (فضای بدنه) و دستگیره‌های اسلحه برای IK دست‌ها
    public void Pose(float phase, float amp, float runT, float aimT, bool hasGun, Vector3 gunPos, Quaternion gunRot, Vector3 rLocal, Vector3 lLocal, float leftForce = 0f, float crouch = 0f, float air = 0f, float verticalSpeed = 0f, float landing = 0f, Vector3 moveLocal = default(Vector3))
    {
        float hipAmp = Mathf.Lerp(0.70f, 1.0f, runT) * amp;                 // ~40° تا ~57°
        float side=moveLocal.sqrMagnitude>.01f?moveLocal.normalized.x:0f;
        float fore=moveLocal.sqrMagnitude>.01f?moveLocal.normalized.z:1f;
        float lean = runT * 12f * amp + crouch * 18f + air * 8f;                                        // خم شدن به جلو
        float bob = (0.5f + 0.5f * Mathf.Cos(2f * phase)) * Mathf.Lerp(0.03f, 0.06f, runT) * amp;

        if (pelvis != null && pelvis.t != null)
        {
            pelvis.t.position = wr.TransformPoint(pelvis.pos + Vector3.up * (bob*(1f-air)-.40f*crouch-landing));
            pelvis.t.rotation = wr.rotation * Quaternion.Euler(lean, Mathf.Sin(phase)*3f*amp*(1f-aimT), -side*7f*amp) * pelvis.rest;
        }

        for (int i = 0; i < 2; i++)
        {
            float ph = i == 0 ? phase : phase + Mathf.PI;                    // پای چپ و راست نیم‌چرخه فاصله دارن
            float ang = Mathf.Sin(ph) * hipAmp;
            // زانو وقتی پا به جلو میاد (نیمه‌ی بازگشت) خیلی خم میشه
            float bend = (Mathf.Max(0f, Mathf.Cos(ph)) * Mathf.Lerp(0.55f, 1.55f, runT) + 0.08f) * amp;
            ang=Mathf.Lerp(ang,.82f+ang*.25f,crouch);
            bend=Mathf.Lerp(bend,1.65f+bend*.2f,crouch);
            float tuck=Mathf.Lerp(.25f,.85f,Mathf.InverseLerp(-8f,6f,verticalSpeed));
            ang=Mathf.Lerp(ang,tuck*(i==0?1f:.65f),air);
            bend=Mathf.Lerp(bend,(i==0?1.15f:.8f),air);
            Leg(i, ang*Mathf.Lerp(1f,fore,aimT*(1f-air)), bend, side*Mathf.Sin(ph)*hipAmp*aimT*(1f-air));

            float swing = -Mathf.Sin(ph) * hipAmp * 0.95f;                    // دست مخالف پای هم‌طرف
            float eb = swing + (Mathf.Lerp(0.35f, 1.5f, runT) * amp + 0.1f);  // آرنج خم
            Arm(i, swing, eb, aimT, hasGun, gunPos, gunRot, rLocal, lLocal, leftForce);
        }
    }

    void Leg(int i, float ang, float bend, float lateral)
    {
        if (thigh[i] == null) return;
        Aim(thigh[i], new Vector3(thigh[i].dir.x+lateral, -Mathf.Cos(ang), Mathf.Sin(ang)));
        if (calf[i] != null)
        {
            float ca = ang - bend;
            Aim(calf[i], new Vector3(calf[i].dir.x+lateral*.65f, -Mathf.Cos(ca), Mathf.Sin(ca)));
        }
        if (foot[i] != null && foot[i].t != null) foot[i].t.rotation = wr.rotation * foot[i].rest;   // کف پا صاف
    }

    void Arm(int i, float swing, float eb, float aimT, bool hasGun, Vector3 gunPos, Quaternion gunRot, Vector3 rLocal, Vector3 lLocal, float leftForce)
    {
        if (upper[i] == null || lower[i] == null) return;
        float side = upper[i].side;
        Vector3 upDir = new Vector3(side * 0.12f, -Mathf.Cos(swing), Mathf.Sin(swing)).normalized;
        Vector3 loDir = new Vector3(side * 0.06f, -Mathf.Cos(eb), Mathf.Sin(eb)).normalized;

        if (hasGun && upper[i].len > 0.05f && lower[i].len > 0.05f)
        {
            // دست راست همیشه اسلحه رو نگه میداره؛ دست چپ فقط موقع نشانه‌گیری روی اسلحه میره
            float w = side > 0f ? 1f : Mathf.Max(aimT * aimT, leftForce);
            if (w > 0.01f)
            {
                Vector3 local = side > 0f ? rLocal : lLocal;
                Vector3 target = gunPos + gunRot * local - (gunRot * Vector3.forward) * 0.06f;   // مچ، یکم عقب‌تر از کف دست
                Vector3 S = wr.InverseTransformPoint(upper[i].t.position);
                Vector3 dU, dL;
                Ik(S, target, upper[i].len, lower[i].len, side, out dU, out dL);
                upDir = Vector3.Slerp(upDir, dU, w);
                loDir = Vector3.Slerp(loDir, dL, w);
            }
        }
        Aim(upper[i], upDir);
        Aim(lower[i], loDir);
    }

    // IK دو‌استخوانی: شونه S، هدف T، طول بازو L1 و ساعد L2 → جهت بازو و ساعد
    static void Ik(Vector3 S, Vector3 T, float L1, float L2, float side, out Vector3 dU, out Vector3 dL)
    {
        Vector3 v = T - S;
        float d = v.magnitude;
        float dc = Mathf.Clamp(d, Mathf.Abs(L1 - L2) * 1.001f + 0.001f, (L1 + L2) * 0.999f);
        Vector3 u = d > 1e-5f ? v / d : Vector3.forward;
        float a = (L1 * L1 - L2 * L2 + dc * dc) / (2f * dc);
        float h = Mathf.Sqrt(Mathf.Max(0f, L1 * L1 - a * a));
        Vector3 hint = new Vector3(side * 0.6f, -1f, -0.3f);          // آرنج پایین و کمی بیرون
        Vector3 pole = hint - Vector3.Dot(hint, u) * u;
        pole = pole.sqrMagnitude > 1e-6f ? pole.normalized : Vector3.down;
        Vector3 elbow = S + u * a + pole * h;
        dU = (elbow - S).normalized;
        dL = (S + u * dc - elbow).normalized;
    }

    void Aim(B b, Vector3 dir)
    {
        if (b == null || b.t == null || b.dir.sqrMagnitude < 0.01f) return;
        b.t.rotation = wr.rotation * (Quaternion.FromToRotation(b.dir, dir.normalized) * b.rest);
    }
}
