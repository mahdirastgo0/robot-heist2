using System.Collections.Generic;
using UnityEngine;

// درخت‌ها: مش پروسیجرال (تنه‌ی خمیده، شاخه، تاج‌های برگی) با بافت‌های واقعی پوست و برگ.
// اگه مدل درخت دانلودی (مثلا Quaternius / Kenney، CC0) داری، فقط بریز تو Assets/Resources/Models/Trees/
// (فایل FBX/OBJ + بافتشون). بازی خودش اونارو به‌جای درخت‌های ساخته‌شده با کد استفاده میکنه.
public partial class GameManager
{
    class TreeVariant { public Mesh mesh; public Material[] mats; }
    readonly List<TreeVariant> treeVariants = new List<TreeVariant>();
    GameObject[] externalTrees;

    class MeshBuf
    {
        public List<Vector3> v = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> bark = new List<int>();
        public List<int> leaf = new List<int>();
    }

    // ---------------------------------------------------------------- shapes
    static Vector3[] icoV;
    static int[] icoT;

    static int IcoMid(List<Vector3> v, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        int idx;
        if (cache.TryGetValue(key, out idx)) return idx;
        v.Add(((v[a] + v[b]) * 0.5f).normalized);
        idx = v.Count - 1;
        cache[key] = idx;
        return idx;
    }

    static void BuildIco()
    {
        if (icoV != null) return;
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var v = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
        int[] f0 =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };
        var f = new List<int>(f0);
        for (int s = 0; s < 2; s++)
        {
            var nf = new List<int>();
            var cache = new Dictionary<long, int>();
            for (int i = 0; i < f.Count; i += 3)
            {
                int a = f[i], b = f[i + 1], c = f[i + 2];
                int ab = IcoMid(v, cache, a, b), bc = IcoMid(v, cache, b, c), ca = IcoMid(v, cache, c, a);
                nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            f = nf;
        }
        // جهت وجه‌ها طوری که رو به بیرون باشن (Unity: نرمال = cross(b-a, c-a))
        for (int i = 0; i < f.Count; i += 3)
        {
            Vector3 a = v[f[i]], b = v[f[i + 1]], c = v[f[i + 2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) { int tmp = f[i + 1]; f[i + 1] = f[i + 2]; f[i + 2] = tmp; }
        }
        icoV = v.ToArray();
        icoT = f.ToArray();
    }

    // مثلث با جهت درست (سمت hint بیرونه)
    static void Tri(MeshBuf m, List<int> list, int a, int b, int c, Vector3 hint)
    {
        Vector3 n = Vector3.Cross(m.v[b] - m.v[a], m.v[c] - m.v[a]);
        if (Vector3.Dot(n, hint) < 0f) { int t = b; b = c; c = t; }
        list.Add(a); list.Add(b); list.Add(c);
    }

    // لوله‌ی مخروطی (تنه / شاخه)
    static void AddTube(MeshBuf m, Vector3 a, Vector3 b, float ra, float rb, int segs, float vTile)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.001f) return;
        dir /= len;
        Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
        Vector3 up2 = Vector3.Cross(dir, side);
        int b0 = m.v.Count;
        for (int ring = 0; ring < 2; ring++)
            for (int s = 0; s <= segs; s++)
            {
                float ang = s / (float)segs * Mathf.PI * 2f;
                Vector3 off = (side * Mathf.Cos(ang) + up2 * Mathf.Sin(ang)) * (ring == 0 ? ra : rb);
                m.v.Add((ring == 0 ? a : b) + off);
                m.uv.Add(new Vector2(s / (float)segs * 2f, ring == 0 ? 0f : len * vTile));
            }
        for (int s = 0; s < segs; s++)
        {
            int i0 = b0 + s, i1 = b0 + s + 1, i2 = b0 + segs + 1 + s, i3 = b0 + segs + 2 + s;
            Vector3 hint = m.v[i0] - a;
            Tri(m, m.bark, i0, i1, i3, hint);
            Tri(m, m.bark, i0, i3, i2, hint);
        }
    }

    // کره‌ی ناصاف (توده‌ی برگ). radii = شعاع در هر محور
    static void AddBlob(MeshBuf m, Vector3 c, Vector3 radii, float seed, float noiseAmt, Vector2 uvOff)
    {
        BuildIco();
        int b0 = m.v.Count;
        for (int i = 0; i < icoV.Length; i++)
        {
            Vector3 d = icoV[i];
            float n = Mathf.PerlinNoise(d.x * 1.9f + seed, d.y * 1.6f + d.z * 1.4f + seed * 0.7f);
            float k = 1f + (n - 0.5f) * noiseAmt;
            m.v.Add(c + new Vector3(d.x * radii.x, d.y * radii.y, d.z * radii.z) * k);
            m.uv.Add(new Vector2(0.5f + 0.5f * d.x + 0.35f * d.z, 0.5f + 0.5f * d.y + 0.35f * d.z) * 1.7f + uvOff);
        }
        for (int i = 0; i < icoT.Length; i++) m.leaf.Add(b0 + icoT[i]);
    }

    // طبقه‌ی کاج (مخروط دندانه‌دار)
    static void AddConeTier(MeshBuf m, Vector3 baseC, float radius, float height, int segs, float seed)
    {
        int b0 = m.v.Count;
        for (int ring = 0; ring < 2; ring++)
            for (int s = 0; s <= segs; s++)
            {
                float ang = s / (float)segs * Mathf.PI * 2f;
                float jag = 1f + ((s % 2 == 0) ? 0.14f : -0.1f) + (Mathf.PerlinNoise(s * 0.7f + seed, seed) - 0.5f) * 0.2f;
                if (ring == 0) m.v.Add(baseC + new Vector3(Mathf.Cos(ang) * radius * jag, 0f, Mathf.Sin(ang) * radius * jag));
                else m.v.Add(baseC + new Vector3(0f, height, 0f));
                m.uv.Add(new Vector2(s / (float)segs * 3f, ring == 0 ? 0f : 1.6f));
            }
        for (int s = 0; s < segs; s++)
        {
            int i0 = b0 + s, i1 = b0 + s + 1, i2 = b0 + segs + 1 + s, i3 = b0 + segs + 2 + s;
            Vector3 hint = new Vector3(m.v[i0].x - baseC.x, 0.2f, m.v[i0].z - baseC.z);
            Tri(m, m.leaf, i0, i1, i3, hint);
            Tri(m, m.leaf, i0, i3, i2, hint);
        }
    }

    TreeVariant FinishTree(MeshBuf m, Material bark, Material leaf)
    {
        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(m.v);
        mesh.SetUVs(0, m.uv);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(m.bark.Count > 0 ? m.bark : new List<int>(), 0);
        mesh.SetTriangles(m.leaf, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return new TreeVariant { mesh = mesh, mats = new[] { bark, leaf } };
    }

    // ---------------------------------------------------------------- species
    TreeVariant MakeBroadleaf(System.Random r, string leafTex, Color tint, float size)
    {
        var m = new MeshBuf();
        float h = (3.0f + (float)r.NextDouble() * 0.9f) * size;
        float bx = ((float)r.NextDouble() - 0.5f) * 0.5f, bz = ((float)r.NextDouble() - 0.5f) * 0.5f;
        Vector3 p0 = new Vector3(0, -0.05f, 0), p1 = new Vector3(bx * 0.4f, h * 0.38f, bz * 0.4f), p2 = new Vector3(bx, h * 0.72f, bz), p3 = new Vector3(bx * 1.2f, h, bz * 1.2f);
        AddTube(m, p0, new Vector3(0, 0.6f, 0), 0.5f * size, 0.3f * size, 9, 0.5f);            // ریشه‌ی پهن
        AddTube(m, new Vector3(0, 0.55f, 0), p1, 0.3f * size, 0.25f * size, 9, 0.5f);
        AddTube(m, p1, p2, 0.25f * size, 0.18f * size, 9, 0.5f);
        AddTube(m, p2, p3, 0.18f * size, 0.1f * size, 8, 0.5f);

        Vector3 crown = p3 + new Vector3(0, 1.2f * size, 0);
        float seed = (float)r.NextDouble() * 50f;
        AddBlob(m, crown, new Vector3(1.9f, 1.55f, 1.9f) * size, seed, 0.3f, Vector2.zero);
        int n = 5 + r.Next(0, 2);
        for (int i = 0; i < n; i++)
        {
            float ang = i / (float)n * Mathf.PI * 2f + (float)r.NextDouble() * 0.5f;
            float rad = (1.2f + (float)r.NextDouble() * 0.5f) * size;
            Vector3 c = crown + new Vector3(Mathf.Cos(ang) * rad, ((float)r.NextDouble() - 0.35f) * 0.9f * size, Mathf.Sin(ang) * rad);
            float br = (1.0f + (float)r.NextDouble() * 0.55f) * size;
            AddBlob(m, c, new Vector3(br, br * 0.85f, br), seed + i * 7.3f, 0.34f, new Vector2(i * 0.31f, i * 0.17f));
            // شاخه از تنه تا توده
            Vector3 start = Vector3.Lerp(p1, p3, 0.45f + 0.5f * (i / (float)n));
            AddTube(m, start, Vector3.Lerp(start, c, 0.72f), 0.1f * size, 0.04f * size, 6, 0.6f);
        }
        float bt = 0.85f + (float)r.NextDouble() * 0.3f;
        return FinishTree(m, TexMat("bark", new Color(bt, bt * 0.97f, bt * 0.93f), 0.04f), TexMat(leafTex, tint, 0.12f));
    }

    TreeVariant MakePine(System.Random r, float size)
    {
        var m = new MeshBuf();
        float seed = (float)r.NextDouble() * 40f;
        int tiers = 5 + r.Next(0, 2);
        float top = 1.2f + tiers * 1.05f;
        AddTube(m, new Vector3(0, -0.05f, 0), new Vector3(0, 0.5f, 0), 0.4f * size, 0.26f * size, 8, 0.5f);
        AddTube(m, new Vector3(0, 0.45f, 0), new Vector3(0, top, 0), 0.26f * size, 0.04f * size, 8, 0.5f);
        for (int i = 0; i < tiers; i++)
        {
            float t = i / (float)(tiers - 1);
            float rad = Mathf.Lerp(1.9f, 0.7f, t) * size;
            float y = (1.1f + i * 1.0f) * 1.0f;
            AddConeTier(m, new Vector3(0, y, 0), rad, 1.9f * size, 12, seed + i * 3f);
        }
        float bt = 0.8f + (float)r.NextDouble() * 0.3f;
        float g = 0.85f + (float)r.NextDouble() * 0.25f;
        return FinishTree(m, TexMat("bark", new Color(bt, bt * 0.9f, bt * 0.85f), 0.04f), TexMat("needles", new Color(g, g, g), 0.1f));
    }

    TreeVariant MakePoplar(System.Random r, string leafTex, Color tint, float size)
    {
        var m = new MeshBuf();
        float seed = (float)r.NextDouble() * 60f;
        float h = 2.0f * size;
        AddTube(m, new Vector3(0, -0.05f, 0), new Vector3(0, 0.5f, 0), 0.4f * size, 0.22f * size, 8, 0.5f);
        AddTube(m, new Vector3(0, 0.45f, 0), new Vector3(0, h + 1.5f, 0), 0.22f * size, 0.08f * size, 8, 0.5f);
        int n = 4;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            float rad = Mathf.Lerp(1.15f, 0.65f, t) * size;
            AddBlob(m, new Vector3(((float)r.NextDouble() - 0.5f) * 0.25f, h + i * 1.05f * size, ((float)r.NextDouble() - 0.5f) * 0.25f),
                new Vector3(rad, 1.25f * size, rad), seed + i * 5f, 0.3f, new Vector2(i * 0.23f, i * 0.41f));
        }
        float bt = 0.85f + (float)r.NextDouble() * 0.25f;
        return FinishTree(m, TexMat("bark", new Color(bt, bt, bt * 0.95f), 0.04f), TexMat(leafTex, tint, 0.12f));
    }

    // نوع درخت‌های هر لند: O بلوط، A بلوط پاییزی، B شکوفه، P کاج، L صنوبر باریک
    static readonly string[] TreeSpecs = { "OOBLO", "PPOPL", "AAOAL", "OLLBL", "PPOO", "LLBOL" };

    void BuildTreeVariants(int landIdx)
    {
        treeVariants.Clear();
        var r = new System.Random(landIdx * 977 + 13);
        string spec = TreeSpecs[Mathf.Clamp(landIdx, 0, TreeSpecs.Length - 1)];
        foreach (char ch in spec)
            for (int k = 0; k < 2; k++)
            {
                float g = 0.9f + (float)r.NextDouble() * 0.2f;
                switch (ch)
                {
                    case 'A': treeVariants.Add(MakeBroadleaf(r, "leaves_autumn", new Color(g, g, g), 1f)); break;
                    case 'B': treeVariants.Add(MakeBroadleaf(r, "leaves_blossom", new Color(g, g, g), 0.9f)); break;
                    case 'P': treeVariants.Add(MakePine(r, 1f)); break;
                    case 'L': treeVariants.Add(MakePoplar(r, "leaves", new Color(g * 0.95f, g, g * 0.9f), 1f)); break;
                    default: treeVariants.Add(MakeBroadleaf(r, "leaves", new Color(g * 0.95f, g, g * 0.9f), 1f)); break;
                }
            }
        externalTrees = Resources.LoadAll<GameObject>("Models/Trees");
    }

    // یه درخت تو جای مشخص میکاره (مدل دانلودی اگه باشه، وگرنه درخت ساخته‌شده با کد)
    void SpawnTree(Vector3 pos, float yaw, float scale, System.Random rng)
    {
        if (externalTrees != null && externalTrees.Length > 0 && rng.Next(0, 10) < 8)
        {
            try
            {
                var prefab = externalTrees[rng.Next(0, externalTrees.Length)];
                var inst = Instantiate(prefab);
                inst.transform.SetParent(scn, false);
                inst.transform.position = pos;
                inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    foreach (var rr in rs) b.Encapsulate(rr.bounds);
                    float k = (6.5f * scale) / Mathf.Max(0.5f, b.size.y);
                    inst.transform.localScale *= k;
                    b = rs[0].bounds;
                    foreach (var rr in rs) b.Encapsulate(rr.bounds);
                    inst.transform.position += new Vector3(0f, pos.y - b.min.y, 0f);
                }
                var cc0 = inst.AddComponent<CapsuleCollider>();
                cc0.radius = 0.3f; cc0.height = 3f; cc0.center = new Vector3(0f, 1.5f / Mathf.Max(0.01f, inst.transform.localScale.y), 0f);
                return;
            }
            catch (System.Exception e) { Debug.LogWarning("External tree failed: " + e.Message); }
        }
        if (treeVariants.Count == 0) return;
        var tv = treeVariants[rng.Next(0, treeVariants.Count)];
        var go = new GameObject("Tree");
        go.transform.SetParent(scn, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = tv.mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = tv.mats;
        var cc = go.AddComponent<CapsuleCollider>();
        cc.radius = 0.3f; cc.height = 3f; cc.center = new Vector3(0f, 1.5f, 0f);
    }
}
