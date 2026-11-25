using UnityEngine;

public struct HumanLook
{
    public Color skin, shirt, pants, shoes, hair;
    public bool cap, mask, glasses, badge, bag, riot;
}

// ساخت مدل آدم از شکل‌های ساده: پا، زانو، تنه، سر، دست، آرنج، کوله
public partial class GameManager
{
    static readonly Color[] SkinTones =
    {
        new Color(0.93f, 0.78f, 0.65f), new Color(0.8f, 0.6f, 0.45f), new Color(0.55f, 0.38f, 0.28f), new Color(0.38f, 0.26f, 0.2f)
    };

    GunSlot MakeSlot(Transform mount, string res, float scale, bool tint, Color tintColor, Vector3 aim)
    {
        GunData d = GunModels.Load(res);
        if (d == null) return null;
        var go = GunModels.Spawn(d, mount, scale, tint, tintColor);
        Transform P(string n, Vector3 p)
        {
            var t = new GameObject(n).transform;
            t.SetParent(go.transform, false);
            t.localPosition = p;
            return t;
        }
        return new GunSlot
        {
            go = go, muzzle = P("Muzzle", d.muzzle), rGrip = P("GripR", d.rightGrip), lGrip = P("GripL", d.leftGrip), aim = aim,
            rLocal = d.rightGrip * scale, lLocal = d.leftGrip * scale
        };
    }

    // اگه مدل‌های FBX داخل Resources/Models باشن ازشون استفاده میشه، وگرنه آدم ساده‌ی ساخته‌شده با کد
    Humanoid BuildModelHuman(Transform parent, bool police)
    {
        if (!ModelLoader.HasCharacter(police)) return null;
        var root = new GameObject("Human");
        root.transform.SetParent(parent, false);
        var hu = root.AddComponent<Humanoid>();
        RigMap map = police ? RigMap.Police() : RigMap.Survival();

        // The original Survival skeleton is the rig used by the procedural animation and grip IK.
        Transform model=ModelLoader.SpawnCharacter(root.transform,police,police?1.82f:1.85f,map);
        if(model==null){Destroy(root);return null;}
        var poser=new BonePoser();
        bool okBones=false;
        try{okBones=poser.Init(root.transform,model,map);}catch(System.Exception e){Debug.LogWarning("Character rig: "+e.Message);}
        if(!okBones){root.SetActive(false);Destroy(root);return null;}
        string st = (police ? "police " : "player ") + "bones " + poser.FoundCount + "/11" + (okBones ? " OK" : " MISSING");
        ModelLoader.Status = ModelLoader.Status.Contains(st) ? ModelLoader.Status : (ModelLoader.Status.Length > 0 ? ModelLoader.Status + "  |  " : "") + st;
        hu.poser = poser;

        // اسلحه: گلاک برای تپانچه و پلیس، مکعب ساده برای بقیه‌ی اسلحه‌ها
        var mount = new GameObject("GunMount").transform;
        mount.SetParent(root.transform, false);
        hu.gunMount = mount;
        hu.gunLow = new Vector3(0.26f, 0.95f, 0.14f);
        hu.gunAim = new Vector3(0f, 1.36f, 0.5f);
        var cube = Prim(PrimitiveType.Cube, "Gun", mount, new Vector3(0f, 0f, 0.15f), new Vector3(0.06f, 0.09f, 0.3f), new Color(0.15f, 0.15f, 0.17f));
        hu.gun = cube.transform;

        // اسلحه‌ها: Glock برای تپانچه و پلیس، HK416 (با اندازه و رنگ‌های متفاوت) برای بقیه
        var list = new System.Collections.Generic.List<GunSlot>();
        var pistol = ExternalGun(mount,0) ?? MakeSlot(mount, "Models/Gun/glock", 1f, false, Color.white, new Vector3(0f, 1.38f, 0.42f));
        if (pistol != null)
        {
            list.Add(pistol);
            if (!police)
            {
                var rifle = "Models/Gun/hk416";
                list.Add(ExternalGun(mount,1) ?? MakeCustomGun(mount,1));                        // SMG
                list.Add(ExternalGun(mount,2) ?? MakeCustomGun(mount,2));       // شاتگان (قهوه‌ای)
                list.Add(ExternalGun(mount,3) ?? MakeSlot(mount, rifle, 1.0f, false, Color.white, new Vector3(0f, 1.40f, 0.26f)));                        // Assault Rifle
                list.Add(ExternalGun(mount,4) ?? MakeCustomGun(mount,4));      // LMG
            }
            if (!police && list[3] == null) list[3] = MakeCustomGun(mount,4);   // اگه HK416 لود نشد فقط تپانچه
            hu.slots = list.ToArray();
        }
        hu.ShowGun(0);
        return hu;
    }

    Humanoid BuildHuman(Transform parent, HumanLook L, bool allowModel = true)
    {
        var modelHuman = allowModel ? BuildModelHuman(parent, L.badge) : null;   // badge = پلیس
        if (modelHuman != null) { if(L.riot) AddRiotGear(modelHuman); return modelHuman; }

        var root = new GameObject("Human");
        root.transform.SetParent(parent, false);
        var hu = root.AddComponent<Humanoid>();
        Transform r = root.transform;

        hu.hipL = MakeLeg(r, -0.1f, L, out hu.kneeL);
        hu.hipR = MakeLeg(r, 0.1f, L, out hu.kneeR);

        // بالاتنه (مبدأ = ارتفاع کمر)
        var body = new GameObject("Body").transform;
        body.SetParent(r, false);
        body.localPosition = new Vector3(0f, 0.92f, 0f);
        hu.body = body;

        Color dark = new Color(0.07f, 0.07f, 0.08f);
        Prim(PrimitiveType.Cube, "Pelvis", body, new Vector3(0f, 0.04f, 0f), new Vector3(0.34f, 0.16f, 0.2f), L.pants);
        Prim(PrimitiveType.Cube, "Torso", body, new Vector3(0f, 0.31f, 0f), new Vector3(0.42f, 0.5f, 0.22f), L.shirt);
        Prim(PrimitiveType.Cube, "Belt", body, new Vector3(0f, 0.1f, 0f), new Vector3(0.36f, 0.05f, 0.23f), dark);
        Prim(PrimitiveType.Sphere, "ShoulderL", body, new Vector3(-0.23f, 0.52f, 0f), Vector3.one * 0.17f, L.shirt);
        Prim(PrimitiveType.Sphere, "ShoulderR", body, new Vector3(0.23f, 0.52f, 0f), Vector3.one * 0.17f, L.shirt);
        Prim(PrimitiveType.Cylinder, "Neck", body, new Vector3(0f, 0.6f, 0f), new Vector3(0.09f, 0.05f, 0.09f), L.skin);

        // جلیقه (پیش‌فرض خاموش؛ بازیکن روشنش می‌کنه)
        hu.vest = Prim(PrimitiveType.Cube, "Vest", body, new Vector3(0f, 0.33f, 0f), new Vector3(0.46f, 0.44f, 0.27f), Color.gray);
        hu.vest.SetActive(false);

        // سر
        Color headCol = L.mask ? new Color(0.06f, 0.06f, 0.07f) : L.skin;
        Prim(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 0.74f, 0f), new Vector3(0.21f, 0.25f, 0.23f), headCol);
        if (L.mask)
        {   // کلاه‌کیپ: نوار پوست دور چشم‌ها
            Prim(PrimitiveType.Cube, "EyeStrip", body, new Vector3(0f, 0.755f, 0.1f), new Vector3(0.17f, 0.05f, 0.05f), L.skin);
        }
        else if (!L.cap)
        {
            Prim(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.79f, -0.012f), new Vector3(0.225f, 0.2f, 0.245f), L.hair);
        }
        else
        {
            Prim(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.78f, -0.02f), new Vector3(0.22f, 0.18f, 0.24f), L.hair);
        }
        // چشم‌ها
        Prim(PrimitiveType.Cube, "EyeL", body, new Vector3(-0.05f, 0.755f, 0.112f), new Vector3(0.035f, 0.025f, 0.02f), Color.black);
        Prim(PrimitiveType.Cube, "EyeR", body, new Vector3(0.05f, 0.755f, 0.112f), new Vector3(0.035f, 0.025f, 0.02f), Color.black);
        if (!L.mask) Prim(PrimitiveType.Cube, "Nose", body, new Vector3(0f, 0.72f, 0.118f), new Vector3(0.03f, 0.05f, 0.035f), L.skin * 0.92f);
        if (L.glasses) Prim(PrimitiveType.Cube, "Glasses", body, new Vector3(0f, 0.755f, 0.122f), new Vector3(0.19f, 0.045f, 0.02f), Color.black);
        if (L.cap)
        {
            Color capCol = L.shirt * 0.8f;
            Prim(PrimitiveType.Cylinder, "Cap", body, new Vector3(0f, 0.855f, 0f), new Vector3(0.235f, 0.035f, 0.245f), capCol);
            Prim(PrimitiveType.Cube, "Brim", body, new Vector3(0f, 0.835f, 0.14f), new Vector3(0.2f, 0.02f, 0.1f), capCol * 0.7f);
        }
        if (L.badge)
        {
            Prim(PrimitiveType.Cube, "Badge", body, new Vector3(0.1f, 0.42f, 0.115f), new Vector3(0.05f, 0.06f, 0.02f), new Color(1f, 0.85f, 0.2f));
            var led = Prim(PrimitiveType.Cube, "Radio", body, new Vector3(0.2f, 0.55f, 0.12f), new Vector3(0.04f, 0.04f, 0.03f), Color.cyan);
            hu.statusLight = led.GetComponent<Renderer>();
        }

        // دست‌ها
        hu.shoulderL = MakeArm(body, -1f, L, out hu.elbowL);
        hu.shoulderR = MakeArm(body, 1f, L, out hu.elbowR);

        // کوله‌پشتی
        if (L.bag)
        {
            var bag = new GameObject("Bag").transform;
            bag.SetParent(body, false);
            bag.localPosition = new Vector3(0f, 0.3f, -0.2f);
            Prim(PrimitiveType.Cube, "BagMain", bag, Vector3.zero, new Vector3(0.3f, 0.4f, 0.16f), new Color(0.9f, 0.5f, 0.1f));
            Prim(PrimitiveType.Cube, "BagPocket", bag, new Vector3(0f, -0.08f, -0.09f), new Vector3(0.22f, 0.14f, 0.05f), new Color(0.7f, 0.38f, 0.08f));
            hu.bag = bag;
        }

        // اسلحه (جلوی سینه وقتی نشانه می‌گیره)
        var mount = new GameObject("GunMount").transform;
        mount.SetParent(body, false);
        hu.gunMount = mount;
        var gun = Prim(PrimitiveType.Cube, "Gun", mount, new Vector3(0f, 0f, 0.15f), new Vector3(0.06f, 0.09f, 0.3f), new Color(0.15f, 0.15f, 0.17f));
        hu.gun = gun.transform;
        if (L.mask)
        {
            var pistol = ExternalGun(mount,0) ?? MakeSlot(mount,"Models/Gun/glock",1f,false,Color.white,Vector3.zero);
            if(pistol!=null)
            {
                var rifle=ExternalGun(mount,3)??MakeSlot(mount,"Models/Gun/hk416",1f,false,Color.white,Vector3.zero);
                hu.slots=new[]{pistol,ExternalGun(mount,1)??MakeCustomGun(mount,1),ExternalGun(mount,2)??MakeCustomGun(mount,2),rifle??MakeCustomGun(mount,4),ExternalGun(mount,4)??MakeCustomGun(mount,4)};
                hu.ShowGun(0);
            }
        }
        return hu;
    }

    Transform MakeLeg(Transform parent, float x, HumanLook L, out Transform knee)
    {
        var hip = new GameObject("Hip").transform;
        hip.SetParent(parent, false);
        hip.localPosition = new Vector3(x, 0.92f, 0f);
        Prim(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.22f, 0f), new Vector3(0.17f, 0.24f, 0.17f), L.pants);

        knee = new GameObject("Knee").transform;
        knee.SetParent(hip, false);
        knee.localPosition = new Vector3(0f, -0.45f, 0f);
        Prim(PrimitiveType.Capsule, "Shin", knee, new Vector3(0f, -0.21f, 0f), new Vector3(0.14f, 0.22f, 0.14f), L.pants);
        Prim(PrimitiveType.Cube, "Foot", knee, new Vector3(0f, -0.43f, 0.06f), new Vector3(0.13f, 0.08f, 0.28f), L.shoes);
        return hip;
    }

    Transform MakeArm(Transform body, float side, HumanLook L, out Transform elbow)
    {
        var sh = new GameObject("Shoulder").transform;
        sh.SetParent(body, false);
        sh.localPosition = new Vector3(0.25f * side, 0.5f, 0f);
        Prim(PrimitiveType.Capsule, "UpperArm", sh, new Vector3(0f, -0.14f, 0f), new Vector3(0.12f, 0.16f, 0.12f), L.shirt);

        elbow = new GameObject("Elbow").transform;
        elbow.SetParent(sh, false);
        elbow.localPosition = new Vector3(0f, -0.29f, 0f);
        Prim(PrimitiveType.Capsule, "Forearm", elbow, new Vector3(0f, -0.13f, 0f), new Vector3(0.1f, 0.15f, 0.1f), L.shirt);
        Prim(PrimitiveType.Sphere, "Hand", elbow, new Vector3(0f, -0.29f, 0f), Vector3.one * 0.1f, L.mask ? L.skin : L.skin);
        return sh;
    }
}
