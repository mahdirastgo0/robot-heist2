using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GunData
{
    public Vector3 muzzle, rightGrip, leftGrip;   // در فضای مدل (مبدأ = دسته‌ی تپانچه، +Z = لوله، +Y = بالا)
    public Mesh mesh;
    public Color[] colors;
    public float[] metallic, smooth;
}

// مدل‌های اسلحه (Glock و HK416) که از FBX سبک‌شده و تو فرمت ساده‌ی .bytes ذخیره شدن.
// جهت‌ها از قبل درست شده: لوله +Z، بالا +Y، مبدأ = جای دست.
public static class GunModels
{
    static readonly Dictionary<string, GunData> cache = new Dictionary<string, GunData>();
    static Shader shader;

    static Vector3 V3(BinaryReader br) { return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle()); }

    public static GunData Load(string res)
    {
        GunData d;
        if (cache.TryGetValue(res, out d)) return d;
        cache[res] = null;
        var ta = Resources.Load<TextAsset>(res);
        if (ta == null) return null;
        try
        {
            using (var br = new BinaryReader(new MemoryStream(ta.bytes)))
            {
                if (new string(br.ReadChars(4)) != "GUN1") return null;
                d = new GunData();
                d.muzzle = V3(br); d.rightGrip = V3(br); d.leftGrip = V3(br);
                int mc = br.ReadInt32();
                var verts = new List<Vector3>();
                var subs = new List<int[]>();
                d.colors = new Color[mc]; d.metallic = new float[mc]; d.smooth = new float[mc];
                for (int m = 0; m < mc; m++)
                {
                    d.colors[m] = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    d.metallic[m] = br.ReadSingle(); d.smooth[m] = br.ReadSingle();
                    int vc = br.ReadInt32(), ic = br.ReadInt32();
                    var vb = br.ReadBytes(vc * 12);
                    var vf = new float[vc * 3];
                    System.Buffer.BlockCopy(vb, 0, vf, 0, vb.Length);
                    int off = verts.Count;
                    for (int i = 0; i < vc; i++) verts.Add(new Vector3(vf[i * 3], vf[i * 3 + 1], vf[i * 3 + 2]));
                    var ib = br.ReadBytes(ic * 4);
                    var idx = new int[ic];
                    System.Buffer.BlockCopy(ib, 0, idx, 0, ib.Length);
                    for (int i = 0; i < ic; i++) idx[i] += off;
                    subs.Add(idx);
                }
                var mesh = new Mesh { name = Path.GetFileName(res) };
                if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(verts);
                mesh.subMeshCount = mc;
                for (int m = 0; m < mc; m++) mesh.SetTriangles(subs[m], m);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                d.mesh = mesh;
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[GunModels] failed to load " + res + ": " + e.Message); return null; }
        cache[res] = d;
        return d;
    }

    static Shader GetShader()
    {
        if (shader == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shader = t.GetComponent<Renderer>().sharedMaterial.shader;
            Object.Destroy(t);
        }
        return shader;
    }

    public static GameObject Spawn(GunData d, Transform parent, float scale, bool tint, Color tintColor)
    {
        var go = new GameObject("GunModel");
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = d.mesh;
        var r = go.AddComponent<MeshRenderer>();
        var mats = new Material[d.colors.Length];
        for (int i = 0; i < mats.Length; i++)
        {
            var m = new Material(GetShader());
            Color c = (tint && i == 0) ? tintColor : d.colors[i];
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", d.metallic[i]);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", d.smooth[i]);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", d.smooth[i]);
            mats[i] = m;
        }
        r.sharedMaterials = mats;
        return go;
    }
}
