using System.Collections.Generic;
using UnityEngine;

// مغازه‌های قابل ورود: پوسته‌ی توخالی (همیشه هست) + داخل مغازه (فقط وقتی بازیکن نزدیکه ساخته میشه)
public partial class GameManager
{
    // ---------------------------------------------------------------- Shell (همیشه ساخته میشه)
    // داخل ۱۰.۸×۱۰.۸ متر، سقف ۳.۶ متر، در جنوبی ۲.۴ متر عرض
    void BuildShopShell(ShopInfo sh, Material wallMat, float tile, int floors, Color dark, Color trim)
    {
        Vector3 c = sh.ctr;
        float h = 3.6f * floors;

        // دیوارهای شمال، شرق، غرب
        TexBox(c + new Vector3(0f, 1.8f, 5.6f), new Vector3(11.6f, 3.6f, 0.4f), wallMat, tile, true);
        TexBox(c + new Vector3(5.6f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 10.8f), wallMat, tile, true);
        TexBox(c + new Vector3(-5.6f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 10.8f), wallMat, tile, true);

        // دیوار جنوبی: چهار ستون، زیرپنجره‌ها، سردر. بین‌شون در (x از -1.2 تا 1.2) و ویترین‌ها (۱.۹ تا ۵.۱)
        foreach (float px in new[] { -5.45f, -1.55f, 1.55f, 5.45f })
            TexBox(c + new Vector3(px, 1.8f, -5.6f), new Vector3(0.7f, 3.6f, 0.4f), wallMat, tile, true);
        foreach (float sx in new[] { -3.5f, 3.5f })
        {
            TexBox(c + new Vector3(sx, 0.45f, -5.6f), new Vector3(3.2f, 0.9f, 0.4f), wallMat, tile, true);
            var g = Prim(PrimitiveType.Cube, "Glass", scn, c + new Vector3(sx, 1.9f, -5.6f), new Vector3(3.2f, 2.0f, 0.05f), Color.white, true);
            g.GetComponent<Renderer>().sharedMaterial = GlassMat();
            g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // قاب پنجره
            Box(c + new Vector3(sx, 2.92f, -5.85f), new Vector3(3.4f, 0.1f, 0.12f), dark);
            Box(c + new Vector3(sx, 0.92f, -5.85f), new Vector3(3.4f, 0.1f, 0.12f), dark);
            Box(c + new Vector3(sx, 1.92f, -5.85f), new Vector3(0.06f, 2.0f, 0.1f), dark);   // میله‌ی وسط
        }
        TexBox(c + new Vector3(0f, 3.25f, -5.6f), new Vector3(11.6f, 0.7f, 0.4f), wallMat, tile, true);

        // قاب در + چراغ OPEN
        Box(c + new Vector3(-1.27f, 1.45f, -5.82f), new Vector3(0.14f, 2.9f, 0.16f), trim);
        Box(c + new Vector3(1.27f, 1.45f, -5.82f), new Vector3(0.14f, 2.9f, 0.16f), trim);
        Box(c + new Vector3(0f, 2.95f, -5.82f), new Vector3(2.7f, 0.12f, 0.16f), trim);
        Box(c + new Vector3(0f, 2.6f, -5.86f), new Vector3(0.7f, 0.16f, 0.04f), new Color(0.2f, 1f, 0.4f));
        Box(c + new Vector3(-1.1f, 1.2f, -5.55f), new Vector3(0.05f, 2.3f, 0.9f), new Color(0.2f, 0.14f, 0.1f));   // لنگه‌ی در، باز

        // سقف + بالای مغازه
        TexBox(c + new Vector3(0f, 3.7f, 0f), new Vector3(11.6f, 0.2f, 11.6f), wallMat, tile, true);
        if (h > 3.8f) TexBox(c + new Vector3(0f, (3.8f + h) / 2f, 0f), new Vector3(11.6f, h - 3.8f, 11.6f), wallMat, tile, true);

        // پنل آلارم کنار در (روی ستون سمت راست) + چراغ وضعیت
        float px0 = c.x + 1.55f, zFace = c.z - 5.8f;
        Prim(PrimitiveType.Cube, "AlarmBox", root, new Vector3(px0, 1.45f, zFace - 0.07f), new Vector3(0.42f, 0.6f, 0.14f), new Color(0.12f, 0.12f, 0.14f));
        for (int r = 0; r < 3; r++)
            for (int k = 0; k < 3; k++)
                Prim(PrimitiveType.Cube, "Key", root, new Vector3(px0 - 0.1f + k * 0.1f, 1.38f - r * 0.09f, zFace - 0.15f), new Vector3(0.06f, 0.05f, 0.02f), new Color(0.7f, 0.7f, 0.72f));
        var led = Prim(PrimitiveType.Cube, "AlarmLED", root, new Vector3(px0, 1.67f, zFace - 0.15f), new Vector3(0.24f, 0.08f, 0.03f), new Color(1f, 0.15f, 0.1f));
        sh.led = led.GetComponent<Renderer>();

        // آژیر بالای در (سمت چپ تابلو)
        var sir = Prim(PrimitiveType.Cube, "Siren", root, new Vector3(c.x + 4.95f, 4.4f, zFace - 0.12f), new Vector3(0.35f, 0.35f, 0.25f), new Color(0.25f, 0.05f, 0.05f));
        sh.siren = sir.GetComponent<Renderer>();

        // نقطه‌ی تعامل پنل آلارم
        sh.panel = new Spot
        {
            type = SpotType.AlarmPanel, pos = new Vector3(px0, 0f, zFace - 1.0f), label = "Alarm Panel", shopType = sh.role, shop = sh,
            duration = NativeCore.HackTimeX100(sh.role) / 100f, color = new Color(1f, 0.25f, 0.2f)
        };
        spots.Add(sh.panel);

        // ظرف‌های لوت (فقط داده؛ ظاهرشون موقع ساخت داخل مغازه میاد)
        foreach (var f in Layouts.Shop[sh.role])
        {
            if (f.kind < 0) continue;
            var s = new Spot
            {
                type = SpotType.Container, kind = f.kind, label = f.name, shopType = sh.role, shop = sh,
                pos = c + new Vector3(f.x, 0f, f.z), half = new Vector2(f.len * 0.5f, f.dep * 0.5f), yaw = f.yaw,
                duration = NativeCore.ContainerTimeX100(f.kind) / 100f, color = ShopColors[sh.role]
            };
            sh.containers.Add(s);
            spots.Add(s);
        }
    }

    // ---------------------------------------------------------------- Interior (lazy)
    static readonly Color[][] GoodsPalette =
    {
        new[] { new Color(0.85f, 0.2f, 0.15f), new Color(0.95f, 0.8f, 0.2f), new Color(0.2f, 0.6f, 0.25f), new Color(0.2f, 0.4f, 0.8f), new Color(0.92f, 0.92f, 0.9f), new Color(0.9f, 0.5f, 0.15f) },
        new[] { new Color(0.1f, 0.1f, 0.12f), new Color(0.85f, 0.85f, 0.88f), new Color(0.25f, 0.28f, 0.35f), new Color(0.2f, 0.5f, 0.9f), new Color(0.6f, 0.6f, 0.65f) },
        new[] { new Color(1f, 0.82f, 0.25f), new Color(0.85f, 0.85f, 0.9f), new Color(0.85f, 0.15f, 0.25f), new Color(0.2f, 0.75f, 0.8f), new Color(0.95f, 0.6f, 0.7f) },
        new[] { new Color(1f, 0.82f, 0.25f), new Color(0.25f, 0.65f, 0.3f), new Color(0.85f, 0.85f, 0.9f), new Color(0.3f, 0.3f, 0.35f) },
    };

    Vector3 FP(FDef f, float lx, float ly, float lz)
    {
        Vector3 p = Quaternion.Euler(0f, f.yaw, 0f) * new Vector3(lx, 0f, lz);
        return new Vector3(f.x + p.x, ly, f.z + p.z);
    }

    // یه جعبه‌ی رنگی تو دستگاه مختصات مبلمان (x = طول، y = ارتفاع، z = عمق / جلو)
    GameObject FB(Transform parent, FDef f, float lx, float ly, float lz, float sx, float sy, float sz, Color col)
    {
        var g = Prim(PrimitiveType.Cube, "F", parent, FP(f, lx, ly, lz), new Vector3(sx, sy, sz), col, false);
        g.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
        return g;
    }

    // جعبه‌ی بافت‌دار (UV بر اساس اندازه‌ی واقعی؛ tile = اندازه‌ی یه تکرار بافت به متر)
    GameObject FBT(Transform parent, FDef f, float lx, float ly, float lz, float sx, float sy, float sz, Material mat, float tile)
    {
        var g = TexBox(FP(f, lx, ly, lz), new Vector3(sx, sy, sz), mat, tile, false, parent);
        g.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
        return g;
    }

    GameObject FGlass(Transform parent, FDef f, float lx, float ly, float lz, float sx, float sy, float sz)
    {
        var g = FB(parent, f, lx, ly, lz, sx, sy, sz, Color.white);
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = GlassMat();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    void FCol(Transform parent, FDef f, float height)
    {
        var g = new GameObject("Col");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = new Vector3(f.x, height * 0.5f, f.z);
        g.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
        g.AddComponent<BoxCollider>().size = new Vector3(f.len, height, f.dep);
    }

    // ---------------------------------------------------------------- Materials
    readonly Dictionary<int, Material> shinyMats = new Dictionary<int, Material>();

    Material ShinyMat(Color c)
    {
        int key = ((Color32)c).GetHashCode();
        Material m;
        if (shinyMats.TryGetValue(key, out m) && m != null) return m;
        m = new Material(PipeShader());
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.9f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.85f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f);
        shinyMats[key] = m;
        return m;
    }

    // بسته‌بندی کالا: برچسب، لوگو، بارکد (رنگ بدنه از tint میاد)
    Material GoodsMat(Color c) { return TexMat("package", c, 0.35f); }

    static void SetMat(GameObject g, Material m) { g.GetComponent<Renderer>().sharedMaterial = m; }

    void GoodsRow(Transform g, FDef f, float y, float z, float length, int role, System.Random rng)
    {
        float step = role == 1 ? 0.5f : (role == 2 ? 0.28f : 0.3f);
        int n = Mathf.Max(1, (int)(length / step));
        var pal = GoodsPalette[role];
        for (int i = 0; i < n; i++)
        {
            float x = -length * 0.5f + step * (i + 0.5f);
            Vector3 size;
            Material mat;
            Color col = pal[rng.Next(0, pal.Length)];
            switch (role)
            {
                case 1:
                    if (rng.Next(0, 3) == 0) { size = new Vector3(0.36f, 0.04f, 0.26f); mat = TexMat("screen", Color.white, 0.7f); }   // لپ‌تاپ
                    else { size = new Vector3(0.38f, 0.1f + (float)rng.NextDouble() * 0.12f, 0.28f); mat = GoodsMat(col); }
                    break;
                case 2: size = new Vector3(0.14f, 0.1f, 0.14f); mat = ShinyMat(col); break;
                default: size = new Vector3(0.2f + (float)rng.NextDouble() * 0.08f, 0.2f + (float)rng.NextDouble() * 0.18f, 0.2f); mat = GoodsMat(col); break;
            }
            var go = Prim(PrimitiveType.Cube, "G", g, FP(f, x, y + size.y * 0.5f, z), size, Color.white, false);
            SetMat(go, mat);
            go.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
        }
    }

    // ---------------------------------------------------------------- Interior
    void BuildInterior(ShopInfo sh)
    {
        var io = new GameObject("Interior:" + sh.name).transform;
        io.SetParent(root, false);
        io.position = sh.ctr;
        sh.interior = io;
        var furn = new GameObject("Furniture").transform;
        furn.SetParent(io, false);
        int role = sh.role;
        var rng = new System.Random(Mathf.RoundToInt(sh.ctr.x * 31f + sh.ctr.z * 17f) + role);
        Color accent = ShopColors[role];

        // سطوح هر نوع مغازه: کف، دیوار، لامبری (کاشی/چوب پایین دیوار)
        Material floorMat, wallIn, wain;
        float floorTile;
        switch (role)
        {
            case 0:   // خواروبار: کاشی شطرنجی، دیوار کِرم، کاشی سفید
                floorMat = TexMat("floor_checker", Color.white, 0.45f); floorTile = 2.4f;
                wallIn = TexMat("wall_soft", new Color(1f, 0.96f, 0.8f), 0.05f); wain = TexMat("subway", Color.white, 0.6f); break;
            case 1:   // الکترونیک: سرامیک تیره، دیوار آبی روشن، پنل فلزی
                floorMat = TexMat("floor_dark", Color.white, 0.4f); floorTile = 2.4f;
                wallIn = TexMat("wall_soft", new Color(0.8f, 0.86f, 0.98f), 0.05f); wain = TexMat("metal_brushed", new Color(0.55f, 0.58f, 0.66f), 0.5f, 0.5f); break;
            case 2:   // جواهری: پارکت، کاغذدیواری، پنل چوبی
                floorMat = TexMat("floor_wood", Color.white, 0.4f); floorTile = 2.0f;
                wallIn = TexMat("wallpaper", Color.white, 0.05f); wain = TexMat("wood_grain", new Color(0.85f, 0.72f, 0.66f), 0.35f); break;
            default:  // بانک: مرمر
                floorMat = TexMat("floor_marble", Color.white, 0.65f); floorTile = 3.0f;
                wallIn = TexMat("wall_soft", new Color(0.96f, 0.94f, 0.86f), 0.05f); wain = TexMat("floor_marble", new Color(0.88f, 0.86f, 0.8f), 0.55f); break;
        }
        TexBox(new Vector3(0f, 0.075f, 0f), new Vector3(10.8f, 0.03f, 10.8f), floorMat, floorTile, false, furn);
        TexBox(new Vector3(0f, 1.8f, 5.38f), new Vector3(10.76f, 3.6f, 0.04f), wallIn, 2.4f, false, furn);
        TexBox(new Vector3(5.38f, 1.8f, 0f), new Vector3(0.04f, 3.6f, 10.76f), wallIn, 2.4f, false, furn);
        TexBox(new Vector3(-5.38f, 1.8f, 0f), new Vector3(0.04f, 3.6f, 10.76f), wallIn, 2.4f, false, furn);
        TexBox(new Vector3(0f, 3.58f, 0f), new Vector3(10.8f, 0.04f, 10.8f), TexMat("ceiling", Color.white, 0.1f), 2.4f, false, furn);

        // لامبری روی سه دیوار + نوار رنگی + لبه
        TexBox(new Vector3(0f, 0.68f, 5.335f), new Vector3(10.7f, 1.1f, 0.05f), wain, 1.6f, false, furn);
        TexBox(new Vector3(5.335f, 0.68f, 0f), new Vector3(0.05f, 1.1f, 10.7f), wain, 1.6f, false, furn);
        TexBox(new Vector3(-5.335f, 0.68f, 0f), new Vector3(0.05f, 1.1f, 10.7f), wain, 1.6f, false, furn);
        Color cap = accent * 0.7f;
        Prim(PrimitiveType.Cube, "WainCap", furn, new Vector3(0f, 1.25f, 5.33f), new Vector3(10.7f, 0.05f, 0.08f), cap);
        Prim(PrimitiveType.Cube, "WainCap", furn, new Vector3(5.33f, 1.25f, 0f), new Vector3(0.08f, 0.05f, 10.7f), cap);
        Prim(PrimitiveType.Cube, "WainCap", furn, new Vector3(-5.33f, 1.25f, 0f), new Vector3(0.08f, 0.05f, 10.7f), cap);
        Prim(PrimitiveType.Cube, "Band", furn, new Vector3(0f, 2.75f, 5.35f), new Vector3(10.76f, 0.12f, 0.02f), accent);
        Prim(PrimitiveType.Cube, "Band", furn, new Vector3(5.35f, 2.75f, 0f), new Vector3(0.02f, 0.12f, 10.76f), accent);
        Prim(PrimitiveType.Cube, "Band", furn, new Vector3(-5.35f, 2.75f, 0f), new Vector3(0.02f, 0.12f, 10.76f), accent);
        foreach (float lx in new[] { -2.6f, 2.6f })
            foreach (float lz in new[] { -2.6f, 2.6f })
                Prim(PrimitiveType.Cube, "CeilingLamp", furn, new Vector3(lx, 3.53f, lz), new Vector3(1.8f, 0.05f, 0.5f), new Color(1f, 0.97f, 0.88f));
        // ساعت دیواری و پوستر
        Prim(PrimitiveType.Cylinder, "Clock", furn, new Vector3(0f, 3.1f, 5.33f), new Vector3(0.5f, 0.02f, 0.5f), new Color(0.95f, 0.95f, 0.95f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Cube, "Poster", furn, new Vector3(-5.33f, 1.9f, -2.5f), new Vector3(0.02f, 1.1f, 0.8f), accent * 0.9f);

        // مبلمان و ظرف‌ها
        int ci = 0;
        foreach (var f in Layouts.Shop[role])
        {
            Spot sp = f.kind >= 0 ? sh.containers[ci++] : null;
            Transform goods = null;
            if (sp != null && !sp.done)
            {
                var gg = new GameObject("Goods:" + f.name);
                gg.transform.SetParent(io, false);
                goods = gg.transform;
                sp.goods = goods;
            }
            BuildFurniture(sh, furn, goods, f, role, accent, rng);
        }

        foreach (var cp in Layouts.Cashiers[role]) MakeCashier(sh, io, cp, accent);

        var si = Prim(PrimitiveType.Cube, "InnerSiren", io, new Vector3(0f, 3.45f, -3.2f), new Vector3(0.3f, 0.15f, 0.3f), new Color(0.25f, 0.05f, 0.05f));
        sh.innerSiren = si.GetComponent<Renderer>();

        try { StaticBatchingUtility.Combine(furn.gameObject); }
        catch (System.Exception e) { Debug.LogWarning("Interior batching skipped: " + e.Message); }
    }

    void DestroyInterior(ShopInfo sh)
    {
        if (sh.interior != null) Destroy(sh.interior.gameObject);
        sh.interior = null;
        sh.cashiers.Clear();
        sh.innerSiren = null;
        foreach (var c in sh.containers) c.goods = null;
    }

    void BuildFurniture(ShopInfo sh, Transform furn, Transform goods, FDef f, int role, Color accent, System.Random rng)
    {
        Material wood = TexMat("wood_grain", Color.white, 0.3f);
        Material woodLt = TexMat("wood_grain", new Color(1.3f, 1.18f, 1.05f), 0.3f);
        Material steel = TexMat("metal_brushed", Color.white, 0.55f, 0.6f);
        Material steelDk = TexMat("metal_brushed", new Color(0.4f, 0.42f, 0.46f), 0.55f, 0.6f);
        Material laminate = TexMat("wall_soft", Color.white, 0.45f);
        Material marble = TexMat("floor_marble", Color.white, 0.75f);
        Material velvet = TexMat("velvet", Color.white, 0.1f);
        Color dark = new Color(0.12f, 0.12f, 0.14f), steelCol = new Color(0.6f, 0.62f, 0.66f);
        float L = f.len, D = f.dep;

        switch (f.style)
        {
            case Layouts.S_GONDOLA:
            {
                FB(furn, f, 0f, 0.1f, 0f, L, 0.2f, D, dark);
                FBT(furn, f, 0f, 0.85f, 0f, L - 0.02f, 1.7f, 0.05f, steel, 1.5f);
                FBT(furn, f, -L * 0.5f + 0.03f, 0.85f, 0f, 0.06f, 1.7f, D, steel, 1.5f);
                FBT(furn, f, L * 0.5f - 0.03f, 0.85f, 0f, 0.06f, 1.7f, D, steel, 1.5f);
                foreach (float y in new[] { 0.3f, 0.75f, 1.2f, 1.62f }) FBT(furn, f, 0f, y, 0f, L - 0.1f, 0.04f, D - 0.04f, laminate, 2f);
                FB(furn, f, 0f, 1.75f, 0f, L, 0.12f, D, accent);
                FCol(furn, f, 1.8f);
                if (goods != null)
                    foreach (float y in new[] { 0.32f, 0.77f, 1.22f })
                    {
                        GoodsRow(goods, f, y, D * 0.5f - 0.2f, L - 0.25f, role, rng);
                        GoodsRow(goods, f, y, -D * 0.5f + 0.2f, L - 0.25f, role, rng);
                    }
                break;
            }
            case Layouts.S_WALLSHELF:
            {
                FBT(furn, f, 0f, 1.1f, -D * 0.5f + 0.025f, L, 2.2f, 0.05f, steel, 1.5f);
                FBT(furn, f, -L * 0.5f + 0.03f, 1.1f, 0f, 0.06f, 2.2f, D, steel, 1.5f);
                FBT(furn, f, L * 0.5f - 0.03f, 1.1f, 0f, 0.06f, 2.2f, D, steel, 1.5f);
                FB(furn, f, 0f, 0.1f, 0f, L, 0.2f, D, dark);
                foreach (float y in new[] { 0.4f, 0.9f, 1.4f, 1.9f }) FBT(furn, f, 0f, y, 0f, L - 0.1f, 0.04f, D - 0.04f, laminate, 2f);
                FCol(furn, f, 2.2f);
                if (goods != null)
                    foreach (float y in new[] { 0.42f, 0.92f, 1.42f })
                        GoodsRow(goods, f, y, 0.02f, L - 0.25f, role, rng);
                break;
            }
            case Layouts.S_FRIDGE:
            {
                FBT(furn, f, 0f, 1f, -0.02f, L, 2f, D - 0.04f, TexMat("metal_brushed", new Color(0.82f, 0.9f, 0.96f), 0.6f, 0.4f), 1.5f);
                FB(furn, f, 0f, 2.03f, 0f, L, 0.08f, D, accent);
                FGlass(furn, f, 0f, 1f, D * 0.5f + 0.01f, L - 0.1f, 1.8f, 0.03f);
                for (int k = -1; k <= 1; k++) FB(furn, f, k * (L / 3f), 1f, D * 0.5f + 0.03f, 0.04f, 1.8f, 0.03f, steelCol);
                FCol(furn, f, 2.05f);
                if (goods != null)
                    foreach (float y in new[] { 0.35f, 0.9f, 1.45f })
                    {
                        int n = Mathf.Max(3, (int)(L / 0.22f));
                        for (int i = 0; i < n; i++)
                        {
                            var b = Prim(PrimitiveType.Cylinder, "Bottle", goods, FP(f, -L * 0.5f + 0.15f + i * ((L - 0.3f) / (n - 1)), y + 0.15f, D * 0.5f - 0.2f), new Vector3(0.09f, 0.15f, 0.09f), Color.white, false);
                            SetMat(b, GoodsMat(GoodsPalette[0][rng.Next(0, 6)]));
                        }
                    }
                break;
            }
            case Layouts.S_CABINET:
            {
                FBT(furn, f, 0f, 0.5f, 0f, L, 1.0f, D, wood, 1.4f);
                FBT(furn, f, 0f, 1.02f, 0f, L + 0.04f, 0.04f, D + 0.04f, woodLt, 1.4f);
                int doors = Mathf.Max(1, Mathf.RoundToInt(L / 0.9f));
                for (int k = 1; k < doors; k++) FB(furn, f, -L * 0.5f + k * (L / doors), 0.5f, D * 0.5f + 0.005f, 0.02f, 0.9f, 0.02f, dark);
                for (int k = 0; k < doors; k++) FB(furn, f, -L * 0.5f + (k + 0.5f) * (L / doors), 0.62f, D * 0.5f + 0.02f, 0.14f, 0.03f, 0.03f, steelCol);
                FCol(furn, f, 1.05f);
                if (goods != null)
                {
                    var cr = Prim(PrimitiveType.Cube, "Crate", goods, FP(f, L * 0.2f, 1.2f, 0f), new Vector3(0.5f, 0.28f, 0.4f), Color.white, false);
                    SetMat(cr, TexMat("crate", Color.white, 0.1f));
                    cr.transform.localRotation = Quaternion.Euler(0f, f.yaw + 10f, 0f);
                }
                break;
            }
            case Layouts.S_COUNTER:
            {
                FBT(furn, f, 0f, 0.5f, 0f, L, 1.0f, D, wood, 1.4f);
                FBT(furn, f, 0f, 1.03f, 0f, L + 0.08f, 0.06f, D + 0.1f, marble, 2.5f);
                FB(furn, f, 0f, 0.5f, D * 0.5f + 0.01f, L - 0.1f, 0.2f, 0.02f, accent);
                FB(furn, f, -L * 0.25f, 1.22f, -0.05f, 0.45f, 0.3f, 0.4f, dark);
                FB(furn, f, -L * 0.25f, 1.4f, 0.0f, 0.34f, 0.2f, 0.03f, new Color(0.2f, 0.7f, 0.5f));
                FB(furn, f, -L * 0.25f + 0.55f, 1.12f, 0.12f, 0.12f, 0.1f, 0.18f, steelCol);
                if (role == 3) FGlass(furn, f, 0f, 1.45f, 0f, L, 0.8f, 0.03f);
                FCol(furn, f, 1.05f);
                if (goods != null)
                {
                    var cm = TexMat("cash", Color.white, 0.2f);
                    var c1 = Prim(PrimitiveType.Cube, "Cash", goods, FP(f, -L * 0.25f - 0.5f, 1.09f, 0.1f), new Vector3(0.3f, 0.06f, 0.2f), Color.white, false);
                    SetMat(c1, cm); c1.transform.localRotation = Quaternion.Euler(0f, f.yaw, 0f);
                    var c2 = Prim(PrimitiveType.Cube, "Cash", goods, FP(f, -L * 0.25f - 0.5f, 1.15f, 0.1f), new Vector3(0.28f, 0.06f, 0.18f), Color.white, false);
                    SetMat(c2, cm); c2.transform.localRotation = Quaternion.Euler(0f, f.yaw + 12f, 0f);
                }
                break;
            }
            case Layouts.S_DISPLAY:
            {
                FB(furn, f, 0f, 0.45f, 0f, L, 0.9f, D, dark);
                FB(furn, f, 0f, 0.45f, D * 0.5f + 0.01f, L - 0.1f, 0.12f, 0.02f, accent);
                FBT(furn, f, 0f, 0.91f, 0f, L - 0.1f, 0.02f, D - 0.1f, velvet, 1f);
                FGlass(furn, f, 0f, 1.06f, 0f, L - 0.02f, 0.3f, D - 0.02f);
                FBT(furn, f, 0f, 1.22f, 0f, L, 0.03f, D, steel, 1.2f);
                FCol(furn, f, 1.22f);
                if (goods != null) GoodsRow(goods, f, 0.93f, 0f, L - 0.3f, 2, rng);
                break;
            }
            case Layouts.S_ISLAND:
            {
                if (role == 2)
                {
                    FB(furn, f, 0f, 0.45f, 0f, L, 0.9f, D, dark);
                    FBT(furn, f, 0f, 0.91f, 0f, L - 0.1f, 0.02f, D - 0.1f, velvet, 1f);
                    FGlass(furn, f, 0f, 1.06f, 0f, L - 0.02f, 0.3f, D - 0.02f);
                    FBT(furn, f, 0f, 1.22f, 0f, L, 0.03f, D, steel, 1.2f);
                    FCol(furn, f, 1.22f);
                    if (goods != null)
                    {
                        GoodsRow(goods, f, 0.93f, D * 0.25f, L - 0.3f, 2, rng);
                        GoodsRow(goods, f, 0.93f, -D * 0.25f, L - 0.3f, 2, rng);
                    }
                }
                else
                {
                    FBT(furn, f, 0f, 0.42f, 0f, L - 0.1f, 0.84f, D - 0.1f, steelDk, 1.5f);
                    FBT(furn, f, 0f, 0.87f, 0f, L, 0.06f, D, laminate, 2f);
                    FB(furn, f, 0f, 0.03f, 0f, L, 0.06f, D, dark);
                    FCol(furn, f, 0.9f);
                    if (goods != null)
                    {
                        GoodsRow(goods, f, 0.9f, D * 0.25f, L - 0.3f, 1, rng);
                        GoodsRow(goods, f, 0.9f, -D * 0.25f, L - 0.3f, 1, rng);
                    }
                }
                break;
            }
            case Layouts.S_SAFE:
            {
                FBT(furn, f, 0f, 0.65f, 0f, L, 1.3f, D, steelDk, 1.2f);
                FB(furn, f, 0f, 0.65f, D * 0.5f + 0.005f, L - 0.2f, 1.1f, 0.02f, new Color(0.04f, 0.04f, 0.05f));
                FCol(furn, f, 1.3f);
                if (goods != null)
                {
                    FBT(goods, f, 0f, 0.65f, D * 0.5f + 0.06f, L - 0.1f, 1.2f, 0.1f, steel, 1f);
                    var wheel = Prim(PrimitiveType.Cylinder, "Wheel", goods, FP(f, 0f, 0.7f, D * 0.5f + 0.14f), new Vector3(0.3f, 0.03f, 0.3f), Color.white, false);
                    SetMat(wheel, ShinyMat(new Color(0.9f, 0.75f, 0.3f)));
                    wheel.transform.localRotation = Quaternion.Euler(90f, f.yaw, 0f);
                    FB(goods, f, L * 0.3f, 1.1f, D * 0.5f + 0.12f, 0.1f, 0.06f, 0.03f, new Color(1f, 0.2f, 0.15f));
                }
                break;
            }
            case Layouts.S_DEPOSIT:
            {
                FBT(furn, f, 0f, 1.1f, 0f, L, 2.2f, D, steelDk, 1.5f);
                int cols = Mathf.Max(2, (int)(L / 0.42f));
                for (int r = 0; r < 6; r++)
                    for (int k = 0; k < cols; k++)
                        FBT(furn, f, -L * 0.5f + (k + 0.5f) * (L / cols), 0.28f + r * 0.34f, D * 0.5f + 0.01f, (L / cols) - 0.04f, 0.28f, 0.02f, (r + k) % 2 == 0 ? steel : steelDk, 0.6f);
                FCol(furn, f, 2.2f);
                if (goods != null)
                    for (int k = 0; k < 4; k++)
                    {
                        float lx = -L * 0.5f + (0.5f + rng.Next(0, cols)) * (L / cols);
                        var gb = FB(goods, f, lx, 0.28f + rng.Next(0, 6) * 0.34f, D * 0.5f + 0.12f, (L / cols) - 0.06f, 0.24f, 0.24f, Color.white);
                        SetMat(gb, ShinyMat(new Color(1f, 0.82f, 0.25f)));
                    }
                break;
            }
            case Layouts.S_BENCH:
            {
                FBT(furn, f, 0f, 0.45f, 0f, L, 0.08f, D, wood, 1.2f);
                FBT(furn, f, 0f, 0.8f, -D * 0.5f + 0.04f, L, 0.4f, 0.06f, wood, 1.2f);
                FB(furn, f, -L * 0.5f + 0.1f, 0.22f, 0f, 0.07f, 0.44f, D - 0.05f, dark);
                FB(furn, f, L * 0.5f - 0.1f, 0.22f, 0f, 0.07f, 0.44f, D - 0.05f, dark);
                FCol(furn, f, 0.5f);
                break;
            }
            case Layouts.S_PARTITION:
            {
                FBT(furn, f, 0f, 1.7f, 0f, L, 3.4f, D, marble, 2.4f);
                FBT(furn, f, 0f, 0.15f, 0f, L + 0.04f, 0.3f, D + 0.04f, steelDk, 1.2f);
                FCol(furn, f, 3.4f);
                break;
            }
            case Layouts.S_DESK:
            {
                FBT(furn, f, 0f, 0.4f, 0f, L, 0.8f, D, wood, 1.4f);
                FBT(furn, f, 0f, 0.83f, 0f, L + 0.06f, 0.05f, D + 0.06f, woodLt, 1.4f);
                FB(furn, f, 0f, 1.08f, -D * 0.25f, 0.5f, 0.35f, 0.04f, dark);
                var scr = FB(furn, f, 0f, 1.08f, -D * 0.25f + 0.025f, 0.44f, 0.29f, 0.01f, Color.white);
                SetMat(scr, TexMat("screen", Color.white, 0.7f));
                FB(furn, f, 0f, 0.5f, -D * 0.5f - 0.6f, 0.55f, 0.08f, 0.55f, dark);
                FB(furn, f, 0f, 0.85f, -D * 0.5f - 0.85f, 0.55f, 0.7f, 0.08f, dark);
                FCol(furn, f, 0.9f);
                if (goods != null) FB(goods, f, L * 0.25f, 0.88f, D * 0.1f, 0.35f, 0.06f, 0.25f, new Color(0.95f, 0.95f, 0.9f));
                break;
            }
            case Layouts.S_VAULTDOOR:
            {
                float gap = L;
                FBT(furn, f, -gap * 0.5f - 0.1f, 1.3f, 0f, 0.22f, 2.6f, D + 0.4f, steelDk, 1.2f);
                FBT(furn, f, gap * 0.5f + 0.1f, 1.3f, 0f, 0.22f, 2.6f, D + 0.4f, steelDk, 1.2f);
                FBT(furn, f, 0f, 2.7f, 0f, gap + 0.44f, 0.3f, D + 0.4f, steelDk, 1.2f);
                for (int k = 0; k < 6; k++) FB(furn, f, -gap * 0.5f - 0.1f, 0.2f + k * 0.45f, (D + 0.4f) * 0.5f + 0.005f, 0.22f, 0.2f, 0.01f, k % 2 == 0 ? new Color(0.95f, 0.8f, 0.1f) : dark);
                break;
            }
        }
    }

    void MakeCashier(ShopInfo sh, Transform io, Vector3 cp, Color accent)
    {
        var go = new GameObject("Cashier");
        go.transform.SetParent(io, false);
        go.transform.localPosition = new Vector3(cp.x, 0.08f, cp.y);
        go.transform.localRotation = Quaternion.Euler(0f, cp.z, 0f);
        var look = new HumanLook
        {
            skin = SkinTones[Random.Range(0, SkinTones.Length)], shirt = Color.Lerp(accent, Color.white, 0.2f), pants = new Color(0.15f, 0.15f, 0.2f),
            shoes = Color.black, hair = new Color(0.25f, 0.15f, 0.08f), glasses = Random.value < 0.4f
        };
        var hu = BuildHuman(go.transform, look, false);
        if (hu.body != null)
            Prim(PrimitiveType.Cube, "Apron", hu.body, new Vector3(0f, 0.27f, 0.125f), new Vector3(0.36f, 0.4f, 0.03f), new Color(0.95f, 0.95f, 0.95f), false);
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.8f; col.radius = 0.32f; col.center = new Vector3(0f, 0.9f, 0f);
        sh.cashiers.Add(hu);
    }

    // ---------------------------------------------------------------- Per-frame (فقط تو لند)
    void UpdateShops()
    {
        if (Player == null) return;
        Vector3 pp = Player.transform.position;
        bool builtOne = false;
        ShopInfo inside = null;

        foreach (var sh in shops)
        {
            float dx = pp.x - sh.ctr.x, dz = pp.z - sh.ctr.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (sh.interior == null) { if (d < 30f && !builtOne) { BuildInterior(sh); builtOne = true; } }
            else if (d > 58f) DestroyInterior(sh);

            if (Mathf.Abs(dx) < 5.3f && Mathf.Abs(dz) < 5.3f)
            {
                inside = sh;
                if (!sh.entered) OnShopEntered(sh);
            }
        }

        // چراغ داخل مغازه
        if (interiorLight != null)
        {
            interiorLight.enabled = inside != null;
            if (inside != null) interiorLight.transform.position = inside.ctr + new Vector3(0f, 3.0f, -0.5f);
        }

        // صندوقدارها دست‌هاشونو میبرن بالا و به بازیکن نگاه میکنن
        foreach (var sh in shops)
        {
            if (sh.interior == null) continue;
            foreach (var hu in sh.cashiers)
            {
                if (hu == null) continue;
                bool scared = sh == inside;
                hu.handsUp = scared;
                if (scared)
                {
                    Vector3 to = Flat(pp - hu.transform.position);
                    if (to.sqrMagnitude > 0.01f)
                    {
                        var root3 = hu.transform.parent;
                        root3.rotation = Quaternion.Slerp(root3.rotation, Quaternion.LookRotation(to.normalized), Time.deltaTime * 6f);
                    }
                }
            }
        }
    }

    // چراغ وضعیت آلارم، آژیر، و صدای آژیر
    void UpdateShopVisuals()
    {
        FitSigns();
        float t = Time.time;
        bool ringNear = false;
        Color red = new Color(1f, 0.1f, 0.08f), blue = new Color(0.1f, 0.3f, 1f), off = new Color(0.25f, 0.05f, 0.05f);
        foreach (var sh in shops)
        {
            bool ringing = sh.alarmTripped && t < sh.alarmUntil;
            if (sh.led != null)
            {
                Color c;
                if (!sh.alarmArmed) c = new Color(0.2f, 1f, 0.3f);
                else if (sh.alarmTripped) c = Mathf.Sin(t * 14f) > 0f ? red : new Color(0.3f, 0f, 0f);
                else c = Mathf.Sin(t * 3f) > 0.6f ? new Color(1f, 0.15f, 0.1f) : new Color(0.3f, 0.04f, 0.04f);
                sh.led.material.color = c;
            }
            Color sc = ringing ? (Mathf.Sin(t * 10f) > 0f ? red : blue) : off;
            if (sh.siren != null) sh.siren.material.color = sc;
            if (sh.innerSiren != null) sh.innerSiren.material.color = sc;
            if (ringing && Player != null && Flat(sh.ctr - Player.transform.position).magnitude < 60f) ringNear = true;
        }
        if (ringNear && t > alarmSndT)
        {
            alarmSndT = t + 3.5f;
            if (GameAudio.I != null) GameAudio.I.Alarm();
        }
    }
}
