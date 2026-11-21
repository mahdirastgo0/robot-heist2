using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public class PropMat { public Color col; public float metallic, smooth; public int flags; public string tex; }

public class PropData
{
    public string name;
    public Mesh mesh;
    public List<PropMat> mats = new List<PropMat>();
    readonly Dictionary<int, Material[]> cache = new Dictionary<int, Material[]>();

    static Shader shader;
    static Shader GetShader()
    {
        if (shader == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shader = t.GetComponent<Renderer>().sharedMaterial.shader;   // سازگار با Built-in و URP
            Object.Destroy(t);
        }
        return shader;
    }

    static void SetCol(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    static void Cutout(Material m)
    {
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
        if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;
    }

    static void Transparent(Material m)
    {
        if (m.HasProperty("_Mode"))
        {
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_ALPHABLEND_ON");
        }
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
        }
        m.renderQueue = 3000;
    }

    // رنگ بدنه (flag 1) میتونه برای هر ماشین عوض بشه
    public Material[] GetMaterials(bool useTint, Color tint)
    {
        int key = useTint ? ((Color32)tint).GetHashCode() | 1 : 0;
        Material[] arr;
        if (cache.TryGetValue(key, out arr)) return arr;
        arr = new Material[mats.Count];
        for (int i = 0; i < arr.Length; i++)
        {
            var pm = mats[i];
            var m = new Material(GetShader()) { name = name + "_" + i };
            Texture2D tex = string.IsNullOrEmpty(pm.tex) ? null : Resources.Load<Texture2D>(pm.tex);
            Color c = pm.col;
            if ((pm.flags & 1) != 0 && useTint) c = tint;
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
                c = Color.white;
            }
            if ((pm.flags & 4) != 0) c.a = tex != null ? 0.55f : 0.35f;
            SetCol(m, c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", pm.metallic);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", pm.smooth);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", pm.smooth);
            if ((pm.flags & 2) != 0) Cutout(m);
            if ((pm.flags & 4) != 0) Transparent(m);
            arr[i] = m;
        }
        cache[key] = arr;
        return arr;
    }
}

// مدل‌های سبک‌شده‌ی ماشین، درخت و وسایل خیابون (فرمت PRP1 ساخته‌شده از FBX/OBJ).
// جهت‌ها از قبل درست شده: +Y بالا، +Z جلو، مبدأ = کف مدل.
public static class PropModels
{
    static readonly Dictionary<string, PropData> cache = new Dictionary<string, PropData>();

    public static PropData Load(string res)
    {
        PropData d;
        if (cache.TryGetValue(res, out d)) return d;
        cache[res] = null;
        var ta = Resources.Load<TextAsset>(res);
        if (ta == null) return null;
        try
        {
            using (var br = new BinaryReader(new MemoryStream(ta.bytes)))
            {
                string magic = new string(br.ReadChars(4));
                if (magic != "PRP1" && magic != "PRP2") return null;
                bool hasN = magic == "PRP2";
                d = new PropData { name = Path.GetFileName(res) };
                int mc = br.ReadInt32();
                var verts = new List<Vector3>();
                var uvs = new List<Vector2>();
                var norms = new List<Vector3>();
                var subs = new List<int[]>();
                for (int m = 0; m < mc; m++)
                {
                    var pm = new PropMat();
                    pm.col = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    pm.metallic = br.ReadSingle(); pm.smooth = br.ReadSingle();
                    pm.flags = br.ReadInt32();
                    int tl = br.ReadInt32();
                    pm.tex = tl > 0 ? new string(br.ReadChars(tl)) : "";
                    d.mats.Add(pm);
                    int vc = br.ReadInt32(), ic = br.ReadInt32();
                    var vb = br.ReadBytes(vc * 12); var vf = new float[vc * 3]; System.Buffer.BlockCopy(vb, 0, vf, 0, vb.Length);
                    var ub = br.ReadBytes(vc * 8); var uf = new float[vc * 2]; System.Buffer.BlockCopy(ub, 0, uf, 0, ub.Length);
                    float[] nf = null;
                    if (hasN) { var nb = br.ReadBytes(vc * 12); nf = new float[vc * 3]; System.Buffer.BlockCopy(nb, 0, nf, 0, nb.Length); }
                    var ib = br.ReadBytes(ic * 4); var idx = new int[ic]; System.Buffer.BlockCopy(ib, 0, idx, 0, ib.Length);
                    int off = verts.Count;
                    for (int i = 0; i < vc; i++)
                    {
                        verts.Add(new Vector3(vf[i * 3], vf[i * 3 + 1], vf[i * 3 + 2]));
                        uvs.Add(new Vector2(uf[i * 2], uf[i * 2 + 1]));
                        if (hasN) norms.Add(new Vector3(nf[i * 3], nf[i * 3 + 1], nf[i * 3 + 2]));
                    }
                    for (int i = 0; i < ic; i++) idx[i] += off;
                    subs.Add(idx);
                }
                var mesh = new Mesh { name = d.name };
                if (verts.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.SetUVs(0, uvs);
                mesh.subMeshCount = mc;
                for (int m = 0; m < mc; m++) mesh.SetTriangles(subs[m], m);
                if (hasN) mesh.SetNormals(norms); else mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                d.mesh = mesh;
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[PropModels] failed to load " + res + ": " + e.Message); return null; }
        cache[res] = d;
        return d;
    }

    public static GameObject Spawn(PropData d, Transform parent, Vector3 pos, float yaw, float scale, bool useTint, Color tint, bool collider)
    {
        var go = new GameObject(d.name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = d.mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = d.GetMaterials(useTint, tint);
        if (collider)
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = d.mesh.bounds.center;
            bc.size = d.mesh.bounds.size;
        }
        return go;
    }
}
