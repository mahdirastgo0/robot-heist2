using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ساخت لند با کد: خیابون‌ها، ساختمون‌ها، پارک و محوطه‌ی کانتینر، مبلمان شهری، ماشین‌ها
public partial class GameManager
{
    static readonly string[] ShopNames = { "Grocery", "Electronics", "Jewelry Store", "Bank" };
    static readonly string[] SignTexts = { "GROCERY", "ELECTRONICS", "JEWELRY", "BANK" };
    static readonly Color[] ShopColors =
    {
        new Color(0.3f, 0.85f, 0.35f), new Color(0.3f, 0.55f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.85f, 0.85f, 0.9f)
    };
    static readonly Color MarketColor = new Color(0.75f, 0.3f, 1f);
    static readonly Color LitWindow = new Color(1f, 0.86f, 0.5f);

    Transform scn;       // پدر فعلی دکور ثابت (موقع ساخت هر بلوک عوض میشه)
    Transform scnRoot;   // دکور عمومی (زمین، خیابون)؛ هر بلوک ساختمون جداگونه ترکیب میشه
    readonly Dictionary<int, Material> matCache = new Dictionary<int, Material>();
    readonly Dictionary<PrimitiveType, Mesh> primMeshes = new Dictionary<PrimitiveType, Mesh>();
    float loadPct;
    string loadText = "";

    // ---------------------------------------------------------------- Scene basics
    // متریال‌ها بر اساس رنگ cache میشن تا batching کار کنه
    Material Mat(Color c)
    {
        Color32 k = c;
        int key = (k.r << 24) | (k.g << 16) | (k.b << 8) | k.a;
        Material m;
        if (!matCache.TryGetValue(key, out m) || m == null)
        {
            m = new Material(PipeShader());
            m.color = c;
            matCache[key] = m;
        }
        return m;
    }

    Mesh PrimMesh(PrimitiveType t)
    {
        Mesh m;
        if (primMeshes.TryGetValue(t, out m) && m != null) return m;
        var g = GameObject.CreatePrimitive(t);
        m = g.GetComponent<MeshFilter>().sharedMesh;
        Destroy(g);
        primMeshes[t] = m;
        return m;
    }

    // سریع‌تر از CreatePrimitive + Destroy(collider): مش مشترک و collider فقط وقتی لازمه
    GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color c, bool withCollider = false)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.AddComponent<MeshFilter>().sharedMesh = PrimMesh(type);
        g.AddComponent<MeshRenderer>().sharedMaterial = Mat(c);
        if (withCollider)
        {
            switch (type)
            {
                case PrimitiveType.Cube: g.AddComponent<BoxCollider>(); break;
                case PrimitiveType.Sphere: g.AddComponent<SphereCollider>(); break;
                default:
                    var cc = g.AddComponent<CapsuleCollider>();
                    cc.radius = 0.5f; cc.height = 2f;
                    break;
            }
        }
        return g;
    }

    GameObject Box(Vector3 pos, Vector3 size, Color c, bool col = false) { return Prim(PrimitiveType.Cube, "Box", scn, pos, size, c, col); }
    GameObject Cyl(Vector3 pos, Vector3 scale, Color c) { return Prim(PrimitiveType.Cylinder, "Cyl", scn, pos, scale, c); }
    GameObject Ball(Vector3 pos, Vector3 scale, Color c) { return Prim(PrimitiveType.Sphere, "Ball", scn, pos, scale, c); }

    void ResetRoot()
    {
        if (root != null) Destroy(root.gameObject);
        root = new GameObject("Level").transform;
        scnRoot = new GameObject("Scenery").transform;
        scnRoot.SetParent(root, false);
        scn = scnRoot;
        spots.Clear(); cops.Clear(); shops.Clear(); signFits.Clear(); blockInfos.Clear();
        bobbers.Clear(); waveA = null; waveB = null;
        Player = null; interiorLight = null;
        holdSpot = null; holdProgress = 0f; hint = "";
    }

    void CombineScenery()
    {
        try { StaticBatchingUtility.Combine(scnRoot.gameObject); }
        catch (System.Exception e) { Debug.LogWarning("Static batching skipped: " + e.Message); }
    }

    Spot MakeSpot(SpotType type, Vector3 pos, Color c, string label, int shopType = 0, float duration = 0f, bool visual = true)
    {
        var s = new Spot { type = type, pos = pos, label = label, shopType = shopType, duration = duration, color = c };
        if (visual)
        {
            s.pad = Prim(PrimitiveType.Cylinder, "Pad", root, pos + Vector3.up * 0.05f, new Vector3(2.4f, 0.03f, 2.4f), c);
            s.beam = Prim(PrimitiveType.Cylinder, "Beam", root, pos + Vector3.up * 3f, new Vector3(0.2f, 3f, 0.2f), c);
        }
        spots.Add(s);
        return s;
    }

    // ---------------------------------------------------------------- 3D signs (TextMesh)
    class SignFit { public Transform t; public Renderer r; public float maxW, maxH; public int tries; }
    readonly List<SignFit> signFits = new List<SignFit>();
    Font signFont;

    GameObject Sign3D(string text, Vector3 pos, float yaw, float maxW, float maxH, Color col)
    {
        var go = new GameObject("Sign:" + text);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (signFont == null)
        {
            try { signFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (System.Exception) { }
            if (signFont == null) signFont = Font.CreateDynamicFontFromOSFont("Arial", 64);
        }
        var tm = go.AddComponent<TextMesh>();
        tm.font = signFont;
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.05f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = col;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = signFont.material;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        signFits.Add(new SignFit { t = go.transform, r = mr, maxW = maxW, maxH = maxH });
        return go;
    }

    // اندازه‌ی واقعی متن بعد از اولین رندر معلوم میشه؛ اون موقع به اندازه‌ی تابلو scale میشه
    void FitSigns()
    {
        for (int i = signFits.Count - 1; i >= 0; i--)
        {
            var f = signFits[i];
            if (f.t == null || f.r == null) { signFits.RemoveAt(i); continue; }
            Vector3 b = f.r.bounds.size;
            float w = Mathf.Max(b.x, b.z), h = b.y;
            if (w < 0.01f || h < 0.01f) { if (++f.tries > 300) signFits.RemoveAt(i); continue; }
            float k = Mathf.Min(f.maxW / w, f.maxH / h);
            f.t.localScale = f.t.localScale * k;
            signFits.RemoveAt(i);
        }
    }

    // ---------------------------------------------------------------- Land grid
    // جاده‌ها: x%6==0 یا y%6==0 | پیاده‌رو: حلقه‌ی دور هر بلوک | ساختمان ۳×۳ وسط هر بلوک
    void BuildGrid(LandDef d, System.Random rng)
    {
        CW = d.bx * 6 + 1; CH = d.by * 6 + 1;
        blocked = new bool[CW, CH];
        walkCells.Clear(); blockInfos.Clear();

        var roles = new List<int>();
        for (int r = 0; r < d.mix.Length; r++)
            for (int k = 0; k < d.mix[r]; k++) roles.Add(r);
        while (roles.Count < d.bx * d.by) roles.Add(5);
        for (int i = roles.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int t = roles[i]; roles[i] = roles[j]; roles[j] = t;
        }

        int n = 0;
        for (int bj = 0; bj < d.by; bj++)
            for (int bi = 0; bi < d.bx; bi++)
            {
                int role = roles[n++];
                var bmin = new Vector2Int(bi * 6 + 2, bj * 6 + 2);
                blockInfos.Add(new Vector3Int(bmin.x, bmin.y, role));
                if (role == 6) blocked[bmin.x + 1, bmin.y + 1] = true;        // پارک: فقط وسط (فواره)
                else
                    for (int yy = 0; yy < 3; yy++)
                        for (int xx = 0; xx < 3; xx++) blocked[bmin.x + xx, bmin.y + yy] = true;
            }
        for (int y = 0; y < CH; y++)
            for (int x = 0; x < CW; x++)
                if (!blocked[x, y]) walkCells.Add(new Vector2Int(x, y));
    }

    string CityMapString()
    {
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < CH; y++)
            for (int x = 0; x < CW; x++)
                sb.Append(blocked[x, y] ? '#' : '.');
        return sb.ToString();
    }

    int ExitRow => 6 * (curLand.by / 2);

    // ---------------------------------------------------------------- Land build (coroutine: لودینگ با درصد)
    // اگه وسط ساخت لند خطا بیاد، به‌جای گیر کردن رو صفحه‌ی Loading، خطا نشون داده میشه و برمیگردی بیس
    IEnumerator LandRoutine(int idx)
    {
        var inner = LandRoutineInner(idx);
        while (true)
        {
            object cur = null;
            System.Exception err = null;
            bool more = false;
            try { more = inner.MoveNext(); if (more) cur = inner.Current; }
            catch (System.Exception e) { err = e; }
            if (err != null)
            {
                Debug.LogException(err);
                loadText = "ERROR: " + err.GetType().Name + " - " + err.Message;
                loadFailed = true;
                yield return new WaitForSeconds(8f);
                loadFailed = false;
                StartBase("Could not load " + Lands.All[idx].name + " (" + err.GetType().Name + "). Check the Unity Console.");
                yield break;
            }
            if (!more) yield break;
            yield return cur;
        }
    }

    bool loadFailed;

    IEnumerator LandRoutineInner(int idx)
    {
        State = GameState.Loading;
        curLandIdx = idx; curLand = Lands.All[idx];
        loadPct = 0f; loadText = "Generating " + curLand.name + "...";
        yield return null;

        var rng = new System.Random(Random.Range(1, 1000000));
        ResetRoot();
        BuildGrid(curLand, rng);
        BuildTreeVariants(idx);
        if (!NativeCore.SetMap(CityMapString(), CW, CH)) { fatalMsg = "Map upload to C++ core failed"; State = GameState.Menu; yield break; }
        ApplyTheme(curLand);
        marketMult = Mathf.Round((NativeCore.LandMarketLo(idx) + (float)rng.NextDouble() * (NativeCore.LandMarketHi(idx) - NativeCore.LandMarketLo(idx))) ) / 100f;

        BuildGround(rng);
        BuildSea(rng);
        loadPct = 0.1f; loadText = "Roads and sidewalks..."; yield return null;
        BuildRoadMarkings();
        loadPct = 0.2f; yield return null;
        BuildStreetProps(rng);
        loadPct = 0.35f; loadText = "Buildings..."; yield return null;

        var bi = blockInfos.ToArray();
        for (int i = 0; i < bi.Length; i++)
        {
            var bmin = new Vector2Int(bi[i].x, bi[i].y);
            int role = bi[i].z;
            // هر بلوک پدر جدا داره و جداگونه batch میشه (فریز کوتاه‌تر + culling بهتر)
            var blk = new GameObject("Block_" + bmin.x + "_" + bmin.y).transform;
            blk.SetParent(root, false);
            scn = blk;
            if (role == 6) BuildPark(bmin, rng);
            else if (role == 7) BuildYard(bmin, rng);
            else
            {
                BuildBuilding(bmin, role, rng);
                if (role <= 4) PlaceShopMarker(bmin, role, rng);
            }
            try { StaticBatchingUtility.Combine(blk.gameObject); }
            catch (System.Exception e) { Debug.LogWarning("Block batching skipped: " + e.Message); }
            scn = scnRoot;
            loadPct = 0.35f + 0.5f * (i + 1) / bi.Length;
            yield return null;
        }

        loadPct = 0.88f; loadText = "Finishing touches..."; yield return null;
        BuildBusStops();
        BuildExitGate();
        CombineScenery();
        loadPct = 0.97f; yield return null;

        int ey = ExitRow;
        CreatePlayer(CellToWorld(new Vector2Int(0, ey)) + Vector3.up * 0.1f, 90f);
        var lg = new GameObject("InteriorLight");
        lg.transform.SetParent(root, false);
        interiorLight = lg.AddComponent<Light>();
        interiorLight.type = LightType.Point;
        interiorLight.range = 15f; interiorLight.intensity = 1.2f;
        interiorLight.color = new Color(1f, 0.93f, 0.8f);
        interiorLight.shadows = LightShadows.None;
        interiorLight.enabled = false;

        OnLandReady();
        SpawnCops(Mathf.Max(1, NativeCore.LandCopStart(idx) - NativeCore.CopStartReduction));
    }

    void PlaceShopMarker(Vector2Int bmin, int role, System.Random rng)
    {
        Vector3 door = CellToWorld(new Vector2Int(bmin.x + 1, bmin.y + 3));
        var news = PropModels.Load("Models/Props/newspaper");
        if (news != null && role != 4 && rng.Next(0, 2) == 0)
            PropModels.Spawn(news, scn, door + new Vector3(1.9f, 0f, 0.4f), rng.Next(0, 4) * 90f, 1f, false, Color.white, true);
        if (role == 4) { MakeSpot(SpotType.Market, door, MarketColor, "BLACK MARKET"); return; }
        var sh = shops[shops.Count - 1];   // BuildBuilding اضافه‌ش کرده
        sh.marker = MakeSpot(SpotType.Shop, door, ShopColors[role], ShopNames[role], role, 0f, false);   // بدون ستون رنگی؛ نشونه فقط تابلو و مینی‌مپ
        sh.marker.shop = sh;
    }

    void BuildGround(System.Random rng)
    {
        float midX = (CW - 1) * Cell / 2f, midZ = (CH - 1) * Cell / 2f;
        float sx = (CW - 1) * Cell + 14f, sz = (CH - 1) * Cell + 14f;
        TexBox(new Vector3(midX, -0.1f, -midZ), new Vector3(sx, 0.2f, sz), TexMat("asphalt", curLand.asphalt, 0.08f), 8f, true);

        var pave = TexMat("pavement", Color.white, 0.1f);
        Color curb = new Color(0.62f, 0.62f, 0.64f);
        for (int j = 0; j < curLand.by; j++)
            for (int i = 0; i < curLand.bx; i++)
            {
                var c = CellToWorld(new Vector2Int(6 * i + 3, 6 * j + 3));
                TexBox(new Vector3(c.x, 0f, c.z), new Vector3(5f * Cell, 0.12f, 5f * Cell), pave, 4f, true);
                // لبه‌ی پیاده‌رو (جدول)
                float e = 2.5f * Cell + 0.1f;
                Box(new Vector3(c.x, 0.07f, c.z - e), new Vector3(5f * Cell + 0.4f, 0.14f, 0.22f), curb);
                Box(new Vector3(c.x, 0.07f, c.z + e), new Vector3(5f * Cell + 0.4f, 0.14f, 0.22f), curb);
                Box(new Vector3(c.x - e, 0.07f, c.z), new Vector3(0.22f, 0.14f, 5f * Cell), curb);
                Box(new Vector3(c.x + e, 0.07f, c.z), new Vector3(0.22f, 0.14f, 5f * Cell), curb);
            }
    }

    // خط وسط جاده + خط عابر پیاده (زبرا) کنار هر چهارراه + دریچه‌ی فاضلاب
    void BuildRoadMarkings()
    {
        Color line = new Color(0.85f, 0.8f, 0.5f);
        Color zebra = new Color(0.92f, 0.92f, 0.92f);
        for (int y = 0; y < CH; y++)
            for (int x = 0; x < CW; x++)
            {
                bool h = y % 6 == 0, v = x % 6 == 0;
                if (h == v) continue;                       // چهارراه یا بلوک
                if (h && x % 6 == 1) continue;              // جای خط عابر
                if (v && y % 6 == 1) continue;
                Vector3 p = CellToWorld(new Vector2Int(x, y));
                if (h) Box(new Vector3(p.x, 0.015f, p.z), new Vector3(2f, 0.03f, 0.15f), line);
                else Box(new Vector3(p.x, 0.015f, p.z), new Vector3(0.15f, 0.03f, 2f), line);
            }

        for (int j = 0; j <= curLand.by; j++)
            for (int i = 0; i <= curLand.bx; i++)
            {
                Vector3 c = CellToWorld(new Vector2Int(6 * i, 6 * j));
                if (i < curLand.bx)   // بازوی شرقی
                    for (int k = -2; k <= 2; k++)
                        Box(new Vector3(c.x + Cell, 0.02f, c.z + k * 0.7f), new Vector3(2.6f, 0.03f, 0.4f), zebra);
                if (j < curLand.by)   // بازوی جنوبی
                    for (int k = -2; k <= 2; k++)
                        Box(new Vector3(c.x + k * 0.7f, 0.02f, c.z - Cell), new Vector3(0.4f, 0.03f, 2.6f), zebra);
            }
    }

    // چراغ خیابان، درخت، نیمکت، سطل زباله، صندوق پست، شیر آتش‌نشانی، تیربرق و ماشین‌های پارک‌شده
    void BuildStreetProps(System.Random rng)
    {
        Color pole = new Color(0.2f, 0.2f, 0.23f);
        Color lamp = curLand.lamp;
        var hydrant = PropModels.Load("Models/Props/hydrant");
        double treeChance = curLand.tier == 1 ? 1.0 : (curLand.tier == 2 ? 0.7 : 0.5);

        for (int j = 0; j < curLand.by; j++)
            for (int i = 0; i < curLand.bx; i++)
            {
                // چراغ: دو گوشه‌ی مقابل هر بلوک (با بازوی خمیده)
                Vector3 a = CellToWorld(new Vector2Int(6 * i + 1, 6 * j + 1)) + new Vector3(-1.4f, 0f, 1.4f);
                Vector3 b = CellToWorld(new Vector2Int(6 * i + 5, 6 * j + 5)) + new Vector3(1.4f, 0f, -1.4f);
                foreach (var p in new[] { a, b })
                {
                    Cyl(new Vector3(p.x, 2.4f, p.z), new Vector3(0.12f, 2.4f, 0.12f), pole);
                    Box(new Vector3(p.x, 0.2f, p.z), new Vector3(0.4f, 0.4f, 0.4f), pole);
                    Box(new Vector3(p.x + 0.35f, 4.85f, p.z), new Vector3(0.9f, 0.08f, 0.1f), pole);
                    Ball(new Vector3(p.x + 0.75f, 4.75f, p.z), new Vector3(0.5f, 0.3f, 0.5f), lamp);
                }
                if (hydrant != null)
                {
                    Vector3 hp = CellToWorld(new Vector2Int(6 * i + 5, 6 * j + 1)) + new Vector3(1.2f, 0f, 1.2f);
                    PropModels.Spawn(hydrant, scn, hp, rng.Next(0, 4) * 90f, 1f, false, Color.white, false);
                }

                // درخت: ضلع غربی، شرقی و شمالی بلوک (ضلع جنوبی درِ مغازه‌هاست)
                var tp = new[]
                {
                    CellToWorld(new Vector2Int(6 * i + 1, 6 * j + 3)) + new Vector3(-1.3f, 0f, 0f),
                    CellToWorld(new Vector2Int(6 * i + 5, 6 * j + 3)) + new Vector3(1.3f, 0f, 0f),
                    CellToWorld(new Vector2Int(6 * i + 3, 6 * j + 1)) + new Vector3(0f, 0f, 1.3f)
                };
                foreach (var p in tp)
                {
                    if (rng.NextDouble() > treeChance) continue;
                    SpawnTree(new Vector3(p.x, 0f, p.z), rng.Next(0, 360), 0.85f + (float)rng.NextDouble() * 0.35f, rng);
                }

                // مبلمان شهری روی پیاده‌رو (جنوب غربی/شمال شرقی بلوک)
                Vector3 bc = CellToWorld(new Vector2Int(6 * i + 3, 6 * j + 3));
                if (rng.Next(0, 2) == 0) MakeBench(bc + new Vector3(-8.6f, 0f, 3.4f), 90f);
                if (rng.Next(0, 2) == 0) MakeTrashCan(bc + new Vector3(8.6f, 0f, -7.2f));
                if (rng.Next(0, 3) == 0) MakeMailbox(bc + new Vector3(8.7f, 0f, 6f));
                MakeBollards(bc + new Vector3(-9f, 0f, -9f));
            }

        // دریچه‌ی فاضلاب روی جاده
        for (int n = 0; n < (CW * CH) / 55; n++)
        {
            int x = rng.Next(0, CW), y = rng.Next(0, CH);
            if ((x % 6 == 0) == (y % 6 == 0)) continue;
            Vector3 p = CellToWorld(new Vector2Int(x, y)) + new Vector3((float)(rng.NextDouble() - 0.5) * 0.8f, 0f, (float)(rng.NextDouble() - 0.5) * 0.8f);
            Cyl(new Vector3(p.x, 0.02f, p.z), new Vector3(0.9f, 0.015f, 0.9f), new Color(0.22f, 0.22f, 0.24f));
        }

        // ماشین‌های پارک‌شده کنار جاده (به‌جز چهارراه‌ها)
        Color[] carCols =
        {
            new Color(0.75f, 0.1f, 0.1f), new Color(0.1f, 0.3f, 0.7f), new Color(0.85f, 0.85f, 0.85f),
            new Color(0.1f, 0.1f, 0.12f), new Color(0.9f, 0.7f, 0.1f), new Color(0.2f, 0.5f, 0.3f)
        };
        int want = Mathf.RoundToInt(12f * curLand.carDensity * (CW * CH) / 625f);
        var placed = new List<Vector3>();
        for (int tries = 0, n = 0; tries < 600 && n < want; tries++)
        {
            int x = rng.Next(0, CW), y = rng.Next(0, CH);
            bool h = y % 6 == 0, v = x % 6 == 0;
            if (h == v) continue;
            Vector3 p = CellToWorld(new Vector2Int(x, y));
            float side = rng.Next(0, 2) == 0 ? 1.1f : -1.1f;
            p += h ? new Vector3(0f, 0f, side) : new Vector3(side, 0f, 0f);
            bool near = false;
            foreach (var q in placed) if ((q - p).magnitude < 5f) near = true;
            if (near) continue;
            placed.Add(p);
            n++;
            MakeCar(p, h ? 0f : 90f, carCols[rng.Next(0, carCols.Length)]);
        }
    }

    void MakeBench(Vector3 p, float yaw)
    {
        var g = new GameObject("Bench").transform;
        g.SetParent(scn, false);
        g.localPosition = p; g.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Color wood = new Color(0.45f, 0.3f, 0.18f), iron = new Color(0.15f, 0.15f, 0.17f);
        Prim(PrimitiveType.Cube, "Seat", g, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.07f, 0.5f), wood, true);
        Prim(PrimitiveType.Cube, "Back", g, new Vector3(0f, 0.85f, -0.22f), new Vector3(1.8f, 0.4f, 0.06f), wood);
        Prim(PrimitiveType.Cube, "LegL", g, new Vector3(-0.8f, 0.25f, 0f), new Vector3(0.07f, 0.5f, 0.45f), iron);
        Prim(PrimitiveType.Cube, "LegR", g, new Vector3(0.8f, 0.25f, 0f), new Vector3(0.07f, 0.5f, 0.45f), iron);
    }

    void MakeTrashCan(Vector3 p)
    {
        Prim(PrimitiveType.Cylinder, "Bin", scn, p + new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.45f, 0.55f), new Color(0.2f, 0.32f, 0.22f), true);
        Prim(PrimitiveType.Cylinder, "BinLid", scn, p + new Vector3(0f, 0.92f, 0f), new Vector3(0.6f, 0.03f, 0.6f), new Color(0.12f, 0.12f, 0.14f));
    }

    void MakeMailbox(Vector3 p)
    {
        Prim(PrimitiveType.Cube, "Mailbox", scn, p + new Vector3(0f, 0.55f, 0f), new Vector3(0.55f, 1.1f, 0.5f), new Color(0.1f, 0.2f, 0.6f), true);
        Prim(PrimitiveType.Cube, "MailSlot", scn, p + new Vector3(0f, 0.85f, -0.26f), new Vector3(0.4f, 0.05f, 0.02f), Color.black);
    }

    void MakeBollards(Vector3 p)
    {
        for (int k = 0; k < 3; k++)
            Prim(PrimitiveType.Cylinder, "Bollard", scn, p + new Vector3(k * 0.9f, 0.4f, 0f), new Vector3(0.16f, 0.4f, 0.16f), new Color(0.9f, 0.75f, 0.1f), true);
    }

    void MakeCar(Vector3 pos, float yaw, Color col)
    {
        var def = PropModels.Load("Models/Props/defender");
        if (def != null)
        {
            // yaw قدیمی: 0 = راستای X، 90 = راستای Z. مدل Defender جلوش +Z هست.
            float y = (90f - yaw) + (Random.value < 0.5f ? 0f : 180f);
            PropModels.Spawn(def, scn, pos, y, 1f, true, col, true);
            return;
        }
        var car = new GameObject("Car").transform;
        car.SetParent(scn, false);
        car.localPosition = pos;
        car.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Prim(PrimitiveType.Cube, "CarBody", car, new Vector3(0f, 0.65f, 0f), new Vector3(3.9f, 0.7f, 1.8f), col, true);
        Prim(PrimitiveType.Cube, "CarCabin", car, new Vector3(-0.2f, 1.25f, 0f), new Vector3(2.1f, 0.6f, 1.6f), new Color(0.15f, 0.2f, 0.28f), true);
        Color tire = new Color(0.06f, 0.06f, 0.07f);
        foreach (float wx in new[] { -1.25f, 1.25f })
            foreach (float wz in new[] { -0.9f, 0.9f })
            {
                var w = Prim(PrimitiveType.Cylinder, "Wheel", car, new Vector3(wx, 0.32f, wz), new Vector3(0.64f, 0.1f, 0.64f), tire);
                w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        Prim(PrimitiveType.Cube, "HeadLights", car, new Vector3(1.96f, 0.7f, 0f), new Vector3(0.05f, 0.18f, 1.4f), new Color(1f, 0.95f, 0.7f));
        Prim(PrimitiveType.Cube, "TailLights", car, new Vector3(-1.96f, 0.7f, 0f), new Vector3(0.05f, 0.18f, 1.4f), new Color(0.8f, 0.05f, 0.05f));
    }

    void BuildBusStops()
    {
        var bus = PropModels.Load("Models/Props/busstop");
        if (bus == null) return;
        Vector3 b1 = CellToWorld(new Vector2Int(7, 6 * Mathf.Min(2, curLand.by - 1) + 3)) + new Vector3(-1.1f, 0f, 0f);
        Vector3 b2 = CellToWorld(new Vector2Int(6 * Mathf.Min(3, curLand.bx - 1) + 5, 9)) + new Vector3(1.1f, 0f, 0f);
        PropModels.Spawn(bus, scn, b1, 0f, 1f, false, Color.white, true);
        PropModels.Spawn(bus, scn, b2, 180f, 1f, false, Color.white, true);
    }

    // دروازه‌ی خروجی (لبه‌ی شرقی)
    void BuildExitGate()
    {
        Vector3 ex = CellToWorld(new Vector2Int(CW - 1, ExitRow));
        Color gate = new Color(0.2f, 1f, 0.4f);
        Box(new Vector3(ex.x, 3f, ex.z - 3.2f), new Vector3(0.6f, 6f, 0.6f), new Color(0.25f, 0.25f, 0.28f));
        Box(new Vector3(ex.x, 3f, ex.z + 3.2f), new Vector3(0.6f, 6f, 0.6f), new Color(0.25f, 0.25f, 0.28f));
        Box(new Vector3(ex.x, 6.1f, ex.z), new Vector3(0.6f, 0.6f, 7.4f), gate);
        MakeSpot(SpotType.Exit, ex, gate, "EXIT -> BASE");
    }


    // ---------------------------------------------------------------- Sea (به‌جای حصار دور لند)
    class Bobber { public Transform t; public Vector3 pos; public float phase, yaw; }
    readonly List<Bobber> bobbers = new List<Bobber>();
    Transform waveA, waveB;
    Vector3 waveABase, waveBBase;
    Texture2D waveTex;
    const float WaveTileA = 24f, WaveTileB = 37f;

    Texture2D WaveTexture()
    {
        if (waveTex != null) return waveTex;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)n * Mathf.PI * 2f, v = y / (float)n * Mathf.PI * 2f;
                float a = Mathf.Sin(u * 3f + Mathf.Sin(v * 2f) * 1.4f) * Mathf.Sin(v * 3f + Mathf.Sin(u * 2f) * 1.2f);   // بدون درز (periodic)
                float c = Mathf.Clamp01((a - 0.3f) * 2.2f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(c * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        waveTex = tex;
        return tex;
    }

    Mesh WaveMesh(float size, float tile)
    {
        float h = size * 0.5f, t = size / tile;
        var m = new Mesh();
        m.vertices = new[] { new Vector3(-h, 0f, -h), new Vector3(-h, 0f, h), new Vector3(h, 0f, h), new Vector3(h, 0f, -h) };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, t), new Vector2(t, t), new Vector2(t, 0f) };
        m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.bounds = new Bounds(Vector3.zero, new Vector3(size, 1f, size));
        return m;
    }

    Transform MakeWaveLayer(float y, float tile, Color tint, Vector3 center)
    {
        var go = new GameObject("SeaWaves");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(center.x, y, center.z);
        go.AddComponent<MeshFilter>().sharedMesh = WaveMesh(1400f, tile);
        var mr = go.AddComponent<MeshRenderer>();
        var sh = Shader.Find("Sprites/Default");
        var m = new Material(sh != null ? sh : PipeShader());
        m.mainTexture = WaveTexture();
        m.color = tint;
        mr.sharedMaterial = m;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return go.transform;
    }

    // دریا دور تا دور لند: اسکله‌ی سنگی، نرده، موج، قایق‌های شناور و فانوس دریایی. بازیکن پشت نرده (دیوار نامرئی) میمونه.
    void BuildSea(System.Random rng)
    {
        float midX = (CW - 1) * Cell / 2f, midZ = (CH - 1) * Cell / 2f;
        float gx = (CW - 1) * Cell + 14f, gz = (CH - 1) * Cell + 14f;      // اندازه‌ی زمین
        Vector3 center = new Vector3(midX, 0f, -midZ);
        Color deep = Color.Lerp(new Color(0.06f, 0.27f, 0.44f), curLand.fog, 0.18f);
        Color quay = new Color(0.55f, 0.56f, 0.6f);

        // آب
        Prim(PrimitiveType.Cube, "Sea", root, new Vector3(midX, -1.1f, -midZ), new Vector3(1400f, 0.2f, 1400f), deep);
        waveABase = new Vector3(center.x, -0.98f, center.z);
        waveBBase = new Vector3(center.x, -0.95f, center.z);
        waveA = MakeWaveLayer(waveABase.y, WaveTileA, new Color(0.85f, 0.95f, 1f, 0.22f), center);
        waveB = MakeWaveLayer(waveBBase.y, WaveTileB, new Color(0.7f, 0.9f, 1f, 0.14f), center);

        // دیواره‌ی اسکله دور زمین + کف‌های سفید کنار دیواره
        float hx = gx / 2f, hz = gz / 2f;
        Box(new Vector3(midX, -0.5f, -midZ + hz + 0.35f), new Vector3(gx + 1.4f, 1.2f, 0.7f), quay);
        Box(new Vector3(midX, -0.5f, -midZ - hz - 0.35f), new Vector3(gx + 1.4f, 1.2f, 0.7f), quay);
        Box(new Vector3(midX + hx + 0.35f, -0.5f, -midZ), new Vector3(0.7f, 1.2f, gz), quay);
        Box(new Vector3(midX - hx - 0.35f, -0.5f, -midZ), new Vector3(0.7f, 1.2f, gz), quay);
        Color foam = new Color(0.92f, 0.96f, 1f);
        Box(new Vector3(midX, -0.95f, -midZ + hz + 1.0f), new Vector3(gx + 2.4f, 0.04f, 0.5f), foam);
        Box(new Vector3(midX, -0.95f, -midZ - hz - 1.0f), new Vector3(gx + 2.4f, 0.04f, 0.5f), foam);
        Box(new Vector3(midX + hx + 1.0f, -0.95f, -midZ), new Vector3(0.5f, 0.04f, gz + 2.4f), foam);
        Box(new Vector3(midX - hx - 1.0f, -0.95f, -midZ), new Vector3(0.5f, 0.04f, gz + 2.4f), foam);
        // خط زرد لبه‌ی اسکله
        Color yellow = new Color(0.95f, 0.8f, 0.15f);
        Box(new Vector3(midX, 0.01f, -midZ + hz - 0.2f), new Vector3(gx, 0.02f, 0.25f), yellow);
        Box(new Vector3(midX, 0.01f, -midZ - hz + 0.2f), new Vector3(gx, 0.02f, 0.25f), yellow);
        Box(new Vector3(midX + hx - 0.2f, 0.01f, -midZ), new Vector3(0.25f, 0.02f, gz), yellow);
        Box(new Vector3(midX - hx + 0.2f, 0.01f, -midZ), new Vector3(0.25f, 0.02f, gz), yellow);

        // نرده + دیوار نامرئی روی خط قدیمیِ حصار
        float xMin = -3f, xMax = (CW - 1) * Cell + 3f, zMax = 3f, zMin = -((CH - 1) * Cell + 3f);
        float lenX = xMax - xMin, lenZ = zMax - zMin;
        Color iron = new Color(0.16f, 0.17f, 0.2f);
        MakeRail(new Vector3((xMin + xMax) / 2f, 0f, zMax), lenX, true, iron);
        MakeRail(new Vector3((xMin + xMax) / 2f, 0f, zMin), lenX, true, iron);
        MakeRail(new Vector3(xMin, 0f, (zMin + zMax) / 2f), lenZ, false, iron);
        MakeRail(new Vector3(xMax, 0f, (zMin + zMax) / 2f), lenZ, false, iron);
        for (int k = 0; k < 4; k++)
        {
            var g = new GameObject("SeaBarrier");
            g.transform.SetParent(root, false);
            bool horiz = k < 2;
            g.transform.position = k == 0 ? new Vector3((xMin + xMax) / 2f, 2f, zMax + 0.5f)
                : k == 1 ? new Vector3((xMin + xMax) / 2f, 2f, zMin - 0.5f)
                : k == 2 ? new Vector3(xMin - 0.5f, 2f, (zMin + zMax) / 2f) : new Vector3(xMax + 0.5f, 2f, (zMin + zMax) / 2f);
            g.AddComponent<BoxCollider>().size = horiz ? new Vector3(lenX + 4f, 4f, 1f) : new Vector3(1f, 4f, lenZ + 4f);
        }

        // بولارد دور اسکله
        for (int i = 0; i < 14; i++)
        {
            int side = rng.Next(0, 4);
            float t = (float)rng.NextDouble();
            Vector3 p = side == 0 ? new Vector3(xMin + t * lenX, 0f, zMax + 2.2f) : side == 1 ? new Vector3(xMin + t * lenX, 0f, zMin - 2.2f)
                : side == 2 ? new Vector3(xMin - 2.2f, 0f, zMin + t * lenZ) : new Vector3(xMax + 2.2f, 0f, zMin + t * lenZ);
            Cyl(p + new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.12f, 0.12f, 0.14f));
            Cyl(p + new Vector3(0f, 0.62f, 0f), new Vector3(0.38f, 0.04f, 0.38f), yellow);
        }

        // قایق‌ها و شناورها
        int boats = curLandIdx == 1 ? 11 : 6;
        Color[] hulls = { new Color(0.85f, 0.85f, 0.88f), new Color(0.75f, 0.15f, 0.15f), new Color(0.15f, 0.35f, 0.65f), new Color(0.9f, 0.75f, 0.2f), new Color(0.2f, 0.2f, 0.24f) };
        for (int i = 0; i < boats; i++)
        {
            int side = rng.Next(0, 4);
            float off = 14f + (float)rng.NextDouble() * 70f, t = (float)rng.NextDouble();
            Vector3 p = side == 0 ? new Vector3(xMin + t * lenX, 0f, zMax + off) : side == 1 ? new Vector3(xMin + t * lenX, 0f, zMin - off)
                : side == 2 ? new Vector3(xMin - off, 0f, zMin + t * lenZ) : new Vector3(xMax + off, 0f, zMin + t * lenZ);
            MakeBoat(p, rng.Next(0, 360), hulls[rng.Next(0, hulls.Length)], rng.Next(0, 3) == 0);
        }
        for (int i = 0; i < 12; i++)
        {
            int side = rng.Next(0, 4);
            float off = 10f + (float)rng.NextDouble() * 50f, t = (float)rng.NextDouble();
            Vector3 p = side == 0 ? new Vector3(xMin + t * lenX, -0.9f, zMax + off) : side == 1 ? new Vector3(xMin + t * lenX, -0.9f, zMin - off)
                : side == 2 ? new Vector3(xMin - off, -0.9f, zMin + t * lenZ) : new Vector3(xMax + off, -0.9f, zMin + t * lenZ);
            var buoy = new GameObject("Buoy").transform;
            buoy.SetParent(root, false);
            Prim(PrimitiveType.Sphere, "BuoyBody", buoy, new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.9f, 0.2f, 0.15f));
            Prim(PrimitiveType.Cylinder, "BuoyTop", buoy, new Vector3(0f, 0.95f, 0f), new Vector3(0.12f, 0.35f, 0.12f), new Color(0.95f, 0.95f, 0.95f));
            AddBobber(buoy, p, 0f);
        }

        // فانوس دریایی در یه گوشه، دور از لند
        float lx = rng.Next(0, 2) == 0 ? xMin - 85f : xMax + 85f, lz = rng.Next(0, 2) == 0 ? zMax + 85f : zMin - 85f;
        for (int k = 0; k < 5; k++)
            Prim(PrimitiveType.Cylinder, "Lighthouse", root, new Vector3(lx, 3f + k * 6f, lz), new Vector3(7f - k * 0.8f, 3f, 7f - k * 0.8f), k % 2 == 0 ? new Color(0.9f, 0.2f, 0.15f) : new Color(0.95f, 0.95f, 0.95f));
        Prim(PrimitiveType.Cylinder, "LighthouseRoom", root, new Vector3(lx, 31.5f, lz), new Vector3(3.6f, 1.2f, 3.6f), new Color(0.2f, 0.2f, 0.24f));
        Prim(PrimitiveType.Sphere, "LighthouseLamp", root, new Vector3(lx, 31.5f, lz), new Vector3(2.4f, 2.4f, 2.4f), new Color(1f, 0.95f, 0.6f));
        Prim(PrimitiveType.Sphere, "LighthouseRock", root, new Vector3(lx, -1.0f, lz), new Vector3(16f, 6f, 16f), new Color(0.3f, 0.3f, 0.33f));
    }

    // نرده: دو میله‌ی افقی + تیرک هر ۴ متر
    void MakeRail(Vector3 center, float length, bool alongX, Color c)
    {
        Vector3 s = alongX ? new Vector3(length, 0.07f, 0.07f) : new Vector3(0.07f, 0.07f, length);
        Box(center + new Vector3(0f, 0.55f, 0f), s, c);
        Box(center + new Vector3(0f, 1.05f, 0f), s, c);
        int n = Mathf.Max(2, Mathf.RoundToInt(length / 4f));
        for (int i = 0; i <= n; i++)
        {
            float t = -length / 2f + length * i / n;
            Vector3 p = center + (alongX ? new Vector3(t, 0f, 0f) : new Vector3(0f, 0f, t));
            Prim(PrimitiveType.Cylinder, "Post", scn, p + new Vector3(0f, 0.55f, 0f), new Vector3(0.1f, 0.55f, 0.1f), c);
        }
    }

    void AddBobber(Transform t, Vector3 pos, float yaw)
    {
        bobbers.Add(new Bobber { t = t, pos = pos, phase = Random.value * 6.28f, yaw = yaw });
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void MakeBoat(Vector3 pos, float yaw, Color hull, bool sail)
    {
        var b = new GameObject("Boat").transform;
        b.SetParent(root, false);
        Prim(PrimitiveType.Cube, "Hull", b, new Vector3(0f, -0.3f, 0f), new Vector3(8f, 1.3f, 2.8f), hull);
        Prim(PrimitiveType.Cube, "Bow", b, new Vector3(4.3f, -0.15f, 0f), new Vector3(1.6f, 1.0f, 1.6f), hull);
        Prim(PrimitiveType.Cube, "Deck", b, new Vector3(0f, 0.38f, 0f), new Vector3(7.6f, 0.1f, 2.5f), new Color(0.55f, 0.4f, 0.25f));
        if (sail)
        {
            Prim(PrimitiveType.Cylinder, "Mast", b, new Vector3(0.5f, 4f, 0f), new Vector3(0.14f, 4f, 0.14f), new Color(0.8f, 0.8f, 0.8f));
            Prim(PrimitiveType.Cube, "Sail", b, new Vector3(-0.6f, 4.2f, 0f), new Vector3(2.6f, 5f, 0.05f), new Color(0.95f, 0.95f, 0.92f));
        }
        else
        {
            Prim(PrimitiveType.Cube, "Cabin", b, new Vector3(-1f, 1.2f, 0f), new Vector3(3.2f, 1.6f, 2.1f), new Color(0.92f, 0.92f, 0.94f));
            Prim(PrimitiveType.Cube, "CabinWin", b, new Vector3(-1f, 1.45f, 1.06f), new Vector3(2.4f, 0.5f, 0.04f), new Color(0.15f, 0.2f, 0.3f));
            Prim(PrimitiveType.Cube, "CabinWin", b, new Vector3(-1f, 1.45f, -1.06f), new Vector3(2.4f, 0.5f, 0.04f), new Color(0.15f, 0.2f, 0.3f));
            Prim(PrimitiveType.Cylinder, "Funnel", b, new Vector3(-1.8f, 2.4f, 0f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.8f, 0.2f, 0.15f));
        }
        AddBobber(b, new Vector3(pos.x, -0.55f, pos.z), yaw);
    }

    // موج‌ها آروم میلغزن و قایق‌ها بالا و پایین میرن
    void UpdateSea()
    {
        float t = Time.time;
        if (waveA != null) waveA.position = waveABase + new Vector3((t * 0.9f) % WaveTileA, 0f, (t * 0.5f) % WaveTileA);
        if (waveB != null) waveB.position = waveBBase + new Vector3(-((t * 0.6f) % WaveTileB), 0f, (t * 0.8f) % WaveTileB);
        for (int i = 0; i < bobbers.Count; i++)
        {
            var b = bobbers[i];
            if (b.t == null) continue;
            b.t.position = b.pos + new Vector3(0f, Mathf.Sin(t * 1.2f + b.phase) * 0.12f, 0f);
            b.t.rotation = Quaternion.Euler(Mathf.Sin(t * 0.9f + b.phase) * 2f, b.yaw, Mathf.Sin(t * 1.1f + b.phase) * 2.5f);
        }
    }

    // ---------------------------------------------------------------- Park & container yard
    void BuildPark(Vector2Int bmin, System.Random rng)
    {
        Vector3 c = CellToWorld(new Vector2Int(bmin.x + 1, bmin.y + 1));
        Color grass = curLand.grass * (0.9f + (float)rng.NextDouble() * 0.2f);
        Box(c + new Vector3(0f, 0.08f, 0f), new Vector3(19.2f, 0.1f, 19.2f), new Color(grass.r, grass.g, grass.b, 1f));
        Color path = new Color(0.72f, 0.7f, 0.66f);
        Box(c + new Vector3(0f, 0.12f, 0f), new Vector3(19.2f, 0.04f, 2f), path);
        Box(c + new Vector3(0f, 0.12f, 0f), new Vector3(2f, 0.04f, 19.2f), path);

        // فواره
        Color stone = new Color(0.62f, 0.62f, 0.66f);
        Prim(PrimitiveType.Cylinder, "FountainBasin", scn, c + new Vector3(0f, 0.2f, 0f), new Vector3(5.2f, 0.25f, 5.2f), stone, true);
        Prim(PrimitiveType.Cylinder, "FountainWater", scn, c + new Vector3(0f, 0.42f, 0f), new Vector3(4.6f, 0.02f, 4.6f), new Color(0.25f, 0.6f, 0.85f));
        Prim(PrimitiveType.Cylinder, "FountainPillar", scn, c + new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.7f, 0.6f), stone);
        Prim(PrimitiveType.Cylinder, "FountainBowl", scn, c + new Vector3(0f, 1.6f, 0f), new Vector3(1.8f, 0.1f, 1.8f), stone);
        Prim(PrimitiveType.Sphere, "FountainTop", scn, c + new Vector3(0f, 1.9f, 0f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.6f, 0.85f, 1f));

        foreach (var s in new[] { new Vector2(4.8f, 4.8f), new Vector2(-4.8f, 4.8f), new Vector2(4.8f, -4.8f), new Vector2(-4.8f, -4.8f) })
        {
            MakeBench(c + new Vector3(s.x, 0.1f, s.y), (s.x * s.y > 0f) ? 45f : 135f);
            Prim(PrimitiveType.Cube, "FlowerBed", scn, c + new Vector3(s.x * 1.6f, 0.17f, s.y * 0.55f), new Vector3(1.6f, 0.2f, 1f), new Color(0.3f, 0.2f, 0.12f));
            for (int f = 0; f < 4; f++)
                Ball(c + new Vector3(s.x * 1.6f - 0.5f + f * 0.33f, 0.4f, s.y * 0.55f), Vector3.one * 0.24f, f % 2 == 0 ? curLand.accent : new Color(1f, 0.85f, 0.3f));
        }

        for (int k = 0; k < 8; k++)
        {
            float ang = k * 45f * Mathf.Deg2Rad;
            Vector3 p = c + new Vector3(Mathf.Cos(ang) * 8.2f, 0.08f, Mathf.Sin(ang) * 8.2f);
            SpawnTree(p, rng.Next(0, 360), 0.95f + (float)rng.NextDouble() * 0.4f, rng);
        }
    }

    void BuildYard(Vector2Int bmin, System.Random rng)
    {
        Vector3 c = CellToWorld(new Vector2Int(bmin.x + 1, bmin.y + 1));
        Box(c + new Vector3(0f, 0.09f, 0f), new Vector3(19.2f, 0.06f, 19.2f), new Color(0.42f, 0.43f, 0.45f));
        for (int k = -3; k <= 3; k++) Box(c + new Vector3(k * 2.6f, 0.125f, 9f), new Vector3(1.4f, 0.02f, 0.2f), new Color(0.9f, 0.8f, 0.1f));   // خط‌کشی

        Color[] cols =
        {
            new Color(0.75f, 0.2f, 0.15f), new Color(0.15f, 0.35f, 0.65f), new Color(0.2f, 0.5f, 0.3f),
            new Color(0.85f, 0.6f, 0.15f), new Color(0.45f, 0.45f, 0.5f)
        };
        var slots = new[] { new Vector3(-3.6f, 0f, -3.8f), new Vector3(3.6f, 0f, -3.8f), new Vector3(-3.6f, 0f, 3.8f), new Vector3(3.6f, 0f, 3.8f) };
        foreach (var s in slots)
        {
            if (rng.Next(0, 5) == 0) continue;
            Color cc = cols[rng.Next(0, cols.Length)];
            MakeShippingContainer(c + s, 0f, cc);
            if (rng.Next(0, 2) == 0) MakeShippingContainer(c + s + new Vector3(0f, 2.65f, 0f), 0f, cols[rng.Next(0, cols.Length)]);
        }
        // پالت و بشکه
        for (int k = 0; k < 4; k++)
        {
            Vector3 p = c + new Vector3(-1.5f + k * 1.1f, 0f, (rng.Next(0, 2) == 0 ? -0.4f : 0.4f));
            Prim(PrimitiveType.Cylinder, "Barrel", scn, p + new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.45f, 0.6f), new Color(0.15f + 0.1f * k, 0.25f, 0.5f), true);
        }
        Box(c + new Vector3(0f, 0.12f, 0f), new Vector3(1.4f, 0.14f, 1.4f), new Color(0.55f, 0.4f, 0.22f));
        Box(c + new Vector3(0.1f, 0.5f, 0.05f), new Vector3(1.1f, 0.6f, 1.1f), new Color(0.6f, 0.45f, 0.25f), true);
        // نیوبری (مانع بتنی)
        foreach (var s in new[] { -7.5f, 7.5f })
            Box(c + new Vector3(s, 0.4f, 0f), new Vector3(0.5f, 0.8f, 4f), new Color(0.7f, 0.7f, 0.72f), true);
    }

    void MakeShippingContainer(Vector3 pos, float yaw, Color col)
    {
        var g = new GameObject("Container").transform;
        g.SetParent(scn, false);
        g.localPosition = pos; g.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Prim(PrimitiveType.Cube, "Body", g, new Vector3(0f, 1.3f, 0f), new Vector3(6f, 2.6f, 2.4f), col, true);
        Color rib = col * 0.78f;
        for (int k = -5; k <= 5; k++)
        {
            Prim(PrimitiveType.Cube, "Rib", g, new Vector3(k * 0.5f, 1.3f, 1.21f), new Vector3(0.12f, 2.4f, 0.04f), rib);
            Prim(PrimitiveType.Cube, "Rib", g, new Vector3(k * 0.5f, 1.3f, -1.21f), new Vector3(0.12f, 2.4f, 0.04f), rib);
        }
        Prim(PrimitiveType.Cube, "DoorBar", g, new Vector3(3.01f, 1.3f, 0.45f), new Vector3(0.04f, 2.3f, 0.06f), new Color(0.15f, 0.15f, 0.17f));
        Prim(PrimitiveType.Cube, "DoorBar", g, new Vector3(3.01f, 1.3f, -0.45f), new Vector3(0.04f, 2.3f, 0.06f), new Color(0.15f, 0.15f, 0.17f));
    }

    // ---------------------------------------------------------------- Buildings
    void BuildBuilding(Vector2Int bmin, int role, System.Random rng)
    {
        Vector3 ctr = CellToWorld(new Vector2Int(bmin.x + 1, bmin.y + 1));
        const float half = 5.8f;
        int floors = role == 3 ? 4 : 2 + rng.Next(0, 3);
        if (curLand.tier >= 2 && role != 4) floors += 1 + (curLand.tier - 2);   // لندهای پیشرفته‌تر بلندتر
        float h = 3.6f * floors;

        Color wall;
        switch (role)
        {
            case 0: wall = new Color(0.82f, 0.74f, 0.58f); break;      // خواروبار: کِرم
            case 1: wall = new Color(0.45f, 0.5f, 0.6f); break;        // الکترونیک: فولادی
            case 2: wall = new Color(0.68f, 0.52f, 0.48f); break;      // جواهری: رز
            case 3: wall = new Color(0.82f, 0.8f, 0.74f); break;       // بانک: سنگ
            case 4: wall = new Color(0.27f, 0.21f, 0.32f); break;      // بلک‌مارکت: بنفش تیره
            default: wall = rng.Next(0, 2) == 0 ? new Color(0.58f, 0.3f, 0.25f) : new Color(0.7f, 0.65f, 0.55f); break;  // آجری / بژ
        }
        Color trim = Color.Lerp(wall, Color.white, 0.4f);
        Color dark = Color.Lerp(wall, Color.black, 0.5f);

        float wallTile;
        var wallMat = WallMat(role, rng, out wallTile);
        bool hollow = role <= 3;      // مغازه‌های قابل ورود

        if (hollow)
        {
            var sh = new ShopInfo { role = role, name = ShopNames[role], ctr = ctr };
            shops.Add(sh);
            BuildShopShell(sh, wallMat, wallTile, floors, dark, trim);
        }
        else
            TexBox(ctr + new Vector3(0f, h / 2f, 0f), new Vector3(2f * half, h, 2f * half), wallMat, wallTile, true);

        if (hollow)
        {   // پلینت فقط دور ساختمان (داخل مغازه خالیه)
            Box(ctr + new Vector3(0f, 0.35f, half + 0.075f), new Vector3(2f * half + 0.3f, 0.7f, 0.15f), dark);
            Box(ctr + new Vector3(half + 0.075f, 0.35f, 0f), new Vector3(0.15f, 0.7f, 2f * half), dark);
            Box(ctr + new Vector3(-half - 0.075f, 0.35f, 0f), new Vector3(0.15f, 0.7f, 2f * half), dark);
            Box(ctr + new Vector3(-3.6f, 0.35f, -half - 0.075f), new Vector3(4.8f, 0.7f, 0.15f), dark);
            Box(ctr + new Vector3(3.6f, 0.35f, -half - 0.075f), new Vector3(4.8f, 0.7f, 0.15f), dark);
        }
        else Box(ctr + new Vector3(0f, 0.35f, 0f), new Vector3(2f * half + 0.3f, 0.7f, 2f * half + 0.3f), dark);
        for (int k = 1; k < floors; k++)
            Box(ctr + new Vector3(0f, 3.6f * k + (hollow && k == 1 ? 0.25f : 0f), 0f), new Vector3(2f * half + 0.2f, 0.16f, 2f * half + 0.2f), trim);
        Box(ctr + new Vector3(0f, h + 0.2f, 0f), new Vector3(2f * half + 0.5f, 0.4f, 2f * half + 0.5f), trim);   // قرنیز بالا
        // لوله‌ی ناودان در دو گوشه
        Cyl(ctr + new Vector3(half + 0.1f, h / 2f, half + 0.1f), new Vector3(0.14f, h / 2f, 0.14f), dark);
        Cyl(ctr + new Vector3(-half - 0.1f, h / 2f, half + 0.1f), new Vector3(0.14f, h / 2f, 0.14f), dark);

        BuildWindows(ctr, half, floors, role, rng);
        BuildRoof(ctr, h, role, rng, dark);
        if (role <= 4) BuildStorefront(ctr, half, role, wall, trim, dark, hollow);
        else BuildApartmentFront(ctr, half, floors, trim, dark, rng);
        if (hollow) BuildSideDetails(ctr, half, rng, dark);
        else if (role == 5) BuildFireEscape(ctr, half, floors, dark);
    }

    // مغازه‌ها: طبقه‌ی همکف پنجره‌ی واقعی داره (تو پوسته‌ی مغازه)، پس از k=1 شروع میشه
    void BuildWindows(Vector3 ctr, float half, int floors, int role, System.Random rng)
    {
        // 0 جنوب (سمت در)، 1 شمال، 2 شرق، 3 غرب
        for (int face = 0; face < 4; face++)
        {
            Vector3 n = face == 0 ? new Vector3(0f, 0f, -1f) : face == 1 ? new Vector3(0f, 0f, 1f) : face == 2 ? new Vector3(1f, 0f, 0f) : new Vector3(-1f, 0f, 0f);
            Vector3 t = face < 2 ? new Vector3(1f, 0f, 0f) : new Vector3(0f, 0f, 1f);
            float[] offs = face < 2 ? new[] { -4.2f, -1.4f, 1.4f, 4.2f } : new[] { -3.6f, 0f, 3.6f };
            Vector3 size = face < 2 ? new Vector3(1.5f, 1.7f, 0.12f) : new Vector3(0.12f, 1.7f, 1.5f);
            Vector3 fsize = face < 2 ? new Vector3(1.8f, 2.0f, 0.08f) : new Vector3(0.08f, 2.0f, 1.8f);
            Vector3 sill = face < 2 ? new Vector3(1.9f, 0.1f, 0.3f) : new Vector3(0.3f, 0.1f, 1.9f);

            int k0 = 0;
            if (role <= 3) k0 = 1;                       // مغازه‌ی قابل ورود
            else if (role == 4 && face == 0) k0 = 1;     // بلک‌مارکت: جلو ویترین داره
            for (int k = k0; k < floors; k++)
                foreach (float o in offs)
                {
                    float y = 3.6f * k + 2.1f;
                    Color glass = rng.NextDouble() < 0.4 ? LitWindow : new Color(0.15f + (float)rng.NextDouble() * 0.06f, 0.2f, 0.28f);
                    Vector3 pos = ctr + n * (half + 0.03f) + t * o + new Vector3(0f, y, 0f);
                    Box(pos, size, glass);
                    Box(ctr + n * (half + 0.01f) + t * o + new Vector3(0f, y, 0f), fsize, new Color(0.1f, 0.1f, 0.12f));
                    Box(ctr + n * (half + 0.12f) + t * o + new Vector3(0f, y - 1.05f, 0f), sill, new Color(0.75f, 0.75f, 0.78f));   // زیرپنجره
                }
        }
    }

    void BuildRoof(Vector3 ctr, float h, int role, System.Random rng, Color dark)
    {
        Color metal = new Color(0.55f, 0.57f, 0.6f);
        int units = 1 + rng.Next(0, 3);
        for (int i = 0; i < units; i++)
        {
            Vector3 p = ctr + new Vector3(-3.5f + (float)rng.NextDouble() * 7f, h + 0.8f, -3.5f + (float)rng.NextDouble() * 7f);
            Box(p, new Vector3(1.4f, 0.8f, 1.1f), metal);
            Cyl(p + new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.04f, 0.7f), dark);
        }
        if (rng.Next(0, 2) == 0)
        {   // مخزن آب
            Vector3 p = ctr + new Vector3(3f, h + 1.4f, 3f);
            Cyl(p, new Vector3(1.6f, 0.9f, 1.6f), new Color(0.45f, 0.32f, 0.22f));
            Box(p + new Vector3(0f, -1.2f, 0f), new Vector3(0.2f, 0.6f, 1.4f), dark);
        }
        if (rng.Next(0, 2) == 0 || role == 4)
        {   // آنتن
            Vector3 p = ctr + new Vector3(-3f, h + 1.8f, 3.5f);
            Cyl(p, new Vector3(0.05f, 1.6f, 0.05f), dark);
            Box(p + new Vector3(0f, 0.9f, 0f), new Vector3(1.2f, 0.05f, 0.05f), dark);
        }
        if (role == 3 || (curLand.tier >= 2 && rng.Next(0, 3) == 0))
        {   // بیلبورد پشت‌بام
            Box(ctr + new Vector3(0f, h + 2.2f, 4.6f), new Vector3(7f, 2.4f, 0.2f), dark);
            Box(ctr + new Vector3(0f, h + 2.2f, 4.48f), new Vector3(6.6f, 2.0f, 0.08f), role == 3 ? new Color(0.85f, 0.75f, 0.35f) : curLand.accent);
            Box(ctr + new Vector3(-2.5f, h + 0.6f, 4.6f), new Vector3(0.2f, 1.2f, 0.2f), dark);
            Box(ctr + new Vector3(2.5f, h + 0.6f, 4.6f), new Vector3(0.2f, 1.2f, 0.2f), dark);
        }
        Box(ctr + new Vector3(-4.2f, h + 0.9f, -4.2f), new Vector3(1.6f, 1.4f, 1.6f), Color.Lerp(dark, Color.gray, 0.4f));  // اتاقک راه‌پله
    }

    // سایبان راه‌راه، تابلو، پله، گلدان، چراغ و پنل آلارم. hollow = در و ویترین واقعی تو پوسته ساخته شده
    void BuildStorefront(Vector3 ctr, float half, int role, Color wall, Color trim, Color dark, bool hollow)
    {
        Color accent = role == 4 ? MarketColor : ShopColors[role];
        float zf = ctr.z - half - 0.04f;      // سطح نمای جنوبی
        Color glass = Color.Lerp(new Color(0.12f, 0.17f, 0.24f), accent, 0.18f);

        if (!hollow)   // بلک‌مارکت: در و ویترین تزئینی
        {
            Box(new Vector3(ctr.x, 1.45f, zf), new Vector3(2.1f, 2.9f, 0.18f), trim);
            Box(new Vector3(ctr.x, 1.25f, zf - 0.06f), new Vector3(1.6f, 2.5f, 0.12f), new Color(0.2f, 0.14f, 0.1f));
            Box(new Vector3(ctr.x + 0.55f, 1.2f, zf - 0.14f), new Vector3(0.08f, 0.08f, 0.06f), new Color(0.9f, 0.8f, 0.3f));
            foreach (float sx in new[] { -3.5f, 3.5f })
            {
                Box(new Vector3(ctr.x + sx, 1.75f, zf), new Vector3(3.5f, 2.5f, 0.12f), dark);
                Box(new Vector3(ctr.x + sx, 1.75f, zf - 0.05f), new Vector3(3.2f, 2.2f, 0.1f), glass);
                Box(new Vector3(ctr.x + sx, 0.45f, zf - 0.06f), new Vector3(3.5f, 0.5f, 0.14f), trim);
            }
        }

        // سایبان راه‌راه
        for (int i = 0; i < 6; i++)
        {
            var s = Box(new Vector3(ctr.x - 3.75f + i * 1.5f, 3.15f, zf - 0.85f), new Vector3(1.5f, 0.1f, 1.8f), i % 2 == 0 ? accent : Color.white);
            s.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
        }

        // تابلو (متن سه‌بعدی)
        Box(new Vector3(ctr.x, 4.55f, zf - 0.1f), new Vector3(7.4f, 1.25f, 0.25f), dark);
        Box(new Vector3(ctr.x, 4.55f, zf - 0.25f), new Vector3(7f, 0.9f, 0.1f), accent);
        string txt = role == 4 ? "BLACK MARKET" : SignTexts[role];
        Sign3D(txt, new Vector3(ctr.x, 4.55f, zf - 0.34f), 0f, 6.4f, 0.7f, new Color(0.06f, 0.06f, 0.08f));

        // پله و گلدان
        Box(new Vector3(ctr.x, 0.1f, zf - 0.8f), new Vector3(3.2f, 0.2f, 1.4f), new Color(0.5f, 0.5f, 0.52f));
        foreach (float sx in new[] { -2.4f, 2.6f })
        {
            Cyl(new Vector3(ctr.x + sx, 0.4f, zf - 0.9f), new Vector3(0.6f, 0.4f, 0.6f), new Color(0.55f, 0.3f, 0.2f));
            Ball(new Vector3(ctr.x + sx, 1.0f, zf - 0.9f), Vector3.one * 0.8f, new Color(0.2f, 0.5f, 0.25f));
        }
        foreach (float sx in new[] { -1.4f, 1.4f })
            Ball(new Vector3(ctr.x + sx, 3.5f, zf - 0.3f), Vector3.one * 0.25f, new Color(1f, 0.9f, 0.6f));   // چراغ بالای در

        if (role == 4)
        {   // نئون بنفش دور تابلوی بلک‌مارکت
            Color neon = new Color(1f, 0.35f, 1f);
            Box(new Vector3(ctr.x, 5.2f, zf - 0.3f), new Vector3(7.4f, 0.08f, 0.08f), neon);
            Box(new Vector3(ctr.x, 3.9f, zf - 0.3f), new Vector3(7.4f, 0.08f, 0.08f), neon);
        }
        if (role == 3)
        {   // ستون‌های بانک و سردر
            foreach (float sx in new[] { -4.8f, -2.4f, 2.4f, 4.8f })
                Cyl(new Vector3(ctr.x + sx, 2.0f, zf - 1.6f), new Vector3(0.7f, 2.0f, 0.7f), new Color(0.9f, 0.88f, 0.82f));
            Box(new Vector3(ctr.x, 4.15f, zf - 1.6f), new Vector3(11.2f, 0.4f, 1.1f), new Color(0.9f, 0.88f, 0.82f));
        }
    }

    // پشت و کنار مغازه: در سرویس، کولر، لوله، چراغ دیواری
    void BuildSideDetails(Vector3 ctr, float half, System.Random rng, Color dark)
    {
        Color metal = new Color(0.55f, 0.57f, 0.6f);
        // در سرویس پشت
        Box(ctr + new Vector3(3.2f, 1.2f, half + 0.06f), new Vector3(1.2f, 2.4f, 0.12f), new Color(0.25f, 0.27f, 0.3f));
        Box(ctr + new Vector3(3.2f, 2.6f, half + 0.4f), new Vector3(1.8f, 0.1f, 0.7f), dark);
        Ball(ctr + new Vector3(3.2f, 2.45f, half + 0.2f), Vector3.one * 0.2f, new Color(1f, 0.9f, 0.6f));
        // کولر بیرونی
        for (int k = 0; k < 2; k++)
        {
            Vector3 p = ctr + new Vector3(half + 0.4f, 0.5f, -2.5f + k * 2.2f);
            Box(p, new Vector3(0.8f, 1.0f, 1.2f), metal);
            Cyl(p + new Vector3(0.41f, 0f, 0f), new Vector3(0.7f, 0.02f, 0.7f), new Color(0.15f, 0.15f, 0.17f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
        // لوله‌ی گاز / سیم‌کشی روی دیوار غربی
        Cyl(ctr + new Vector3(-half - 0.08f, 1.6f, -1f), new Vector3(0.08f, 1.6f, 0.08f), new Color(0.75f, 0.6f, 0.1f));
        Box(ctr + new Vector3(-half - 0.08f, 3.0f, 0f), new Vector3(0.1f, 0.25f, 0.25f), dark);
    }

    // ساختمان معمولی: در ساده، سایبان کوچک، بالکن، پنجره‌ی همکف هم داره
    void BuildApartmentFront(Vector3 ctr, float half, int floors, Color trim, Color dark, System.Random rng)
    {
        float zf = ctr.z - half - 0.04f;
        Box(new Vector3(ctr.x, 1.3f, zf), new Vector3(1.8f, 2.6f, 0.2f), new Color(0.25f, 0.17f, 0.12f));
        Box(new Vector3(ctr.x, 2.8f, zf - 0.5f), new Vector3(3f, 0.12f, 1.2f), dark);
        Box(new Vector3(ctr.x, 0.1f, zf - 0.7f), new Vector3(2.4f, 0.2f, 1.2f), new Color(0.5f, 0.5f, 0.52f));
        Box(new Vector3(ctr.x + 1.4f, 1.3f, zf - 0.08f), new Vector3(0.3f, 0.6f, 0.06f), new Color(0.7f, 0.7f, 0.72f));   // آیفون
        for (int k = 1; k < floors; k++)
            foreach (float sx in new[] { -2.8f, 2.8f })
            {
                Box(new Vector3(ctr.x + sx, 3.6f * k, zf - 0.5f), new Vector3(2.6f, 0.15f, 1.0f), trim);
                Box(new Vector3(ctr.x + sx, 3.6f * k + 0.5f, zf - 0.98f), new Vector3(2.6f, 0.9f, 0.05f), dark);
                if (rng.Next(0, 2) == 0) Ball(new Vector3(ctr.x + sx + 0.7f, 3.6f * k + 0.35f, zf - 0.6f), Vector3.one * 0.5f, new Color(0.2f, 0.5f, 0.25f));
            }
        // کولر روی نما
        for (int k = 1; k < floors; k++)
            if (rng.Next(0, 3) == 0) Box(new Vector3(ctr.x + 4.7f, 3.6f * k + 1.2f, zf - 0.3f), new Vector3(0.9f, 0.6f, 0.5f), new Color(0.8f, 0.8f, 0.82f));
    }

    // پلکان فرار اضطراری روی دیوار شرقی
    void BuildFireEscape(Vector3 ctr, float half, int floors, Color dark)
    {
        Color iron = new Color(0.12f, 0.12f, 0.14f);
        for (int k = 1; k < floors; k++)
        {
            Vector3 p = ctr + new Vector3(half + 0.55f, 3.6f * k, 1.5f);
            Box(p, new Vector3(1.1f, 0.1f, 2.6f), iron);
            Box(p + new Vector3(0.52f, 0.5f, 0f), new Vector3(0.05f, 1f, 2.6f), iron);
            var st = Box(p + new Vector3(0f, -1.8f, 1.0f), new Vector3(0.9f, 0.08f, 0.5f), iron);
            st.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var ladder = Box(p + new Vector3(0.2f, -1.8f, -0.9f), new Vector3(0.5f, 3.4f, 0.08f), iron);
            ladder.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        }
    }

    // ---------------------------------------------------------------- Characters
    void CreatePlayer(Vector3 pos, float yaw)
    {
        var p = new GameObject("Player");
        p.transform.SetParent(root, false);
        p.transform.position = pos;

        var look = new HumanLook
        {
            skin = SkinTones[1], shirt = new Color(0.12f, 0.12f, 0.15f), pants = new Color(0.16f, 0.22f, 0.36f),
            shoes = new Color(0.9f, 0.9f, 0.9f), hair = new Color(0.1f, 0.08f, 0.06f), mask = true, bag = true
        };
        BuildHuman(p.transform, look);

        var cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.9f, 0f); cc.stepOffset = 0.3f;
        var pc = p.AddComponent<PlayerController>();
        pc.Init(this, yaw);
        Player = pc;
    }

    void CreateCop(Vector2Int cell)
    {
        var r = new GameObject("Cop");
        r.transform.SetParent(root, false);
        r.transform.position = CellToWorld(cell);

        var look = new HumanLook
        {
            skin = SkinTones[Random.Range(0, SkinTones.Length)], shirt = new Color(0.1f, 0.2f, 0.5f), pants = new Color(0.07f, 0.09f, 0.18f),
            shoes = Color.black, hair = new Color(0.15f, 0.1f, 0.07f), cap = true, glasses = true, badge = true
        };
        var hu = BuildHuman(r.transform, look);
        hu.gun.localScale = new Vector3(0.06f, 0.09f, 0.3f);
        hu.ShowGun(0);

        var col = r.AddComponent<CapsuleCollider>();
        col.height = 1.85f; col.radius = 0.35f; col.center = new Vector3(0f, 0.92f, 0f);
        var rc = r.AddComponent<RobotCop>();
        rc.Init(this, Player, hu);
        cops.Add(rc);
    }
}
