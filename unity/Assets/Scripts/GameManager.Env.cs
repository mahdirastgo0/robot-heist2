using System.Collections.Generic;
using UnityEngine;

// محیط: آسمان (خورشید و ابر)، نور، مه و مواد با تکسچر کیفیت بالا (آسفالت، پیاده‌رو، آجر، بتن...)
public partial class GameManager
{
    // جهت خورشید؛ باید با اسکریپت ساخت آسمان (gensky.py) یکی باشه
    static readonly Vector3 SunDir = new Vector3(0.8365f, 0.5f, 0.2241f);
    readonly Dictionary<string, Material> texMats = new Dictionary<string, Material>();
    readonly Dictionary<string, Mesh> boxMeshes = new Dictionary<string, Mesh>();
    Light sunLight, interiorLight;
    Material glassMat;

    void SetupCameraAndLight()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        if(cam.GetComponent<AudioListener>()==null) cam.gameObject.AddComponent<AudioListener>();
        cam.orthographic = false;
        cam.fieldOfView = 65f;
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 1000f; cam.allowHDR=true; cam.allowMSAA=true;

        // آسمون: پانورامای ۳۶۰ درجه با خورشید و ابر
        var skyTex = Resources.Load<Texture2D>("Textures/sky_panorama");
        var skySh = Shader.Find("Skybox/Panoramic");
        if (skyTex != null && skySh != null)
        {
            var sm = new Material(skySh);
            sm.SetTexture("_MainTex", skyTex);
            if (sm.HasProperty("_Exposure")) sm.SetFloat("_Exposure", 1f);
            RenderSettings.skybox = sm;
            cam.clearFlags = CameraClearFlags.Skybox;
        }
        else
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.9f);
        }

        // خورشید (همون جهتی که تو تصویر آسمون هست)
        var old = GameObject.Find("Directional Light");
        if (old != null) old.SetActive(false);
        var lg = new GameObject("Sun");
        var l = lg.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.93f, 0.8f);
        l.intensity = 1.25f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.85f;
        lg.transform.rotation = Quaternion.LookRotation(-SunDir.normalized);
        RenderSettings.sun = l;
        sunLight = l;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.56f, 0.77f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.55f, 0.5f);
        RenderSettings.ambientGroundColor = new Color(0.24f, 0.22f, 0.21f);
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = 0.8f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.fogColor = new Color(0.8f, 0.8f, 0.82f);

        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 150f;
        QualitySettings.antiAliasing=4;
        QualitySettings.masterTextureLimit=0;
        QualitySettings.shadowCascades = 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        DynamicGI.UpdateEnvironment();
    }

    Shader PipeShader()
    {
        if (shader == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shader = t.GetComponent<Renderer>().sharedMaterial.shader;   // سازگار با Built-in و URP
            Destroy(t);
        }
        return shader;
    }

    Vector2 TextureTiling(string name)
    {
        switch(name)
        {
            case "asphalt": return new Vector2(2.4f,2.4f);
            case "pavement": return new Vector2(2.1f,2.1f);
            case "brick": return new Vector2(1.35f,1.35f);
            case "concrete": return new Vector2(1.8f,1.8f);
            case "plaster": return new Vector2(1.5f,1.5f);
            case "sand": return new Vector2(2.35f,2.35f);
            case "wood": return new Vector2(1.55f,1.55f);
            case "metal": return new Vector2(1.15f,1.15f);
            default: return Vector2.one;
        }
    }
    void ApplyTexScale(Material m,string prop,Vector2 scale)
    {
        if(m.HasProperty(prop)) m.SetTextureScale(prop,scale);
    }
    void ApplyDetail(Material m,string albedoName,string normalName,float scale,float normalScale=.7f)
    {
        var detailAlb=Resources.Load<Texture2D>("Textures/"+albedoName+"_Albedo");
        var detailNrm=Resources.Load<Texture2D>("Textures/"+normalName+"_Normal");
        if(detailAlb!=null && m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap",detailAlb);
        if(detailNrm!=null && m.HasProperty("_DetailNormalMap"))
        {
            m.SetTexture("_DetailNormalMap",detailNrm);
            if(m.HasProperty("_DetailNormalMapScale"))m.SetFloat("_DetailNormalMapScale",normalScale);
            m.EnableKeyword("_DETAIL_MULX2");
        }
        if(m.HasProperty("_DetailAlbedoMapScale"))m.SetFloat("_DetailAlbedoMapScale",scale);
        if(m.HasProperty("_UVSec"))m.SetFloat("_UVSec",0f);
    }

    // ماده با آلبیدو + نرمال مپ از Resources/Textures/<name>_Albedo / _Normal
    Material TexMat(string name, Color tint, float smooth, float metal = 0f)
    {
        string key = name + "|" + ((Color32)tint).GetHashCode() + "|" + smooth + "|" + metal;
        Material m;
        if (texMats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(PipeShader()) { name = name };
        var alb = Resources.Load<Texture2D>("ExternalArt/Textures/" + name + "_Albedo") ?? Resources.Load<Texture2D>("Textures/" + name + "_Albedo");
        var nrm = Resources.Load<Texture2D>("ExternalArt/Textures/" + name + "_Normal") ?? Resources.Load<Texture2D>("Textures/" + name + "_Normal");
        var mask = Resources.Load<Texture2D>("ExternalArt/Textures/" + name + "_MetalSmooth");
        var ao = Resources.Load<Texture2D>("ExternalArt/Textures/" + name + "_AO");
        Vector2 scale=TextureTiling(name);
        if (alb != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", alb);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", alb);
            ApplyTexScale(m,"_BaseMap",scale); ApplyTexScale(m,"_MainTex",scale);
        }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
        if (nrm != null && m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", nrm);
            ApplyTexScale(m,"_BumpMap",scale);
            if(m.HasProperty("_BumpScale"))m.SetFloat("_BumpScale", name=="asphalt"||name=="pavement"||name=="brick" ? 1.15f : .85f);
            m.EnableKeyword("_NORMALMAP");
        }
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if(mask!=null && m.HasProperty("_MetallicGlossMap"))
        {
            m.SetTexture("_MetallicGlossMap",mask);
            ApplyTexScale(m,"_MetallicGlossMap",scale);
            m.EnableKeyword("_METALLICGLOSSMAP");
            if(m.HasProperty("_GlossMapScale"))m.SetFloat("_GlossMapScale",1f);
        }
        if(ao!=null && m.HasProperty("_OcclusionMap")) { m.SetTexture("_OcclusionMap",ao); ApplyTexScale(m,"_OcclusionMap",scale); if(m.HasProperty("_OcclusionStrength"))m.SetFloat("_OcclusionStrength",.9f); }
        if(name=="asphalt"||name=="pavement")ApplyDetail(m,"concrete","concrete",4.2f,.45f);
        else if(name=="brick")ApplyDetail(m,"concrete","brick",2.4f,.55f);
        else if(name=="plaster"||name=="concrete")ApplyDetail(m,"plaster","concrete",3.2f,.50f);
        else if(name=="sand")ApplyDetail(m,"sand","sand",3.6f,.65f);
        else if(name=="wood")ApplyDetail(m,"wood_grain","wood",2.2f,.55f);
        m.enableInstancing=true;
        texMats[key] = m;
        return m;
    }

    // مکعبی که UV هاش بر حسب متر/tile هست، پس تکسچر روی هر وجه با اندازه‌ی واقعی تکرار میشه
    static Mesh BoxMesh(Vector3 s, float tile)
    {
        var m = new Mesh { name = "TexBox" };
        Vector3 h = s * 0.5f;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();

        void Face(Vector3 center, Vector3 u, Vector3 w, Vector3 nrm, float uLen, float wLen)
        {
            int i = v.Count;
            v.Add(center - u - w); v.Add(center + u - w); v.Add(center + u + w); v.Add(center - u + w);
            for (int k = 0; k < 4; k++) n.Add(nrm);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(uLen / tile, 0f));
            uv.Add(new Vector2(uLen / tile, wLen / tile)); uv.Add(new Vector2(0f, wLen / tile));
            Vector3 c = Vector3.Cross(v[i + 1] - v[i], v[i + 2] - v[i]);
            if (Vector3.Dot(c, nrm) > 0f) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
            else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
        }

        Face(new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z), new Vector3(0, h.y, 0), Vector3.right, s.z, s.y);
        Face(new Vector3(-h.x, 0, 0), new Vector3(0, 0, h.z), new Vector3(0, h.y, 0), Vector3.left, s.z, s.y);
        Face(new Vector3(0, 0, h.z), new Vector3(h.x, 0, 0), new Vector3(0, h.y, 0), Vector3.forward, s.x, s.y);
        Face(new Vector3(0, 0, -h.z), new Vector3(h.x, 0, 0), new Vector3(0, h.y, 0), Vector3.back, s.x, s.y);
        Face(new Vector3(0, h.y, 0), new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z), Vector3.up, s.x, s.z);
        Face(new Vector3(0, -h.y, 0), new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z), Vector3.down, s.x, s.z);
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        m.RecalculateTangents();
        return m;
    }

    // مش‌های هم‌اندازه با هم share میشن (حافظه‌ی کمتر)
    Mesh BoxMeshCached(Vector3 s, float tile)
    {
        string key = s.x.ToString("0.###") + "," + s.y.ToString("0.###") + "," + s.z.ToString("0.###") + "|" + tile.ToString("0.##");
        Mesh m;
        if (!boxMeshes.TryGetValue(key, out m) || m == null) { m = BoxMesh(s, tile); boxMeshes[key] = m; }
        return m;
    }

    GameObject TexBox(Vector3 pos, Vector3 size, Material mat, float tile, bool collider, Transform parent = null)
    {
        var go = new GameObject("TexBox");
        go.transform.SetParent(parent != null ? parent : scn, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = BoxMeshCached(size, tile);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    Color LandTint(Color c) { return curLand != null ? new Color(c.r * curLand.wallTint.r, c.g * curLand.wallTint.g, c.b * curLand.wallTint.b, 1f) : c; }

    // متریال دیوار هر ساختمون (آجر، گچ، پنل فلزی، بتن) با رنگ‌بندی هر لند
    Material WallMat(int role, System.Random rng, out float tile)
    {
        switch (role)
        {
            case 0: tile = 4f; return TexMat("plaster", LandTint(new Color(1f, 0.93f, 0.76f)), 0.06f);          // خواروبار
            case 1: tile = 2.5f; return TexMat("metal", LandTint(new Color(0.62f, 0.7f, 0.85f)), 0.35f, 0.3f);  // الکترونیک
            case 2: tile = 4f; return TexMat("plaster", LandTint(new Color(1f, 0.78f, 0.74f)), 0.06f);          // جواهری
            case 3: tile = 3f; return TexMat("concrete", LandTint(new Color(1f, 0.98f, 0.92f)), 0.12f);         // بانک
            case 4: tile = 2.5f; return TexMat("metal", new Color(0.38f, 0.3f, 0.5f), 0.4f, 0.35f);             // بلک‌مارکت
            default:
                if (rng.Next(0, 3) < 2) { tile = 1.2f; return TexMat("brick", LandTint(Color.white), 0.08f); }
                tile = 4f; return TexMat("plaster", LandTint(new Color(0.95f, 0.88f, 0.78f)), 0.06f);
        }
    }

    // شیشه‌ی نیمه‌شفاف (بدون نور و مه؛ سبک و تو Built-in و URP یکسان)
    Material GlassMat()
    {
        if (glassMat == null)
        {
            var sh = Shader.Find("Sprites/Default");
            glassMat = new Material(sh != null ? sh : PipeShader());
            glassMat.color = new Color(0.55f, 0.78f, 0.95f, 0.22f);
        }
        return glassMat;
    }

    // تم هر لند: رنگ و شدت خورشید، مه. null = تم پیش‌فرض (بیس)
    void ApplyTheme(LandDef d)
    {
        Color sun = d != null ? d.sun : new Color(1f, 0.93f, 0.8f);
        float inten = d != null ? d.sunIntensity : 1.25f;
        Color fog = d != null ? d.fog : new Color(0.8f, 0.8f, 0.82f);
        float dens = d != null ? d.fogDensity : 0.0028f;
        if (sunLight != null) { sunLight.color = sun; sunLight.intensity = inten; }
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = dens;
    }
}
