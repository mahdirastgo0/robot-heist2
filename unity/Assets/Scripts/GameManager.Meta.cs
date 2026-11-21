using System.IO;
using UnityEngine;

// لایه‌ی متا: سرورها (هر کدوم یه پروفایل جدا)، سیکل ۳ روزه، درخت تکنولوژی و ارتقای خونه.
// همه‌ی قوانین تو هسته‌ی C++ هستن؛ اینجا فقط جریان بازی و نمایش.
public partial class GameManager
{
    public const int ServerCount = 3;
    static readonly string[] ServerNames = { "ALPHA", "BRAVO", "CHARLIE" };
    static readonly string[] ServerRegions = { "Europe", "Americas", "Asia" };

    int menuPage;                 // 0 = صفحه‌ی اصلی، 1 = انتخاب سرور
    int serverSlot = -1;
    int techNewShown;             // نودهای تازه باز شده (برای برچسب NEW)
    int mapSel;                   // لند انتخاب‌شده تو نقشه‌ی جهان
    int confirmWipeSlot = -1;
    int trialStep;                // مرحله‌ی دادگاه بعد از ریست (۰ تا ۳)
    bool surrenderedEarly;        // این دادگاه چون خودت تسلیم شدی برگزار شده
    bool surrenderConfirm;        // کلیک اول روی «تسلیم» زده شده

    static readonly string[] AliasA = { "Silent", "Rusty", "Crimson", "Lucky", "Shadow", "Iron", "Cold", "Wild" };
    static readonly string[] AliasB = { "Fox", "Viper", "Raven", "Wolf", "Ghost", "Bishop", "Mako", "Dutch" };

    // اسم مستعار کاراکتر (برای هر سرور ثابته)
    public static string Alias()
    {
        long c = NativeCore.Profile64(NativeCore.Created);
        if (c < 0) c = -c;
        return AliasA[(int)(c % AliasA.Length)] + " " + AliasB[(int)((c / AliasA.Length) % AliasB.Length)];
    }

    // کمترین jail time لازم برای نود بعدی (۰ = همه باز شدن)
    static int NextTechNeed()
    {
        int best = 0;
        int jail = NativeCore.Profile(NativeCore.Jail);
        for (int i = 0; i < NativeCore.TechCount; i++)
            if (NativeCore.TechState(i) == 1)
            {
                int need = NativeCore.TechInfo(i, 2);
                if (need > jail && (best == 0 || need < best)) best = need;
            }
        return best;
    }

    class ServerCard { public bool exists; public int cycle, jail, house, money; public long remaining; }
    readonly ServerCard[] serverCards = new ServerCard[ServerCount];

    string ServerPath(int slot) { return Path.Combine(Application.persistentDataPath, "server" + (slot + 1) + ".sav"); }

    void RefreshServerCards()
    {
        for (int i = 0; i < ServerCount; i++)
        {
            var c = new ServerCard();
            string p = ServerPath(i);
            long start = NativeCore.Peek(p, NativeCore.CycleStart);
            c.exists = start >= 0;
            if (c.exists)
            {
                c.cycle = (int)NativeCore.Peek(p, NativeCore.CycleNo);
                c.jail = (int)NativeCore.Peek(p, NativeCore.Jail);
                c.house = (int)NativeCore.Peek(p, NativeCore.House);
                c.money = (int)NativeCore.Peek(p, NativeCore.Money);
                c.remaining = start + NativeCore.CycleLength - NativeCore.Now;
            }
            serverCards[i] = c;
        }
    }

    // وارد سرور شدن: اگه سرور تازه باشه از صفر شروع میشه (jail time = 0، خونه لول ۱، فقط تپانچه)
    void EnterServer(int slot)
    {
        int r = NativeCore.OpenServer(ServerPath(slot));
        if (r == 0) { fatalMsg = "Could not open the save file:\n" + ServerPath(slot); return; }
        serverSlot = slot;
        menuPage = 0;
        StartBase(r == 1
            ? "Welcome to server " + ServerNames[slot] + "! You start from zero. Jail time: 0."
            : "Welcome back to server " + ServerNames[slot] + ".");
        if (r == 1) CurrentPanel = UiPanel.Intro;      // مقدمه‌ی داستان برای سرور تازه
    }

    void LeaveToServerSelect()
    {
        paused = false;
        CurrentPanel = UiPanel.None;
        State = GameState.Menu;
        menuPage = 1;
        RefreshServerCards();
    }

    void HouseUpgradeClicked()
    {
        int r = NativeCore.HouseUpgrade();
        if (r == 1)
        {
            int lv = NativeCore.Profile(NativeCore.House);
            StartBase("House upgraded to level " + lv + "! New weapons, armor and lands are unlocked.");
            if (GameAudio.I != null) GameAudio.I.Coin();
        }
        else if (r == 0) Toast("Not enough money.");
        else if (r == -2) Toast("Complete more escapes first (see the requirement).");
        else Toast("Your house is already at the maximum level.");
    }

    // ---------------------------------------------------------------- Helpers
    public static string FormatSpan(long sec)
    {
        if (sec < 0) sec = 0;
        long d = sec / 86400, h = (sec % 86400) / 3600, m = (sec % 3600) / 60, s = sec % 60;
        if (d > 0) return d + "d " + h.ToString("00") + "h " + m.ToString("00") + "m";
        if (h > 0) return h + "h " + m.ToString("00") + "m";
        return m.ToString("00") + ":" + s.ToString("00");
    }

    static string FormatJail(int minutes)
    {
        if (minutes < 60) return minutes + " min";
        return (minutes / 60) + "h " + (minutes % 60).ToString("00") + "m";
    }

    // نود درخت تکنولوژی که تو ستون/ردیف داده شده
    static int TechAt(int branch, int row)
    {
        for (int i = 0; i < NativeCore.TechCount; i++)
            if (NativeCore.TechInfo(i, 0) == branch && NativeCore.TechInfo(i, 1) == row) return i;
        return -1;
    }
}
