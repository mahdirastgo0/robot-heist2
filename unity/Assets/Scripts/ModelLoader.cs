using System.Collections.Generic;
using UnityEngine;

// بارگذاری مدل‌های FBX از Assets/Resources/Models و درست کردن متریال‌ها
public static class ModelLoader
{
    const string PlayerPath = "Models/Player/Survival_Character";
    const string PolicePath = "Models/Police/Police";
    const string PlayerTex = "Models/Player/Textures/";
    const string PoliceTex = "Models/Police/Textures/";

    static bool loaded;
    static GameObject playerPrefab, policePrefab;
    static readonly Dictionary<string, Material> policeCache = new Dictionary<string, Material>();

    public static string Status = "";   // وضعیت مدل‌ها (تو منو نشون داده میشه)

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        playerPrefab = Resources.Load<GameObject>(PlayerPath);
        policePrefab = Resources.Load<GameObject>(PolicePath);
    }

    public static bool HasCharacter(bool police) { Load(); return (police ? policePrefab : playerPrefab) != null; }

    // ---------------------------------------------------------------- Character
    // مدل رو زیر wrapper میسازه، قدش رو تنظیم می‌کنه، جهتش رو درست می‌کنه و متریال میده
    public static Transform SpawnCharacter(Transform wrapper, bool police, float height, RigMap map)
    {
        Load();
        var prefab = police ? policePrefab : playerPrefab;
        if (prefab == null) return null;

        var model = Object.Instantiate(prefab, wrapper).transform;
        foreach (var a in model.GetComponentsInChildren<Animator>(true)) Object.Destroy(a);
        foreach (var a in model.GetComponentsInChildren<Animation>(true)) Object.Destroy(a);

        // رو به جلو بودن: از روی جهت انگشت پا
        if (BonePoser.NeedsFlip(wrapper, model, map)) model.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.localRotation;

        // قد و جای پا
        Bounds b = RendererBounds(model);
        if (b.size.y > 0.001f)
        {
            float s = height / b.size.y;
            model.localScale *= s;
            b = RendererBounds(model);
            model.position += Vector3.up * (wrapper.position.y - b.min.y);
        }

        if (police) ApplyPolice(model); else ApplySurvival(model);
        return model;
    }

    // Uploaded legacy FBX has several biped copies. Only use a complete, bound skeleton.
    public static Transform TrySpawnUploadedPlayer(Transform wrapper,float height,out RigMap map)
    {
        map=null;
        var prefab=Resources.Load<GameObject>("ExternalArt/People/CustomPlayer/CustomPlayer");
        if(prefab==null)return null;
        var model=Object.Instantiate(prefab,wrapper,false).transform;
        try
        {
            var index=BonePoser.Index(model);
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var bound=new HashSet<Transform>();
            foreach(var skin in skins) if(skin.sharedMesh!=null) foreach(var bone in skin.bones) if(bone!=null)bound.Add(bone);
            Transform pelvis=null;int best=-1;
            foreach(string suffix in new[]{"001","001_ncl1_1","","_ncl1_1"})
            {
                string[] Pair(string part){return new[]{"Bip001 L "+part+suffix,"Bip001 R "+part+suffix};}
                var candidate=new RigMap{pelvis="Bip001 Pelvis"+suffix,head="Bip001 Head"+suffix,thigh=Pair("Thigh"),calf=Pair("Calf"),foot=Pair("Foot"),toe=suffix.StartsWith("001")?Pair("Toe"):Pair("Toe0"),upper=Pair("UpperArm"),lower=Pair("Forearm"),hand=Pair("Hand")};
                var required=new List<string>{candidate.pelvis,candidate.head};
                foreach(var names in new[]{candidate.thigh,candidate.calf,candidate.foot,candidate.upper,candidate.lower,candidate.hand})required.AddRange(names);
                bool complete=true;int score=0;
                foreach(var name in required){if(!index.TryGetValue(name,out Transform bone)){complete=false;break;}if(bound.Contains(bone))score++;}
                if(!complete||score<8||score<=best)continue;
                pelvis=index[candidate.pelvis];map=candidate;best=score;
            }
            if(map==null||pelvis==null)throw new System.InvalidOperationException("No complete bound biped found");
            // Match only the selected skeleton; do not render overlapping alternate body copies.
            int visible=0;
            foreach(var skin in skins)
            {
                bool match=false;
                foreach(var bone in skin.bones)if(bone!=null&&(bone==pelvis||bone.IsChildOf(pelvis))){match=true;break;}
                if(skin.name.ToLowerInvariant().Contains("eye_shell"))match=false;
                skin.enabled=match&&skin.sharedMesh!=null&&skin.gameObject.activeInHierarchy;
                if(skin.enabled)visible++;
            }
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))renderer.enabled=false;
            if(visible==0)throw new System.InvalidOperationException("No visible skin on chosen rig");
            foreach(var a in model.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(var a in model.GetComponentsInChildren<Animation>(true))a.enabled=false;
            if(BonePoser.NeedsFlip(wrapper,model,map))model.localRotation=Quaternion.Euler(0,180,0)*model.localRotation;
            Bounds b=RendererBounds(model);
            if(b.size.y<.001f||float.IsNaN(b.size.y)||float.IsInfinity(b.size.y))throw new System.InvalidOperationException("Invalid character height");
            model.localScale*=height/b.size.y;b=RendererBounds(model);
            model.position+=wrapper.position-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var skin in skins)if(skin.enabled)
            {
                var materials=skin.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var src=materials[i];var m=src!=null?new Material(src):new Material(Shader.Find("Standard"));
                    string n=(skin.name+" "+(src!=null?src.name:"")).ToLowerInvariant();
                    if(m.mainTexture==null)
                    {
                        Color col=n.Contains("jeans")?new Color(.16f,.22f,.32f):n.Contains("shirt")||n.Contains("cloth")?new Color(.32f,.13f,.11f):n.Contains("shoe")||n.Contains("boot")?new Color(.085f,.08f,.075f):n.Contains("eye")?new Color(.78f,.76f,.72f):new Color(.36f,.23f,.17f);
                        SetCol(m,col);
                    }
                    Matte(m);materials[i]=m;
                }
                skin.sharedMaterials=materials;skin.updateWhenOffscreen=true;
            }
            return model;
        }
        catch(System.Exception e){model.gameObject.SetActive(false);Object.Destroy(model.gameObject);map=null;Debug.LogWarning("Uploaded player fallback: "+e.Message);return null;}
    }

    static Bounds RendererBounds(Transform t)
    {
        bool first = true;
        Bounds b = new Bounds(t.position, Vector3.zero);
        foreach (var r in t.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // ---------------------------------------------------------------- Materials
    static Texture2D Tex(string path) { return Resources.Load<Texture2D>(path); }

    static void SetMain(Material m, Texture2D tex)
    {
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
    }
    static void SetCol(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
    static void SetNormal(Material m, Texture2D n)
    {
        if (n == null || !m.HasProperty("_BumpMap")) return;
        m.SetTexture("_BumpMap", n);
        m.EnableKeyword("_NORMALMAP");
    }
    static void Matte(Material m)
    {
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
    }
    static void Cutout(Material m)
    {
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
        if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;
    }

    static Material Clone(Material src)
    {
        if (src == null) src = new Material(Shader.Find("Standard"));
        var m = new Material(src);
        m.name = src.name;
        SetCol(m, Color.white);
        Matte(m);
        return m;
    }

    // --- پلیس: تکسچرهای Benjamin
    static void ApplyPolice(Transform model)
    {
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            string rn = r.name;
            if (rn.Contains("Tongue") || rn.Contains("Teeth")) { r.enabled = false; continue; }
            var src = r.sharedMaterials;
            var outm = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                string key = rn + "/" + i;
                Material m;
                if (!policeCache.TryGetValue(key, out m) || m == null)
                {
                    m = BuildPoliceMaterial(rn, src[i] != null ? src[i].name : "", src[i]);
                    policeCache[key] = m;
                }
                outm[i] = m;
            }
            r.sharedMaterials = outm;
        }
    }

    static Material BuildPoliceMaterial(string rend, string mat, Material src)
    {
        var m = Clone(src);
        string diff = null, norm = null; bool cut = false;
        if (mat.Contains("Pants")) diff = "Pants_0_Diffuse";
        else if (mat.Contains("Shirt")) { diff = "Shirt_0_Diffuse_A"; cut = true; }
        else if (mat.Contains("RL_Shoes")) { diff = "RL_Shoes_Diffuse"; norm = "RL_Shoes_Normal"; }
        else if (mat.Contains("Cavity")) diff = "RL_Cavity_Diffuse";
        else if (mat.Contains("Hate")) { diff = "Hate_Diffuse_A"; cut = true; }
        else if (mat.Contains("Hat")) { diff = "Hat_Diffuse_A"; norm = "Hat_Normal"; cut = true; }
        else if (mat.Contains("Hair")) { diff = "RL_Hair_Transparency_Diffuse_A"; cut = true; }
        else if (mat.Contains("Iris")) diff = rend.Contains("L_Eye") ? "RL_Iris_Diffuse_0001" : "RL_Iris_Diffuse";
        else if (mat.Contains("Skin"))
        {
            if (rend.Contains("Face")) diff = "RL_Skin_Diffuse_0002";
            else if (rend.Contains("Upper") || rend.Contains("Hands")) { diff = "RL_Skin_Diffuse_0001"; norm = "RL_Skin_Normal_0001"; }
            else { diff = "RL_Skin_Diffuse"; norm = "RL_Skin_Normal"; }
        }
        var t = diff != null ? Tex(PoliceTex + diff) : null;
        if (t != null) SetMain(m, t); else SetCol(m, new Color(0.8f, 0.65f, 0.55f));
        SetNormal(m, norm != null ? Tex(PoliceTex + norm) : null);
        if (cut) Cutout(m);
        return m;
    }

    // --- کاراکتر Survival: تکسچرها داخل FBX نیست، پس رنگ ساده؛ اگه فایل‌ها رو بذاری تو Resources/Models/Player/Textures استفاده میشن
    static void ApplySurvival(Transform model)
    {
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            var src = r.sharedMaterials;
            var outm = new Material[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                var m = Clone(src[i]);
                string n = src[i] != null ? src[i].name : "";
                string baseTex = null, normTex = null;
                Color col = new Color(0.55f, 0.55f, 0.55f);

                if (n.Contains("Jacket")) { col = new Color(0.28f, 0.33f, 0.2f); baseTex = "Jacket_BaseColor"; normTex = "Jacket_Normal"; }
                else if (n.Contains("Jeans")) { col = new Color(0.17f, 0.23f, 0.36f); baseTex = "Jeans_BaseColor"; normTex = "Jeans_Normal"; }
                else if (n.Contains("Shoes")) { col = new Color(0.1f, 0.09f, 0.08f); baseTex = "Shoes_BaseColor"; normTex = "Shoes_Normal"; }
                else if (n.Contains("Gloves")) { col = new Color(0.08f, 0.08f, 0.08f); baseTex = "Gloves_BaseColor"; normTex = "Gloves_Normal"; }
                else if (n.Contains("Backpack")) { col = new Color(0.55f, 0.35f, 0.15f); baseTex = "Backpack_BaseColor"; normTex = "Backpack_Normal"; }
                else if (n.Contains("Hair")) { col = new Color(0.22f, 0.14f, 0.08f); baseTex = "HairBrown_Diffuse_2"; normTex = "HairBrown_Normal"; }
                else if (n.Contains("Brows")) { col = new Color(0.12f, 0.08f, 0.05f); baseTex = "Eyebrows_BaseColor"; }
                else if (n.Contains("Mouth")) col = new Color(0.45f, 0.18f, 0.18f);
                else if (n.Contains("Eye")) { col = new Color(0.12f, 0.08f, 0.05f); baseTex = "darkBrownEye_light_albedo_4k"; }
                else if (n == "Head" || n.Contains("Head")) { col = new Color(0.86f, 0.68f, 0.56f); baseTex = "Body_BaseColor.1001"; normTex = "Body_Normal.1001"; }
                else if (n.Contains("Body")) { col = new Color(0.86f, 0.68f, 0.56f); baseTex = "Body_BaseColor.1002"; normTex = "Body_Normal.1002"; }
                else if (n.Contains("Arms")) { col = new Color(0.86f, 0.68f, 0.56f); baseTex = "Body_BaseColor.1003"; normTex = "Body_Normal.1003"; }

                var t = baseTex != null ? Tex(PlayerTex + baseTex) : null;
                if (t != null) SetMain(m, t); else { SetMain(m, null); SetCol(m, col); }
                if (t != null && normTex != null) SetNormal(m, Tex(PlayerTex + normTex));
                outm[i] = m;
            }
            r.sharedMaterials = outm;
        }
    }
}
