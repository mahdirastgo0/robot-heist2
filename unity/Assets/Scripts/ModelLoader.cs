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
