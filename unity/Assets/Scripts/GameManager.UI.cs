using System.Collections.Generic;
using UnityEngine;

// رابط کاربری (IMGUI) - بدون نیاز به Canvas یا TextMeshPro
public partial class GameManager
{
    GUIStyle sLabel, sBig, sCenter, sRight, sSmall, sBtn, sSmallC, sMid, sTiny, sWrap;

    void InitStyles()
    {
        if (sLabel != null) return;
        sLabel = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        sLabel.normal.textColor = Color.white;
        sBig = new GUIStyle(sLabel) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sMid = new GUIStyle(sLabel) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sCenter = new GUIStyle(sLabel) { alignment = TextAnchor.MiddleCenter };
        sRight = new GUIStyle(sLabel) { alignment = TextAnchor.MiddleRight };
        sSmall = new GUIStyle(sLabel) { fontSize = 15 };
        sSmallC = new GUIStyle(sSmall) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
        sSmall.wordWrap = false;
        sTiny = new GUIStyle(sSmall) { fontSize = 13 };
        sWrap = new GUIStyle(sSmall) { wordWrap = true };
        sBtn = new GUIStyle(GUI.skin.button) { fontSize = 20 };
    }

    static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void Text(Rect r, string t, GUIStyle st, Color c)
    {
        var old = st.normal.textColor;
        st.normal.textColor = c;
        GUI.Label(r, t, st);
        st.normal.textColor = old;
    }

    static void Line(Vector2 a, Vector2 b, float th, Color c)
    {
        Matrix4x4 old = GUI.matrix;
        float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(ang, a);
        Fill(new Rect(a.x, a.y - th * 0.5f, (b - a).magnitude, th), c);
        GUI.matrix = old;
    }

    void OnGUI()
    {
        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float W = Screen.width / s, H = 720f;
        InitStyles();

        if (State == GameState.Menu || !NativeCore.Available) { DrawMenu(W, H); return; }
        if (State == GameState.Loading) { DrawLoading(W, H); return; }

        DrawWorldLabels(s);
        DrawHud(W, H);
        if (State == GameState.Dead) DrawDead(W, H);
        if (CurrentPanel != UiPanel.None) DrawPanel(W, H);
        if (paused) DrawPause(W, H);
    }

    // ---------------------------------------------------------------- Menu & servers
    void DrawMenu(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.72f));
        float bw = 700, x = (W - bw) / 2, y = 50;
        GUI.Label(new Rect(x, y, bw, 60), "ROBOT HEIST", sBig); y += 64;
        GUI.Label(new Rect(x, y, bw, 30), "Rob shops. Sell the loot. Escape the robot cops.", sCenter); y += 46;

        if (!NativeCore.Available || fatalMsg.Length > 0)
        {
            Text(new Rect(x, y, bw, 120), fatalMsg.Length > 0 ? fatalMsg : "C++ core not loaded.", sCenter, new Color(1f, 0.4f, 0.4f));
            return;
        }

        if (menuPage == 0)
        {
            string[] lines =
            {
                "WASD  move          Mouse  look / aim",
                "Left mouse  shoot     R  reload     Shift  sprint",
                "1-5  switch weapon     E  interact     T  tech tree (base)     M  big map",
                "K  music on/off     Esc  pause",
                "",
                "1. Pick a server. Every server starts from zero.",
                "2. Choose a land on the world map. Higher lands need a higher house level.",
                "3. Disable a shop's alarm at its red panel, then search shelves, cabinets and the register (hold E).",
                "4. Sell loot at the purple BLACK MARKET and escape through the green EXIT.",
                "5. Every 3 days the cycle ends: money, gear and house reset,",
                "    but your crimes become JAIL TIME, which unlocks new tech tree items (???)."
            };
            foreach (var l in lines) { GUI.Label(new Rect(x - 40, y, bw + 80, 26), l, sCenter); y += 26; }
            y += 10;
            if (ModelLoader.Status.Length > 0) Text(new Rect(x - 100, y - 4, bw + 200, 22), "Models: " + ModelLoader.Status, sSmall, new Color(0.6f, 0.9f, 0.6f));
            y += 34;
            if (GUI.Button(new Rect(x + 200, y, bw - 400, 52), "PLAY", sBtn)) { menuPage = 1; RefreshServerCards(); }
            return;
        }

        // انتخاب سرور
        GUI.Label(new Rect(x, y, bw, 36), "SELECT SERVER", sMid); y += 52;
        for (int i = 0; i < ServerCount; i++)
        {
            var c = serverCards[i];
            if (c == null) { RefreshServerCards(); c = serverCards[i]; }
            var r = new Rect(x - 20, y, bw + 40, 92);
            Fill(r, new Color(0.1f, 0.12f, 0.16f, 0.95f));
            Fill(new Rect(r.x, r.y, 6, r.height), c.exists ? new Color(0.3f, 0.8f, 1f) : new Color(0.4f, 0.4f, 0.45f));
            GUI.Label(new Rect(r.x + 20, r.y + 6, 300, 30), "SERVER " + ServerNames[i], sLabel);
            Text(new Rect(r.x + 20, r.y + 34, 300, 22), ServerRegions[i], sSmall, new Color(0.65f, 0.7f, 0.8f));
            if (c.exists)
            {
                Text(new Rect(r.x + 20, r.y + 58, 440, 22),
                    "Cycle " + c.cycle + "   House Lv " + c.house + "   Jail time " + FormatJail(c.jail), sSmall, Color.white);
                Text(new Rect(r.x + 20, r.y + 74, 440, 18),
                    c.remaining > 0 ? "Wipe in " + FormatSpan(c.remaining) : "Wipe pending - happens when you enter", sSmall, new Color(1f, 0.8f, 0.4f));
            }
            else Text(new Rect(r.x + 20, r.y + 62, 440, 22), "Empty - you start from zero (jail time 0)", sSmall, new Color(0.7f, 0.75f, 0.8f));

            if (GUI.Button(new Rect(r.xMax - 200, r.y + 12, 180, 38), c.exists ? "ENTER" : "NEW GAME", sBtn)) EnterServer(i);
            if (c.exists)
            {
                if (confirmWipeSlot == i)
                {
                    if (GUI.Button(new Rect(r.xMax - 200, r.y + 54, 180, 30), "Really delete?", sBtn)) { NativeCore.DeleteServer(ServerPath(i)); confirmWipeSlot = -1; RefreshServerCards(); }
                }
                else if (GUI.Button(new Rect(r.xMax - 200, r.y + 54, 180, 30), "Delete", sBtn)) confirmWipeSlot = i;
            }
            y += 100;
        }
        if (GUI.Button(new Rect(x + 230, y + 6, bw - 460, 40), "Back", sBtn)) { menuPage = 0; confirmWipeSlot = -1; }
    }

    void DrawLoading(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.04f, 0.05f, 0.08f, 1f));
        GUI.Label(new Rect(0, H / 2 - 70, W, 60), curLand != null ? curLand.name.ToUpper() : "LOADING", sBig);
        if (loadFailed) Text(new Rect(40, H / 2 - 10, W - 80, 90), loadText, sWrap, new Color(1f, 0.45f, 0.4f));
        else GUI.Label(new Rect(0, H / 2 - 10, W, 30), loadText, sCenter);
        Fill(new Rect(W / 2 - 250, H / 2 + 34, 500, 12), new Color(1, 1, 1, 0.15f));
        Fill(new Rect(W / 2 - 250, H / 2 + 34, 500 * Mathf.Clamp01(loadPct), 12), new Color(1f, 0.8f, 0.2f));
    }

    void DrawPause(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.55f));
        GUI.Label(new Rect(0, H / 2 - 110, W, 70), "PAUSED", sBig);
        if (GUI.Button(new Rect(W / 2 - 130, H / 2 - 20, 260, 44), "Resume (Esc)", sBtn)) paused = false;
        if (State == GameState.Base)
            if (GUI.Button(new Rect(W / 2 - 130, H / 2 + 34, 260, 44), "Switch server", sBtn)) LeaveToServerSelect();
    }

    // ---------------------------------------------------------------- HUD
    void DrawHud(float W, float H)
    {
        var p = Player;
        if (p == null) return;

        // سلامتی و جلیقه
        Fill(new Rect(20, H - 56, 260, 20), new Color(0, 0, 0, 0.55f));
        Fill(new Rect(20, H - 56, 260 * Mathf.Clamp01(p.health / 100f), 20), new Color(0.85f, 0.15f, 0.15f));
        GUI.Label(new Rect(24, H - 59, 260, 24), "HP " + Mathf.CeilToInt(p.health), sSmall);
        if (p.maxArmor > 0f)
        {
            Fill(new Rect(20, H - 82, 260, 20), new Color(0, 0, 0, 0.55f));
            Fill(new Rect(20, H - 82, 260 * Mathf.Clamp01(p.armor / p.maxArmor), 20), new Color(0.25f, 0.5f, 1f));
            GUI.Label(new Rect(24, H - 85, 260, 24), "ARMOR " + Mathf.CeilToInt(p.armor), sSmall);
        }

        // اسلحه
        var w = NativeCore.Weapons[p.weapon];
        string ammo = p.Reloading ? "RELOADING..." : p.Ammo + " / " + (int)w.mag;
        GUI.Label(new Rect(W - 380, H - 92, 360, 30), NativeCore.WeaponNames[p.weapon] + "   " + ammo, sRight);
        for (int i = 0; i < NativeCore.WeaponCount; i++)
        {
            var r = new Rect(W - 20 - (NativeCore.WeaponCount - i) * 56, H - 56, 52, 30);
            bool owned = NativeCore.OwnsWeapon(i);
            Fill(r, i == p.weapon ? new Color(1f, 0.8f, 0.2f, 0.9f) : new Color(0, 0, 0, owned ? 0.6f : 0.3f));
            Text(r, (i + 1).ToString(), sCenter, i == p.weapon ? Color.black : (owned ? Color.white : Color.gray));
        }

        // پول، سیکل و jail time
        int house = NativeCore.Profile(NativeCore.House);
        GUI.Label(new Rect(20, 12, 620, 28), "Bank: $" + NativeCore.Profile(NativeCore.Money) + "    House Lv " + house + "    " + Alias(), sLabel);
        Text(new Rect(20, 40, 560, 24), "Cycle " + NativeCore.Profile(NativeCore.CycleNo) + "  -  wipe in " + FormatSpan(NativeCore.CycleRemaining), sSmall, new Color(1f, 0.8f, 0.4f));
        Text(new Rect(20, 60, 560, 24), "Jail time " + FormatJail(NativeCore.Profile(NativeCore.Jail)) + "   (+" + FormatJail(NativeCore.JailPreview) + " this cycle)", sSmall, new Color(0.6f, 0.85f, 1f));
        float ly = 86;
        if (State == GameState.City)
        {
            Text(new Rect(20, ly, 560, 28), curLand.name + "  (market x" + marketMult.ToString("0.00") + ")", sSmall, new Color(0.8f, 0.8f, 0.85f)); ly += 22;
            Text(new Rect(20, ly, 500, 28), "Bag value: $" + BagValue(), sLabel, new Color(1f, 0.85f, 0.3f)); ly += 28;
            Text(new Rect(20, ly, 500, 28), "Cash this run: $" + runCash, sLabel, new Color(0.5f, 1f, 0.6f)); ly += 32;
        }
        else ly += 8;

        // ستاره‌های تحت تعقیب
        if (State == GameState.City)
        {
            string stars = "";
            for (int i = 0; i < 5; i++) stars += i < Wanted ? "*" : "-";
            Text(new Rect(W / 2 - 150, 10, 300, 40), "WANTED " + stars, sCenter, Wanted > 0 ? new Color(1f, 0.3f, 0.3f) : Color.gray);
            bool ringing = false;
            foreach (var sh in shops) if (sh.alarmTripped && Time.time < sh.alarmUntil) ringing = true;
            if (ringing && Mathf.Sin(Time.time * 8f) > 0f) Text(new Rect(W / 2 - 200, 46, 400, 30), "ALARM RINGING", sCenter, new Color(1f, 0.2f, 0.2f));
        }

        // نشانه‌گیر
        if (!InputBlocked)
        {
            float cx = W / 2, cy = H / 2;
            Color cc = new Color(1, 1, 1, 0.9f);
            Fill(new Rect(cx - 1, cy - 10, 2, 6), cc); Fill(new Rect(cx - 1, cy + 4, 2, 6), cc);
            Fill(new Rect(cx - 10, cy - 1, 6, 2), cc); Fill(new Rect(cx + 4, cy - 1, 6, 2), cc);
        }

        // راهنما و نوار جستجو / هک
        if (hint.Length > 0 && !InputBlocked)
        {
            Fill(new Rect(W / 2 - 300, H - 170, 600, 36), new Color(0, 0, 0, 0.55f));
            GUI.Label(new Rect(W / 2 - 300, H - 170, 600, 36), hint, sCenter);
            if (holdSpot != null)
            {
                Fill(new Rect(W / 2 - 200, H - 128, 400, 14), new Color(0, 0, 0, 0.6f));
                Fill(new Rect(W / 2 - 200, H - 128, 400 * HoldProgress01, 14), holdSpot.type == SpotType.AlarmPanel ? new Color(0.3f, 0.9f, 1f) : new Color(1f, 0.8f, 0.2f));
            }
        }

        // پیام‌ها
        float ty = Mathf.Max(ly, 120f) + 4f;
        foreach (var t in toasts)
        {
            Fill(new Rect(18, ty, 760, 26), new Color(0, 0, 0, 0.45f));
            GUI.Label(new Rect(24, ty, 750, 26), t.text, sSmall);
            ty += 28;
        }

        if (State == GameState.City) { DrawMinimap(W, H); DrawBag(W); }

        // فلش قرمز وقتی ضربه می‌خوری
        if (p.hurtTime > 0f) Fill(new Rect(0, 0, W, H), new Color(0.8f, 0f, 0f, Mathf.Clamp01(p.hurtTime * 1.2f)));
    }

    // لیست آیتم‌های داخل کیف (آیتم‌های کمیاب طلایی)
    void DrawBag(float W)
    {
        float x = W - 250f, y = 276f;
        Text(new Rect(x, y, 238, 24), "BAG " + bag.Count + "/" + BagCap + "   $" + BagValue(), sSmall, Color.white);
        y += 24f;
        foreach (int id in bag)
        {
            bool rare = NativeCore.ItemSpecial[id];
            Fill(new Rect(x, y, 238, 22), new Color(0f, 0f, 0f, 0.5f));
            Text(new Rect(x + 4, y, 232, 22), NativeCore.ItemNames[id] + "  $" + NativeCore.ItemValue[id], sSmall, rare ? new Color(1f, 0.8f, 0.2f) : new Color(0.9f, 0.9f, 0.9f));
            y += 24f;
        }
    }

    string LabelFor(Spot sp)
    {
        if (sp.type == SpotType.Shop && sp.shop != null)
        {
            var sh = sp.shop;
            string s = sp.label;
            if (sh.alarmTripped) s += "  [ALARM!]";
            else if (sh.alarmArmed) s += "  [ALARM ON]";
            else s += "  [alarm off]";
            if (NativeCore.HasIntel) s += "  (" + sh.Left + " left)";
            return s;
        }
        return sp.label;
    }

    Color LabelColor(Spot sp)
    {
        if (sp.type == SpotType.Shop && sp.shop != null)
        {
            if (sp.shop.alarmTripped) return new Color(1f, 0.3f, 0.3f);
            if (!sp.shop.alarmArmed) return Color.Lerp(sp.color, new Color(0.5f, 1f, 0.6f), 0.5f);
        }
        return sp.color;
    }

    void DrawWorldLabels(float s)
    {
        if (cam == null || Player == null) return;
        Vector3 pp = Player.transform.position;
        foreach (var sp in spots)
        {
            if (sp.done) continue;
            if (sp.type == SpotType.Container) continue;
            float lim = sp.type == SpotType.AlarmPanel ? 10f : 45f;
            if ((sp.pos - pp).sqrMagnitude > lim * lim) continue;
            Vector3 at = sp.type == SpotType.AlarmPanel ? sp.pos + Vector3.up * 2.1f : sp.pos + Vector3.up * 7f;
            Vector3 sc = cam.WorldToScreenPoint(at);
            if (sc.z <= 0f) continue;
            Text(new Rect(sc.x / s - 160, (Screen.height - sc.y) / s - 12, 320, 24), LabelFor(sp), sCenter, LabelColor(sp));
        }
    }

    // ---------------------------------------------------------------- Minimap
    bool bigMap;
    readonly Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();
    static readonly Color[] RoleCol =
    {
        new Color(0.35f, 0.78f, 0.42f), new Color(0.35f, 0.58f, 0.98f), new Color(0.98f, 0.82f, 0.32f),
        new Color(0.9f, 0.9f, 0.96f), new Color(0.75f, 0.4f, 0.98f), new Color(0.55f, 0.55f, 0.62f)
    };
    static readonly string[] RoleIcon = { "ic_grocery", "ic_electronics", "ic_jewelry", "ic_bank", "ic_market", "ic_home" };

    Texture2D Icon(string name)
    {
        Texture2D t;
        if (iconCache.TryGetValue(name, out t)) return t;
        t = Resources.Load<Texture2D>("Icons/" + name);
        iconCache[name] = t;
        return t;
    }

    Vector3 MapCenterWorld() { return new Vector3((CW - 1) * Cell * 0.5f, 0f, -(CH - 1) * Cell * 0.5f); }

    // آیکون روی نقشه: حلقه‌ی وضعیت + دیسک رنگی + نماد. اگه آیکون‌ها نباشن، مربع رنگی میکشه.
    void MapIcon(Vector2 p, float size, string glyph, Color disc, Color ring, float alpha = 1f)
    {
        var tDisc = Icon("ic_disc");
        var tGlyph = Icon(glyph);
        var old = GUI.color;
        if (tDisc == null || tGlyph == null)
        {
            Fill(new Rect(p.x - size / 2, p.y - size / 2, size, size), Color.black);
            Fill(new Rect(p.x - size / 2 + 1, p.y - size / 2 + 1, size - 2, size - 2), disc);
            return;
        }
        if (ring.a > 0.01f)
        {
            var tRing = Icon("ic_ring");
            if (tRing != null)
            {
                GUI.color = new Color(ring.r, ring.g, ring.b, ring.a * alpha);
                float rs = size * 1.28f;
                GUI.DrawTexture(new Rect(p.x - rs / 2, p.y - rs / 2, rs, rs), tRing);
            }
        }
        GUI.color = new Color(disc.r, disc.g, disc.b, alpha);
        GUI.DrawTexture(new Rect(p.x - size / 2, p.y - size / 2, size, size), tDisc);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        float gs = size * 0.74f;
        GUI.DrawTexture(new Rect(p.x - gs / 2, p.y - gs / 2, gs, gs), tGlyph);
        GUI.color = old;
    }

    // کنج صفحه: دنبال بازیکن، ۱۲۰ متر دید. کلید M: نقشه‌ی کامل لند
    void DrawMinimap(float W, float H)
    {
        if (Player == null) return;
        float size = 240f;
        var r = new Rect(W - size - 12, 12, size, size);
        DrawMapView(r, Player.transform.position, size / 120f, false);
        Text(new Rect(r.x, r.yMax + 1, size, 18), "M - full map", sSmallC, new Color(1f, 1f, 1f, 0.65f));

        if (bigMap)
        {
            Fill(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.72f));
            float bs = Mathf.Min(H - 80f, W - 360f);
            float mx = Mathf.Max(20f, (W - bs - 300f) * 0.5f);
            var br = new Rect(mx, 56f, bs, bs);
            GUI.Label(new Rect(0, 8, W, 40), curLand.name.ToUpper() + " - FULL MAP", sMid);
            DrawMapView(br, MapCenterWorld(), bs / (Mathf.Max(CW, CH) * Cell + 18f), true);
            DrawMapLegend(new Rect(br.xMax + 24f, 70f, 280f, 520f));
        }
    }

    void DrawMapLegend(Rect r)
    {
        Fill(r, new Color(0.08f, 0.09f, 0.12f, 0.92f));
        Text(new Rect(r.x + 12, r.y + 6, r.width - 24, 26), "LEGEND", sLabel, new Color(1f, 0.85f, 0.4f));
        string[] glyph = { "ic_grocery", "ic_electronics", "ic_jewelry", "ic_bank", "ic_market", "ic_exit", "ic_park", "ic_yard", "ic_home", "ic_cop" };
        string[] names = { "Grocery", "Electronics", "Jewelry Store", "Bank", "Black Market", "Exit", "Park", "Container yard", "Housing", "Police robot" };
        Color[] cols = { RoleCol[0], RoleCol[1], RoleCol[2], RoleCol[3], RoleCol[4], new Color(0.25f, 1f, 0.45f), new Color(0.3f, 0.65f, 0.35f), new Color(0.7f, 0.45f, 0.3f), RoleCol[5], new Color(1f, 0.2f, 0.2f) };
        float y = r.y + 40;
        for (int i = 0; i < glyph.Length; i++)
        {
            MapIcon(new Vector2(r.x + 30, y + 14), 24f, glyph[i], cols[i], new Color(0, 0, 0, 0));
            Text(new Rect(r.x + 56, y + 2, r.width - 64, 24), names[i], sSmall, Color.white);
            y += 32;
        }
        var tp = Icon("ic_player");
        if (tp != null) { var old = GUI.color; GUI.color = Color.white; GUI.DrawTexture(new Rect(r.x + 18, y + 2, 24, 24), tp); GUI.color = old; }
        Text(new Rect(r.x + 56, y + 2, r.width - 64, 24), "You (points where you look)", sSmall, Color.white);
        y += 40;
        Text(new Rect(r.x + 12, y, r.width - 24, 20), "Ring around a shop:", sTiny, new Color(0.8f, 0.82f, 0.88f)); y += 22;
        string[] rn = { "red = alarm armed", "green = alarm disabled", "flashing = alarm ringing", "gray = cleaned out" };
        Color[] rc = { new Color(1f, 0.3f, 0.25f), new Color(0.25f, 1f, 0.4f), Color.white, new Color(0.5f, 0.5f, 0.55f) };
        for (int i = 0; i < rn.Length; i++)
        {
            Fill(new Rect(r.x + 16, y + 6, 14, 6), rc[i]);
            Text(new Rect(r.x + 40, y, r.width - 48, 20), rn[i], sTiny, Color.white);
            y += 20;
        }
        Text(new Rect(r.x + 12, r.yMax - 28, r.width - 24, 22), "M / Esc - close", sTiny, new Color(0.7f, 0.72f, 0.78f));
    }

    void DrawMapView(Rect r, Vector3 center, float ppm, bool big)
    {
        GUI.BeginGroup(r);
        float cx = r.width * 0.5f, cy = r.height * 0.5f;
        Fill(new Rect(0, 0, r.width, r.height), Color.Lerp(new Color(0.06f, 0.27f, 0.44f), curLand.fog, 0.15f));   // دریا

        Vector3 mid = MapCenterWorld();
        float gx = (CW - 1) * Cell + 14f, gz = (CH - 1) * Cell + 14f;
        MapRect(cx, cy, ppm, center, mid, gx + 1.6f, gz + 1.6f, new Color(0.6f, 0.62f, 0.66f));                       // اسکله
        MapRect(cx, cy, ppm, center, mid, gx, gz, Color.Lerp(curLand.asphalt, new Color(0.2f, 0.2f, 0.23f), 0.6f));   // جاده‌ها

        float iconS = big ? 30f : 19f;
        foreach (var b in blockInfos)
        {
            Vector3 c = CellToWorld(new Vector2Int(b.x + 1, b.y + 1));
            MapRect(cx, cy, ppm, center, c, 20f, 20f, new Color(0.5f, 0.5f, 0.54f));                  // پیاده‌رو
            Vector2 p = MapPt(cx, cy, ppm, center, c);
            if (b.z == 6)
            {
                MapRect(cx, cy, ppm, center, c, 19f, 19f, new Color(0.25f, 0.5f, 0.28f));
                MapRect(cx, cy, ppm, center, c, 19f, 2f, new Color(0.7f, 0.68f, 0.62f));
                MapRect(cx, cy, ppm, center, c, 2f, 19f, new Color(0.7f, 0.68f, 0.62f));
                MapRect(cx, cy, ppm, center, c, 5f, 5f, new Color(0.3f, 0.6f, 0.85f));
                MapIcon(p + new Vector2(0f, big ? -26f : -15f), iconS * 0.78f, "ic_park", new Color(0.3f, 0.65f, 0.35f), new Color(0, 0, 0, 0), 0.92f);
            }
            else if (b.z == 7)
            {
                MapRect(cx, cy, ppm, center, c, 19f, 19f, new Color(0.42f, 0.38f, 0.32f));
                foreach (var o in new[] { new Vector2(-3.6f, -3.8f), new Vector2(3.6f, -3.8f), new Vector2(-3.6f, 3.8f), new Vector2(3.6f, 3.8f) })
                    MapRect(cx, cy, ppm, center, c + new Vector3(o.x, 0f, o.y), 6f, 2.4f, new Color(0.7f, 0.38f, 0.25f));
                MapIcon(p, iconS * 0.78f, "ic_yard", new Color(0.7f, 0.45f, 0.3f), new Color(0, 0, 0, 0), 0.92f);
            }
            else
            {
                MapRect(cx, cy, ppm, center, c, 11.6f, 11.6f, b.z <= 4 ? Color.Lerp(RoleCol[Mathf.Clamp(b.z, 0, 5)], new Color(0.3f, 0.3f, 0.34f), 0.55f) : RoleCol[5]);
                if (b.z <= 4) MapRect(cx, cy, ppm, center, c + new Vector3(0f, 0f, -5.5f), 2.6f, 1.2f, new Color(0.1f, 0.1f, 0.12f));   // در
                if (b.z == 5 && big) MapIcon(p, iconS * 0.7f, "ic_home", RoleCol[5], new Color(0, 0, 0, 0), 0.8f);
            }
        }

        // مغازه‌ها، بلک‌مارکت و خروجی: هر کدوم آیکون خودشون رو دارن
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
        foreach (var sp in spots)
        {
            if (sp.Minor || sp.type == SpotType.Garage) continue;
            Vector2 p = MapPt(cx, cy, ppm, center, sp.pos);
            if (p.x < -20 || p.y < -20 || p.x > r.width + 20 || p.y > r.height + 20) continue;
            if (sp.type == SpotType.Exit)
                MapIcon(p, iconS * 1.1f, "ic_exit", new Color(0.25f, 1f, 0.45f), new Color(1f, 1f, 1f, 0.9f));
            else if (sp.type == SpotType.Market)
                MapIcon(p, iconS * 1.1f, "ic_market", RoleCol[4], new Color(RoleCol[4].r, RoleCol[4].g, RoleCol[4].b, 0.6f + 0.4f * pulse));
            else if (sp.type == SpotType.Shop && sp.shop != null)
            {
                var sh = sp.shop;
                Color ring;
                if (sp.done) ring = new Color(0.5f, 0.5f, 0.55f);
                else if (sh.alarmTripped && Time.time < sh.alarmUntil) ring = pulse > 0.5f ? new Color(1f, 0.15f, 0.1f) : Color.white;
                else if (!sh.alarmArmed) ring = new Color(0.25f, 1f, 0.4f);
                else ring = new Color(1f, 0.3f, 0.25f);
                MapIcon(p, iconS, RoleIcon[Mathf.Clamp(sh.role, 0, 4)], sp.done ? new Color(0.5f, 0.5f, 0.55f) : RoleCol[Mathf.Clamp(sh.role, 0, 4)], ring, sp.done ? 0.7f : 1f);
                if (big && NativeCore.HasIntel && !sp.done) Text(new Rect(p.x - 14, p.y + iconS * 0.55f, 28, 18), sh.Left.ToString(), sSmallC, Color.white);
            }
        }

        // پلیس‌ها (وقتی تحت تعقیبی بزرگ‌تر و چشمک‌زن)
        foreach (var c in cops)
        {
            if (c == null) continue;
            Vector2 p = MapPt(cx, cy, ppm, center, c.transform.position);
            if (p.x < -10 || p.y < -10 || p.x > r.width + 10 || p.y > r.height + 10) continue;
            float s = (big ? 16f : 12f) + (Wanted > 0 ? pulse * 3f : 0f);
            MapIcon(p, s, "ic_cop", new Color(1f, 0.15f, 0.15f), new Color(0, 0, 0, 0));
        }

        // مقیاس و شمال
        float bar = 20f * ppm;
        Fill(new Rect(8, r.height - 14, bar, 4), Color.white);
        Text(new Rect(8 + bar + 4, r.height - 20, 50, 16), "20 m", sTiny, Color.white);
        Fill(new Rect(cx - 11, 3, 22, 20), new Color(0f, 0f, 0f, 0.5f));
        Text(new Rect(cx - 11, 3, 22, 20), "N", sSmallC, Color.white);
        GUI.EndGroup();

        // بازیکن: فلش به سمت نگاهت + مخروط دید.
        // عمداً بیرون از BeginGroup و با مختصات مطلق کشیده میشه: RotateAroundPivot داخل گروه مرکز چرخش رو جابجا میکنه.
        if (Player != null)
        {
            Vector2 lp = MapPt(r.width * 0.5f, r.height * 0.5f, ppm, center, Player.transform.position);
            if (lp.x > 4f && lp.y > 4f && lp.x < r.width - 4f && lp.y < r.height - 4f)
            {
                Vector2 ap = new Vector2(r.x + lp.x, r.y + lp.y);
                float edge = Mathf.Min(Mathf.Min(lp.x, r.width - lp.x), Mathf.Min(lp.y, r.height - lp.y));    // فاصله تا نزدیک‌ترین لبه
                float yaw = cam != null ? cam.transform.eulerAngles.y : Player.transform.eulerAngles.y;
                var tc = Icon("ic_cone");
                var tp = Icon("ic_player");
                float coneS = Mathf.Min((big ? 78f : 40f) * 2f, edge * 2f);
                float ps = Mathf.Min(big ? 40f : 28f, edge * 2f);
                Matrix4x4 old = GUI.matrix;
                GUIUtility.RotateAroundPivot(yaw, ap);
                if (tc != null) { GUI.color = new Color(1f, 1f, 0.7f, 0.8f); GUI.DrawTexture(new Rect(ap.x - coneS / 2f, ap.y - coneS / 2f, coneS, coneS), tc); GUI.color = Color.white; }
                if (tp != null) GUI.DrawTexture(new Rect(ap.x - ps / 2f, ap.y - ps / 2f, ps, ps), tp);
                else Fill(new Rect(ap.x - 4f, ap.y - 8f, 8f, 14f), Color.white);
                GUI.matrix = old;
            }
        }

        // قاب
        Color fr = new Color(1f, 1f, 1f, 0.75f);
        Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, 2), fr);
        Fill(new Rect(r.x - 2, r.yMax, r.width + 4, 2), fr);
        Fill(new Rect(r.x - 2, r.y, 2, r.height), fr);
        Fill(new Rect(r.xMax, r.y, 2, r.height), fr);
    }

    // نقطه‌ی دنیا → پیکسل داخل نقشه (شمال = بالا، z مثبت = شمال)
    Vector2 MapPt(float cx, float cy, float ppm, Vector3 center, Vector3 w)
    {
        return new Vector2(cx + (w.x - center.x) * ppm, cy - (w.z - center.z) * ppm);
    }

    void MapRect(float cx, float cy, float ppm, Vector3 center, Vector3 wc, float wm, float hm, Color c)
    {
        Vector2 p = MapPt(cx, cy, ppm, center, wc);
        Fill(new Rect(p.x - wm * ppm * 0.5f, p.y - hm * ppm * 0.5f, wm * ppm, hm * ppm), c);
    }

    // ---------------------------------------------------------------- Dead
    void DrawDead(float W, float H)
    {
        Fill(new Rect(0, 0, W, H), new Color(0.35f, 0f, 0f, 0.6f));
        GUI.Label(new Rect(0, H / 2 - 90, W, 70), "BUSTED", sBig);
        GUI.Label(new Rect(0, H / 2 - 20, W, 30), deathMsg, sCenter);
        GUI.Label(new Rect(0, H / 2 + 20, W, 30), "Press E to wake up at your base", sCenter);
    }

    // ---------------------------------------------------------------- Panels
    void DrawPanel(float W, float H)
    {
        switch (CurrentPanel)
        {
            case UiPanel.Weapons: DrawGearPanel(W, H, true); break;
            case UiPanel.Armor: DrawGearPanel(W, H, false); break;
            case UiPanel.Tech: DrawTechPanel(W, H); break;
            case UiPanel.House: DrawHousePanel(W, H); break;
            case UiPanel.Map: DrawMapPanel(W, H); break;
            case UiPanel.Summary: DrawSummaryPanel(W, H); break;
            case UiPanel.Intro: DrawIntroPanel(W, H); break;
            case UiPanel.Surrender: DrawSurrenderPanel(W, H); break;
        }
    }

    static Rect PanelRect(float W, float H, float bw, float bh, string title, GUIStyle st)
    {
        Fill(new Rect(0, 0, W, H), new Color(0, 0, 0, 0.45f));
        var r = new Rect((W - bw) / 2, (H - bh) / 2, bw, bh);
        Fill(r, new Color(0.08f, 0.09f, 0.12f, 0.96f));
        GUI.Label(new Rect(r.x, r.y + 8, bw, 44), title, st);
        return r;
    }

    void ClosePanelButton(Rect r)
    {
        if (GUI.Button(new Rect(r.x + r.width / 2 - 90, r.yMax - 52, 180, 38), "Close (Esc)", sBtn)) CurrentPanel = UiPanel.None;
    }

    // اسلحه‌ها / جلیقه‌ها: ناشناخته (???) تا تک‌تری باز نشه، بعد لول خونه، بعد پول
    void DrawGearPanel(float W, float H, bool weapons)
    {
        float bw = 860, bh = weapons ? 470 : 360;
        var r = PanelRect(W, H, bw, bh, weapons ? "WEAPON DEALER" : "ARMOR LOCKER", sBig);
        float x = r.x, y = r.y;
        GUI.Label(new Rect(x + 20, y + 56, bw - 40, 28), "Your money: $" + NativeCore.Profile(NativeCore.Money) + "      House level: " + NativeCore.Profile(NativeCore.House), sLabel);
        float ry = y + 100;

        int count = weapons ? NativeCore.WeaponCount : NativeCore.VestCount - 1;
        for (int n = 0; n < count; n++)
        {
            int i = weapons ? n : n + 1;
            int st = weapons ? NativeCore.WeaponState(i) : NativeCore.VestState(i);
            int price = weapons ? (int)NativeCore.Weapons[i].price : (int)NativeCore.VestPrice[i];
            int need = weapons ? (int)NativeCore.Weapons[i].house : NativeCore.VestHouse(i);
            Fill(new Rect(x + 14, ry - 4, bw - 28, 44), new Color(1, 1, 1, 0.04f));

            if (st == 0)
            {
                int tech = weapons ? (int)NativeCore.Weapons[i].tech : NativeCore.VestTech(i);
                int jailNeed = tech >= 0 ? NativeCore.TechInfo(tech, 2) : 0;
                Text(new Rect(x + 24, ry, 100, 28), "???", sLabel, new Color(0.55f, 0.6f, 0.66f));
                Text(new Rect(x + 90, ry + 4, 560, 28), "Required: House Lv " + need + "   +   Tech Tree (" + FormatJail(jailNeed) + " jail time)", sSmall, new Color(1f, 0.8f, 0.45f));
                GUI.enabled = false; GUI.Button(new Rect(x + bw - 200, ry, 176, 34), "LOCKED", sBtn); GUI.enabled = true;
            }
            else
            {
                string info;
                if (weapons)
                {
                    var w = NativeCore.Weapons[i];
                    info = NativeCore.WeaponNames[i] + "   DMG " + (int)w.damage + (w.pellets > 1 ? " x" + (int)w.pellets : "") + "   " + (1f / w.interval).ToString("0.0") + "/s   MAG " + (int)w.mag;
                }
                else info = NativeCore.VestNames[i] + "   Armor " + (int)NativeCore.ArmorOf(i);
                Text(new Rect(x + 24, ry + 4, 620, 28), info, sSmall, st == 1 ? new Color(0.75f, 0.75f, 0.8f) : Color.white);

                if (st == 3) { GUI.enabled = false; GUI.Button(new Rect(x + bw - 200, ry, 176, 34), "OWNED", sBtn); GUI.enabled = true; }
                else if (st == 1)
                {
                    Text(new Rect(x + bw - 420, ry + 4, 210, 28), "House Lv " + need + " needed", sSmall, new Color(1f, 0.65f, 0.4f));
                    GUI.enabled = false; GUI.Button(new Rect(x + bw - 200, ry, 176, 34), "$" + price, sBtn); GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = NativeCore.Profile(NativeCore.Money) >= price;
                    if (GUI.Button(new Rect(x + bw - 200, ry, 176, 34), "BUY  $" + price, sBtn)) { if (weapons) BuyWeapon(i); else BuyVest(i); }
                    GUI.enabled = true;
                }
            }
            ry += 52;
        }
        ClosePanelButton(r);
    }

    // درخت تکنولوژی: نودهای ناشناخته (???) با jail time کل باز میشن
    void DrawTechPanel(float W, float H)
    {
        float bw = Mathf.Min(1240f, W - 30f), bh = 640f;
        var r = PanelRect(W, H, bw, bh, "TECH TREE", sBig);
        int jail = NativeCore.Profile(NativeCore.Jail);
        Text(new Rect(r.x + 24, r.y + 54, bw - 48, 26),
            "Total jail time: " + FormatJail(jail) + "   (this cycle so far: +" + FormatJail(NativeCore.JailPreview) + ")      Next contact at: " + (NextTechNeed() > 0 ? FormatJail(NextTechNeed()) : "-") + "      Wipe in " + FormatSpan(NativeCore.CycleRemaining),
            sSmall, new Color(0.6f, 0.85f, 1f));
        Text(new Rect(r.x + 24, r.y + 76, bw - 48, 24), "Every 3 days the court sentences you for your crimes. Time inside builds contacts: jail time never resets and unlocks the ??? items.", sSmall, new Color(0.7f, 0.72f, 0.78f));

        float colW = (bw - 40f) / NativeCore.BranchNames.Length, nodeW = colW - 16f, nodeH = 80f, top = r.y + 112f, gap = 92f;
        for (int b = 0; b < NativeCore.BranchNames.Length; b++)
        {
            float cx = r.x + 20f + b * colW;
            Text(new Rect(cx, top - 20f, nodeW, 20), NativeCore.BranchNames[b], sSmallC, new Color(0.9f, 0.9f, 0.95f));
            for (int row = 0; row < 5; row++)
            {
                int i = TechAt(b, row);
                if (i < 0) continue;
                var nr = new Rect(cx, top + 8f + row * gap, nodeW, nodeH);
                int st = NativeCore.TechState(i);
                int need = NativeCore.TechInfo(i, 2), parent = NativeCore.TechInfo(i, 3);

                if (row > 0 && TechAt(b, row - 1) >= 0)
                    Fill(new Rect(nr.center.x - 2f, nr.y - (gap - nodeH), 4f, gap - nodeH), st == 2 ? new Color(0.3f, 0.9f, 0.5f) : new Color(0.3f, 0.32f, 0.38f));

                if (st == 2)
                {
                    Fill(nr, new Color(0.1f, 0.3f, 0.2f, 0.98f));
                    Fill(new Rect(nr.x, nr.y, 5, nr.height), new Color(0.3f, 0.9f, 0.5f));
                    Text(new Rect(nr.x + 12, nr.y + 4, nr.width - 14, 24), NativeCore.TechNames[i], sSmall, Color.white);
                    Text(new Rect(nr.x + 12, nr.y + 26, nr.width - 18, nr.height - 28), NativeCore.TechDescs[i], sWrap, new Color(0.8f, 0.95f, 0.85f));
                    if ((techNewShown & (1 << i)) != 0)
                    {
                        Fill(new Rect(nr.xMax - 54, nr.y + 4, 50, 20), new Color(1f, 0.8f, 0.2f));
                        Text(new Rect(nr.xMax - 54, nr.y + 4, 50, 20), "NEW!", sSmallC, Color.black);
                    }
                }
                else if (st == 1)
                {
                    Fill(nr, new Color(0.16f, 0.17f, 0.22f, 0.98f));
                    Fill(new Rect(nr.x, nr.y, 5, nr.height), new Color(0.9f, 0.75f, 0.3f));
                    Text(new Rect(nr.x + 12, nr.y + 4, nr.width - 14, 30), "???", sLabel, new Color(0.8f, 0.8f, 0.85f));
                    Text(new Rect(nr.x + 12, nr.y + 36, nr.width - 14, 20), "Needs " + FormatJail(need) + " jail time", sTiny, new Color(0.95f, 0.8f, 0.4f));
                    float f = need > 0 ? Mathf.Clamp01(jail / (float)need) : 1f;
                    Fill(new Rect(nr.x + 12, nr.yMax - 16, nr.width - 24, 6), new Color(1, 1, 1, 0.12f));
                    Fill(new Rect(nr.x + 12, nr.yMax - 16, (nr.width - 24) * f, 6), new Color(0.95f, 0.8f, 0.4f));
                }
                else
                {
                    Fill(nr, new Color(0.1f, 0.11f, 0.14f, 0.98f));
                    Text(new Rect(nr.x + 12, nr.y + 4, nr.width - 14, 30), "???", sLabel, new Color(0.4f, 0.42f, 0.48f));
                    Text(new Rect(nr.x + 12, nr.y + 36, nr.width - 14, 20), "Unknown", sTiny, new Color(0.4f, 0.42f, 0.48f));
                }
            }
        }
        ClosePanelButton(r);
    }

    // ارتقای خونه: لول‌های بالاتر، اسلحه/جلیقه/لند جدید باز میکنن
    void DrawHousePanel(float W, float H)
    {
        float bw = 860, bh = 520;
        var r = PanelRect(W, H, bw, bh, "HOUSE", sBig);
        float x = r.x + 24, y = r.y + 62;
        int lv = NativeCore.Profile(NativeCore.House);
        GUI.Label(new Rect(x, y, bw - 48, 32), NativeCore.HouseNames[Mathf.Clamp(lv, 1, 3)].ToUpper() + "  -  Level " + lv + " / " + NativeCore.HouseMax, sMid); y += 44;
        GUI.Label(new Rect(x, y, bw - 48, 28), "Your money: $" + NativeCore.Profile(NativeCore.Money), sLabel); y += 40;

        if (lv >= NativeCore.HouseMax)
        {
            GUI.Label(new Rect(x, y, bw - 48, 60), "Your house is fully upgraded.\nEverything you know is unlocked this cycle.", sCenter);
            ClosePanelButton(r);
            return;
        }

        int next = lv + 1;
        int cost = NativeCore.HouseCost(next), tier = NativeCore.HouseReqTier(next), runsReq = NativeCore.HouseReqRuns(next);
        int runs = NativeCore.TierRuns(tier);
        Text(new Rect(x, y, bw - 48, 28), "UPGRADE TO LEVEL " + next + " - " + NativeCore.HouseNames[next].ToUpper(), sLabel, new Color(1f, 0.8f, 0.3f)); y += 36;
        bool runsOk = runs >= runsReq;
        Text(new Rect(x, y, bw - 48, 24), (runsOk ? "[x] " : "[ ] ") + "Escape successfully from Tier " + tier + " lands: " + Mathf.Min(runs, runsReq) + " / " + runsReq, sSmall, runsOk ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.7f, 0.5f)); y += 24;
        bool moneyOk = NativeCore.Profile(NativeCore.Money) >= cost;
        Text(new Rect(x, y, bw - 48, 24), (moneyOk ? "[x] " : "[ ] ") + "Pay $" + cost, sSmall, moneyOk ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.7f, 0.5f)); y += 36;

        Text(new Rect(x, y, bw - 48, 24), "This unlocks:", sSmall, Color.white); y += 24;
        for (int i = 0; i < NativeCore.WeaponCount; i++)
            if ((int)NativeCore.Weapons[i].house == next)
            {
                bool known = NativeCore.WeaponState(i) != 0;
                Text(new Rect(x + 16, y, bw - 80, 22), "Weapon: " + NativeCore.WeaponNames[i] + (known ? "" : "   (also needs the tech tree)"), sSmall, known ? new Color(0.85f, 0.9f, 1f) : new Color(0.55f, 0.58f, 0.64f)); y += 22;
            }
        for (int v = 1; v < NativeCore.VestCount; v++)
            if (NativeCore.VestHouse(v) == next)
            {
                bool known = NativeCore.VestState(v) != 0;
                Text(new Rect(x + 16, y, bw - 80, 22), "Armor: " + NativeCore.VestNames[v] + (known ? "" : "   (also needs the tech tree)"), sSmall, known ? new Color(0.85f, 0.9f, 1f) : new Color(0.55f, 0.58f, 0.64f)); y += 22;
            }
        for (int l = 0; l < NativeCore.LandCount; l++)
            if (NativeCore.LandHouseReq(l) == next)
            { Text(new Rect(x + 16, y, bw - 80, 22), "Land: " + Lands.All[l].name + "  (Tier " + NativeCore.LandTier(l) + ")", sSmall, new Color(0.7f, 1f, 0.8f)); y += 22; }

        GUI.enabled = runsOk && moneyOk;
        if (GUI.Button(new Rect(r.x + bw / 2 - 150, r.yMax - 112, 300, 46), "UPGRADE  $" + cost, sBtn)) HouseUpgradeClicked();
        GUI.enabled = true;
        ClosePanelButton(r);
    }

    // نقشه‌ی جهان: لندهای تیر ۱ → ۲ → ۳
    void DrawMapPanel(float W, float H)
    {
        float bw = Mathf.Min(1120f, W - 30f), bh = 560f;
        var r = PanelRect(W, H, bw, bh, "WORLD MAP", sBig);
        var mapR = new Rect(r.x + 20, r.y + 64, bw - 400, bh - 130);
        Fill(mapR, new Color(0.05f, 0.1f, 0.16f));
        for (int g = 1; g < 10; g++)
        {
            Fill(new Rect(mapR.x + mapR.width * g / 10f, mapR.y, 1, mapR.height), new Color(1, 1, 1, 0.04f));
            Fill(new Rect(mapR.x, mapR.y + mapR.height * g / 10f, mapR.width, 1), new Color(1, 1, 1, 0.04f));
        }
        Text(new Rect(mapR.x + 8, mapR.y + 4, 200, 20), "TIER 1  >  TIER 2  >  TIER 3", sSmall, new Color(0.5f, 0.6f, 0.7f));

        var pos = new Vector2[NativeCore.LandCount];
        for (int i = 0; i < pos.Length; i++) pos[i] = new Vector2(mapR.x + Lands.All[i].mapPos.x * mapR.width, mapR.y + 20 + Lands.All[i].mapPos.y * (mapR.height - 40));
        int[,] edges = { { 0, 3 }, { 1, 3 }, { 2, 4 }, { 3, 5 }, { 4, 5 }, { 0, 1 }, { 1, 2 } };
        for (int e = 0; e < edges.GetLength(0); e++)
        {
            int a = edges[e, 0], b = edges[e, 1];
            bool open = NativeCore.LandUnlocked(a) && NativeCore.LandUnlocked(b);
            Line(pos[a], pos[b], 3f, open ? new Color(0.35f, 0.7f, 0.9f, 0.55f) : new Color(0.3f, 0.3f, 0.36f, 0.5f));
        }

        Color[] tierCol = { Color.white, new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.75f, 0.2f), new Color(0.95f, 0.3f, 0.35f) };
        for (int i = 0; i < pos.Length; i++)
        {
            bool unlocked = NativeCore.LandUnlocked(i);
            int tier = NativeCore.LandTier(i);
            float sz = 30f + tier * 8f;
            var nr = new Rect(pos[i].x - sz / 2, pos[i].y - sz / 2, sz, sz);
            if (mapSel == i) Fill(new Rect(nr.x - 5, nr.y - 5, nr.width + 10, nr.height + 10), new Color(1f, 1f, 1f, 0.9f));
            Fill(nr, unlocked ? tierCol[tier] : new Color(0.25f, 0.26f, 0.3f));
            Text(new Rect(nr.x, nr.y, nr.width, nr.height), unlocked ? tier.ToString() : "X", sCenter, unlocked ? Color.black : new Color(0.6f, 0.6f, 0.65f));
            Text(new Rect(pos[i].x - 100, nr.yMax + 2, 200, 20), Lands.All[i].name + "  (House Lv " + NativeCore.LandHouseReq(i) + ")", sSmallC, unlocked ? Color.white : new Color(0.55f, 0.57f, 0.62f));
            if (GUI.Button(nr, GUIContent.none, GUIStyle.none)) mapSel = i;
        }

        // اطلاعات لند انتخاب‌شده
        var d = Lands.All[mapSel];
        var ir = new Rect(mapR.xMax + 16, mapR.y, r.xMax - mapR.xMax - 36, mapR.height);
        Fill(ir, new Color(0.11f, 0.13f, 0.18f));
        float y = ir.y + 8, ix = ir.x + 12, iw = ir.width - 24;
        Text(new Rect(ix, y, iw, 30), d.name.ToUpper(), sLabel, Color.white); y += 32;
        Text(new Rect(ix, y, iw, 50), d.tagline, sWrap, new Color(0.75f, 0.78f, 0.85f)); y += 56;
        string[] rows =
        {
            "Tier: " + d.tier,
            "Size: " + ((d.bx * 6) * (int)Cell) + " x " + ((d.by * 6) * (int)Cell) + " m",
            "Danger: " + d.danger,
            "Loot value: x" + (NativeCore.LandLootX100(mapSel) / 100f).ToString("0.00"),
            "Black market: x" + (NativeCore.LandMarketLo(mapSel) / 100f).ToString("0.00") + " - x" + (NativeCore.LandMarketHi(mapSel) / 100f).ToString("0.00"),
            "Police at start: " + NativeCore.LandCopStart(mapSel),
            "Alarm: " + NativeCore.AlarmStars(0, mapSel) + "-" + NativeCore.AlarmStars(3, mapSel) + " stars",
            "Escapes this cycle: " + NativeCore.Profile(NativeCore.Land0 + mapSel)
        };
        foreach (var row in rows) { Text(new Rect(ix, y, iw, 22), row, sSmall, Color.white); y += 22; }

        bool ok = NativeCore.LandUnlocked(mapSel);
        if (!ok) Text(new Rect(ix, ir.yMax - 96, iw, 40), "Needs house level " + NativeCore.LandHouseReq(mapSel), sWrap, new Color(1f, 0.6f, 0.4f));
        GUI.enabled = ok;
        if (GUI.Button(new Rect(ix, ir.yMax - 50, iw, 40), "ENTER", sBtn)) StartLand(mapSel);
        GUI.enabled = true;
        ClosePanelButton(r);
    }

    // خط فدرال: قبل از پایان سیکل خودت میری تسلیم میشی و دادگاه همون لحظه برگزار میشه
    void DrawSurrenderPanel(float W, float H)
    {
        float bw = 800, bh = 540;
        var r = PanelRect(W, H, bw, bh, "FEDERAL HOTLINE", sMid);
        float x = r.x + 34, y = r.y + 70, w = bw - 68;
        int preview = NativeCore.JailPreview, jail = NativeCore.Profile(NativeCore.Jail);
        Color gold = new Color(1f, 0.82f, 0.4f), blue = new Color(0.6f, 0.85f, 1f), warn = new Color(0.95f, 0.6f, 0.5f);

        GUI.Label(new Rect(x, y, w, 60), "Don't want to wait for the deadline? Call the Feds and turn yourself in now. The court meets immediately.", sWrap); y += 66;
        Text(new Rect(x, y, w, 26), "Jail time you would receive now:  +" + FormatJail(preview), sLabel, blue); y += 34;
        Text(new Rect(x, y, w, 24), "Total jail time after:  " + FormatJail(jail + preview) + "      Cycle clock: " + FormatSpan(NativeCore.CycleRemaining) + " left", sSmall, Color.white); y += 32;

        int gain = NativeCore.TechMaskWith(preview) & ~NativeCore.TechMask;
        string names = "";
        int count = 0;
        for (int i = 0; i < NativeCore.TechCount; i++) if ((gain & (1 << i)) != 0) { count++; names += (names.Length > 0 ? ", " : "") + NativeCore.TechNames[i]; }
        Text(new Rect(x, y, w, 24), count > 0 ? "You would unlock " + count + " new contact(s):" : "No new contacts yet (next: " + (NextTechNeed() > 0 ? FormatJail(NextTechNeed()) + " jail time" : "-") + ")", sSmall, gold); y += 24;
        if (count > 0) { GUI.Label(new Rect(x + 10, y, w - 20, 60), names, sWrap); y += 62; } else y += 8;

        Text(new Rect(x, y, w, 24), "What happens:", sSmall, warn); y += 24;
        string[] lines =
        {
            "- Money, weapons, armor, house level and land progress are reset (tech perks like Parole Cash still apply).",
            "- A new 72-hour cycle starts now; the time left on the old one is lost.",
            "- Jail time is permanent and unlocks the ??? items in the tech tree."
        };
        foreach (var l in lines) { GUI.Label(new Rect(x + 10, y, w - 20, 40), l, sWrap); y += 38; }

        if (preview <= 0)
        {
            Text(new Rect(x, r.yMax - 110, w, 24), "You haven't done anything worth confessing yet. Rob something first.", sSmall, warn);
            GUI.enabled = false;
        }
        if (GUI.Button(new Rect(r.x + bw / 2 - 150, r.yMax - 100, 300, 44), surrenderConfirm ? "REALLY TURN MYSELF IN?" : "TURN MYSELF IN", sBtn))
        {
            if (!surrenderConfirm) surrenderConfirm = true;
            else if (NativeCore.Surrender()) { surrenderConfirm = false; StartBase(null, true); }
        }
        GUI.enabled = true;
        ClosePanelButton(r);
    }

    // ---------------------------------------------------------------- Story: intro & trial
    // مقدمه‌ی سرور تازه
    void DrawIntroPanel(float W, float H)
    {
        float bw = 820, bh = 520;
        var r = PanelRect(W, H, bw, bh, "RELEASED ON PAROLE", sMid);
        float x = r.x + 34, y = r.y + 72, w = bw - 68;
        Text(new Rect(x, y, w, 30), "You are " + Alias().ToUpper() + ".", sLabel, new Color(0.6f, 0.85f, 1f)); y += 44;
        string[] paras =
        {
            "You walk out of prison with nothing but a plan.",
            "The Feds gave you 72 hours of freedom. When the cycle ends, you must report to the Federal Building.",
            "Until then: rob shops, sell the loot, upgrade your house, buy better gear.",
            "At the end the court counts every crime, seizes everything you own and sentences you. That jail time is never lost - inside you make contacts, and contacts unlock new weapons, armor and tricks in the tech tree.",
            "Then you are released again, with nothing - but a little smarter."
        };
        foreach (var t in paras)
        {
            GUI.Label(new Rect(x, y, w, 70), t, sWrap);
            y += t.Length > 100 ? 66f : 40f;
        }
        if (GUI.Button(new Rect(r.x + bw / 2 - 110, r.yMax - 56, 220, 40), "Start your first cycle", sBtn)) CurrentPanel = UiPanel.None;
    }

    string ChargeText(int i)
    {
        switch (i)
        {
            case 0: return "Breaking and entering - " + NativeCore.Snap(NativeCore.Shops) + " shop(s)";
            case 1: return "Grand larceny - " + NativeCore.Snap(NativeCore.Looted) + " container(s) searched";
            case 2: return "Robbing " + NativeCore.Snap(NativeCore.Registers) + " cash register(s)";
            case 3: return "Setting off " + NativeCore.Snap(NativeCore.Alarms) + " alarm(s)";
            case 4: return "Tampering with " + NativeCore.Snap(NativeCore.Disarmed) + " security system(s)";
            case 5: return "Destroying " + NativeCore.Snap(NativeCore.Kills) + " police robot(s)";
            case 6: return "Evading police at wanted level " + NativeCore.Snap(NativeCore.MaxWanted);
            case 7: return "Getting arrested " + NativeCore.Snap(NativeCore.Busted) + " time(s)";
            case 8: return "Fleeing " + NativeCore.Snap(NativeCore.Runs) + " crime scene(s) with the goods";
            default: return "Possession of stolen goods worth $" + NativeCore.Snap(NativeCore.LootValue);
        }
    }

    // بعد از ریست ۳ روزه: معرفی به فدرال‌ها ← اتهام‌ها ← حکم دادگاه ← آشنایی‌های زندان (آیتم‌های جدید)
    void DrawSummaryPanel(float W, float H)
    {
        string[] titles = { "THE FEDERAL BUILDING", "THE CHARGES", "THE COURT", "TIME SERVED" };
        float bw = 860, bh = 640;
        var r = PanelRect(W, H, bw, bh, titles[Mathf.Clamp(trialStep, 0, 3)], sMid);
        float x = r.x + 34, y = r.y + 70, w = bw - 68;
        string alias = Alias();
        int cycleDone = NativeCore.Profile(NativeCore.CycleNo) - 1;
        int sentence = NativeCore.Profile(NativeCore.LastJail);
        Color gold = new Color(1f, 0.82f, 0.4f), blue = new Color(0.6f, 0.85f, 1f), grey = new Color(0.55f, 0.58f, 0.64f);

        switch (trialStep)
        {
            case 0:
                if (surrenderedEarly)
                {
                    Text(new Rect(x, y, w, 30), "CYCLE " + cycleDone + " - YOU TURN YOURSELF IN", sLabel, gold); y += 46;
                    GUI.Label(new Rect(x, y, w, 70), "You pick up the phone and tell the Feds you're coming in on your own. No need to wait for the deadline.", sWrap); y += 70;
                }
                else
                {
                    Text(new Rect(x, y, w, 30), "CYCLE " + cycleDone + " IS OVER", sLabel, gold); y += 46;
                    GUI.Label(new Rect(x, y, w, 70), "The 72 hours are up. Your parole says you report to the Federal Building - or they come for you.", sWrap); y += 70;
                }
                GUI.Label(new Rect(x, y, w, 70), "You push through the doors with your hands raised. The robots at the metal detector go quiet.", sWrap); y += 80;
                Text(new Rect(x, y, w, 30), alias.ToUpper() + ":", sSmall, blue); y += 24;
                GUI.Label(new Rect(x + 16, y, w - 16, 50), "\"My name is " + alias + ". I did it all. Write it down.\"", sWrap); y += 70;
                Text(new Rect(x, y, w, 30), "FEDERAL AGENT:", sSmall, gold); y += 24;
                GUI.Label(new Rect(x + 16, y, w - 16, 60), "\"Take a seat, " + alias + ". Let's go through your file.\"", sWrap);
                break;

            case 1:
            {
                Text(new Rect(x, y, w, 30), "FEDERAL AGENT reads the charges:", sSmall, gold); y += 32;
                int shown = 0;
                for (int i = 0; i < NativeCore.JailParts; i++)
                {
                    int v = NativeCore.SnapJailPart(i);
                    if (v <= 0) continue;
                    shown++;
                    Fill(new Rect(x, y - 1, w, 24), new Color(1, 1, 1, shown % 2 == 0 ? 0.05f : 0.02f));
                    Text(new Rect(x + 8, y, w - 160, 22), "- " + ChargeText(i), sSmall, Color.white);
                    Text(new Rect(x + w - 150, y, 142, 22), "+" + v + " min", sSmall, blue);
                    y += 26;
                }
                if (shown == 0)
                {
                    GUI.Label(new Rect(x, y, w, 80), "The agent flips through an empty file.\n\"You did nothing this cycle. Wasted trip.\"", sWrap);
                    y += 80;
                }
                Fill(new Rect(x, y + 4, w, 2), new Color(1, 1, 1, 0.25f)); y += 14;
                Text(new Rect(x, y, w, 26), "Stolen goods this cycle: $" + NativeCore.Snap(NativeCore.LootValue) + "      Earned: $" + NativeCore.Snap(NativeCore.Earned) + "      Best run: $" + NativeCore.Snap(NativeCore.BestRun), sSmall, grey);
                break;
            }

            case 2:
            {
                GUI.Label(new Rect(x, y, w, 60), "The judge doesn't look up from the file.", sWrap); y += 50;
                Text(new Rect(x, y, w, 26), "JUDGE:", sSmall, gold); y += 24;
                GUI.Label(new Rect(x + 16, y, w - 16, 90), "\"" + alias + ", the court finds you guilty on all counts. You are sentenced to " + FormatJail(sentence) + " of jail time.\"", sWrap); y += 100;
                Text(new Rect(x, y, w, 40), "SENTENCE:  " + FormatJail(sentence), sMid, blue); y += 52;
                Text(new Rect(x, y, w, 26), "Total time served: " + FormatJail(NativeCore.Profile(NativeCore.Jail)), sLabel, Color.white); y += 46;
                Text(new Rect(x, y, w, 24), "Seized: your money, weapons, armor and your house.", sSmall, new Color(0.95f, 0.6f, 0.5f)); y += 26;
                int money = NativeCore.Profile(NativeCore.Money), vest = NativeCore.Profile(NativeCore.Vest);
                if (money > 0) { Text(new Rect(x, y, w, 24), "Your lawyers hid $" + money + " before the seizure.", sSmall, new Color(0.5f, 1f, 0.6f)); y += 26; }
                if (vest > 0) { Text(new Rect(x, y, w, 24), "A care package is waiting: you start with a " + NativeCore.VestNames[vest] + ".", sSmall, new Color(0.5f, 1f, 0.6f)); y += 26; }
                break;
            }

            default:
            {
                GUI.Label(new Rect(x, y, w, 60), "Behind bars, nobody wastes time. You meet people who know people.", sWrap); y += 50;
                int mask = NativeCore.SnapNewTechMask, n = 0, total = 0;
                for (int i = 0; i < NativeCore.TechCount; i++) if ((mask & (1 << i)) != 0) total++;
                float rowH = total > 12 ? 22f : 25f;
                for (int i = 0; i < NativeCore.TechCount; i++)
                    if ((mask & (1 << i)) != 0)
                    {
                        n++;
                        Fill(new Rect(x, y - 1, w, rowH - 1f), new Color(0.3f, 0.9f, 0.5f, 0.07f));
                        Text(new Rect(x + 8, y, 200, 22), NativeCore.TechNames[i], sSmall, new Color(0.5f, 1f, 0.65f));
                        Text(new Rect(x + 214, y + 1, w - 214, 22), NativeCore.TechDescs[i], sTiny, new Color(0.8f, 0.85f, 0.9f));
                        y += rowH;
                    }
                if (n == 0) { GUI.Label(new Rect(x, y, w, 60), "Nobody inside has anything to offer yet. Commit bigger crimes.", sWrap); y += 40; }
                int next = NextTechNeed();
                y += 8;
                if (next > 0) Text(new Rect(x, y, w, 24), "Next contact: ???  at " + FormatJail(next) + " jail time  (you have " + FormatJail(NativeCore.Profile(NativeCore.Jail)) + ")", sSmall, gold);
                else Text(new Rect(x, y, w, 24), "You know everyone worth knowing.", sSmall, gold);
                break;
            }
        }

        if (trialStep > 0 && GUI.Button(new Rect(r.x + 34, r.yMax - 56, 140, 40), "< Back", sBtn)) trialStep--;
        if (trialStep < 3)
        {
            if (GUI.Button(new Rect(r.xMax - 174, r.yMax - 56, 140, 40), "Next >", sBtn)) trialStep++;
        }
        else
        {
            if (GUI.Button(new Rect(r.xMax - 390, r.yMax - 56, 180, 40), "Open Tech Tree", sBtn)) OpenPanel(UiPanel.Tech);
            if (GUI.Button(new Rect(r.xMax - 194, r.yMax - 56, 160, 40), "Walk free", sBtn)) CurrentPanel = UiPanel.None;
        }
    }
}
