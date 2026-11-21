using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// پل بین C# و هسته‌ی C++ (RobotHeistCore.dll داخل Assets/Plugins/x86_64)
// همه‌ی قوانین (قیمت‌ها، شرط‌های باز شدن، jail time، سیکل ۳ روزه) تو هسته‌ی C++ هستن؛ اینجا فقط اسم‌ها و پل هست.
public static class NativeCore
{
    const string Lib = "RobotHeistCore";
    const CallingConvention Conv = CallingConvention.Cdecl;
    public const int ApiVersion = 2;

    // فیلدهای پروفایل (دقیقاً مثل enum Field تو core.cpp)
    public const int Money = 0, WeaponMask = 1, Vest = 2, House = 3, Runs = 4, Earned = 5, Kills = 6, BestRun = 7,
        Shops = 8, Looted = 9, Registers = 10, Alarms = 11, Disarmed = 12, MaxWanted = 13, Busted = 14, LootValue = 15,
        Land0 = 16, CycleEnd = 22,
        Jail = 22, CycleNo = 23, CycleStart = 24, TechSeen = 25, LastJail = 26,
        LifeKills = 27, LifeLooted = 28, LifeEarned = 29, Created = 30, FieldCount = 31;

    public const int WeaponCount = 5, VestCount = 4, LandCount = 6, TechCount = 25, JailParts = 10;
    public const int HouseMax = 3;

    public static readonly string[] WeaponNames = { "Pistol", "SMG", "Shotgun", "Assault Rifle", "LMG" };
    public static readonly string[] VestNames = { "No Vest", "Light Vest", "Medium Vest", "Heavy Vest" };

    // آیتم‌های سرقت (ترتیب دقیقاً مثل جدول C++). آیتم‌های خاص با [RARE] مشخص میشن
    public const int ItemCount = 19;
    public static readonly string[] ItemNames =
    {
        "Cash Roll", "Cigarette Carton", "Premium Whiskey", "Vintage Wine",
        "Smartphone", "Laptop", "Game Console", "Camera", "Prototype Chip",
        "Silver Necklace", "Gold Watch", "Diamond Ring", "Pearl Set", "Royal Ruby",
        "Cash Bundle", "Gold Bar", "Bearer Bonds", "Safe Deposit Box", "Cartel Ledger"
    };
    public static int[] ItemValue = new int[ItemCount];
    public static bool[] ItemSpecial = new bool[ItemCount];

    // اسم و توضیح نودهای درخت تکنولوژی (ترتیب مثل kTech تو core.cpp). هر نود یه آشنایی از زندانه.
    public static readonly string[] TechNames =
    {
        "Pistol", "SMG", "Shotgun", "Assault Rifle", "LMG",
        "Light Vest", "Medium Vest", "Heavy Vest",
        "Alarm Jammer", "Jammer Mk II", "Fast Hands", "Thermal Drill", "Insider Intel",
        "Bigger Bag", "Fence Contacts", "Heat Sink", "Bag Mk II", "Cartel Fence",
        "Public Defender", "Wire Cutters", "Fed Informant", "Parole Cash", "Presidential Pardon",
        "Care Package", "Kevlar Contract"
    };
    public static readonly string[] TechDescs =
    {
        "Glock 19. Your trusty sidearm.",
        "Rapid fire, low damage. Cheap on the street.",
        "Dutch (cell 12) knows a guy with pump-actions. 8 pellets.",
        "An ex-soldier cellmate sells you rifle contacts.",
        "Mama the arms dealer owes you. 100-round belt.",
        "Basic plates under your jacket.",
        "A guard sells you better plates.",
        "Ceramic plates smuggled out in the laundry.",
        "Hack a shop alarm at its outside panel.",
        "An electrician cellmate upgrades your jammer.",
        "Slim the pickpocket teaches you. Search 25% faster.",
        "A safecracker's drill. Opens vault safes.",
        "A bank clerk's tips: labels show loot left.",
        "The tailor in block C: +2 bag slots.",
        "A fence outside: +10% black market prices.",
        "Lay-low tricks: wanted level cools faster.",
        "A custom gear bag: +2 more bag slots.",
        "The cartel buys anything: +25% prices.",
        "Busted? You keep 30% of your unbanked cash.",
        "Alarms raise one fewer wanted star.",
        "A fed on the take: one fewer cop at the start.",
        "Start every cycle with $1500 from your stash.",
        "Keep 25% of your money through the wipe.",
        "Start every cycle wearing a Medium Vest.",
        "All vests give +25% armor."
    };
    public static readonly string[] BranchNames = { "WEAPONS", "ARMOR", "TOOLS", "ECONOMY", "LEGAL" };
    public static readonly string[] ContainerNames = { "Shelf", "Cabinet", "Cash Register", "Display Case", "Safe" };
    public static readonly string[] JailPartNames =
    {
        "Shops broken into", "Containers looted", "Registers emptied", "Alarms triggered", "Alarms disarmed",
        "Cops taken down", "Highest wanted level", "Times busted", "Successful escapes", "Loot stolen ($1000s)"
    };
    public static readonly string[] HouseNames = { "", "Hideout", "Safehouse", "Penthouse" };

    public struct WeaponStats
    {
        public float damage, interval, mag, reload, spread, range, price, pellets, house, tech;
    }

    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_api_version();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_field_count();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tech_count();
    [DllImport(Lib, CallingConvention = Conv)]
    static extern int rh_map_set([MarshalAs(UnmanagedType.LPStr)] string map, int cols, int rows);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_find_path(int fx, int fy, int tx, int ty, int[] outXY, int maxPairs);
    [DllImport(Lib, CallingConvention = Conv)]
    static extern int rh_server_open([MarshalAs(UnmanagedType.LPStr)] string path, long now);
    [DllImport(Lib, CallingConvention = Conv)]
    static extern long rh_peek([MarshalAs(UnmanagedType.LPStr)] string path, int field);
    [DllImport(Lib, CallingConvention = Conv)]
    static extern int rh_server_delete([MarshalAs(UnmanagedType.LPStr)] string path);
    [DllImport(Lib, CallingConvention = Conv)] static extern long rh_profile_get(int field);
    [DllImport(Lib, CallingConvention = Conv)] static extern void rh_profile_add(int field, long delta);
    [DllImport(Lib, CallingConvention = Conv)] static extern void rh_profile_max(int field, long v);
    [DllImport(Lib, CallingConvention = Conv)] static extern void rh_cycle_set_length(long sec);
    [DllImport(Lib, CallingConvention = Conv)] static extern long rh_cycle_length();
    [DllImport(Lib, CallingConvention = Conv)] static extern long rh_cycle_remaining(long now);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_cycle_check(long now);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_surrender(long now);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tech_mask_with(int extra);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_jail_preview();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_jail_part(int i);
    [DllImport(Lib, CallingConvention = Conv)] static extern long rh_snap_get(int field);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_snap_jail_part(int i);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_snap_new_tech_mask();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tech_info(int i, int field);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tech_state(int i);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tech_mask();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tech_new_mask();
    [DllImport(Lib, CallingConvention = Conv)] static extern void rh_tech_mark_seen();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_bonus(int kind);
    [DllImport(Lib, CallingConvention = Conv)] static extern float rh_weapon_stat(int idx, int stat);
    [DllImport(Lib, CallingConvention = Conv)] static extern float rh_vest_stat(int level, int stat);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_weapon_state(int idx);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_vest_state(int level);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_buy_weapon(int idx);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_buy_vest(int level);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_house_info(int level, int field);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_tier_runs(int tier);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_house_upgrade();
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_land_info(int i, int field);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_land_unlocked(int i);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_item_value(int id);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_item_special(int id);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_container_info(int kind, int field);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_loot_pick(int shopType, int kind, int rnd);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_alarm_stars(int shopType, int land);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_hack_time(int shopType);
    [DllImport(Lib, CallingConvention = Conv)] static extern int rh_sell_price(int value, int marketX100, int landX100);
    [DllImport(Lib, CallingConvention = Conv)] static extern void rh_bank_run(int cash, int land);

    public static bool Available { get; private set; }
    public static string Error { get; private set; } = "";

    public static WeaponStats[] Weapons = new WeaponStats[WeaponCount];
    public static float[] VestPrice = new float[VestCount];

    static readonly int[] pathBuf = new int[4096];

    // برای تست: جلو انداختن ساعت بازی (ثانیه). فقط تو Editor/Development استفاده میشه.
    public static long DevTimeOffset;
    public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DevTimeOffset;

    // لود DLL و چک نسخه. (پروفایل با OpenServer باز میشه)
    public static bool Init()
    {
        try
        {
            int ver = rh_api_version();
            if (ver != ApiVersion) { Error = "DLL version " + ver + " (need " + ApiVersion + ") - rebuild native/build_windows.bat"; Debug.LogError("[NativeCore] " + Error); return false; }
            if (rh_field_count() != FieldCount || rh_tech_count() != TechCount) { Error = "DLL tables mismatch - rebuild native/build_windows.bat"; Debug.LogError("[NativeCore] " + Error); return false; }
            Available = true;
            for (int i = 0; i < WeaponCount; i++)
                Weapons[i] = new WeaponStats
                {
                    damage = rh_weapon_stat(i, 0), interval = rh_weapon_stat(i, 1), mag = rh_weapon_stat(i, 2),
                    reload = rh_weapon_stat(i, 3), spread = rh_weapon_stat(i, 4), range = rh_weapon_stat(i, 5),
                    price = rh_weapon_stat(i, 6), pellets = rh_weapon_stat(i, 7), house = rh_weapon_stat(i, 8), tech = rh_weapon_stat(i, 9)
                };
            for (int i = 0; i < ItemCount; i++) { ItemValue[i] = rh_item_value(i); ItemSpecial[i] = rh_item_special(i) == 1; }
            for (int i = 0; i < VestCount; i++) { VestPrice[i] = rh_vest_stat(i, 1); }
        }
        catch (Exception e)   // DllNotFoundException / EntryPointNotFoundException
        {
            Available = false;
            Error = e.GetType().Name;
            Debug.LogError("[NativeCore] C++ library not loaded: " + e.Message);
        }
        return Available;
    }

    // ---------------------------------------------------------------- سرور و سیکل
    public static int OpenServer(string path) => rh_server_open(path, Now);              // 1 = تازه، 2 = لود شد
    public static long Peek(string path, int field) => rh_peek(path, field);              // -1 = فایل نیست
    public static void DeleteServer(string path) => rh_server_delete(path);
    public static int CycleCheck() => rh_cycle_check(Now);                                // >0 = ریست انجام شد
    public static long CycleRemaining => rh_cycle_remaining(Now);
    public static long CycleLength => rh_cycle_length();
    public static void CycleSetLength(long sec) => rh_cycle_set_length(sec);
    public static int JailPreview => rh_jail_preview();
    public static bool Surrender() { return rh_surrender(Now) == 1; }          // تسلیم زودتر به فدرال‌ها
    public static int TechMaskWith(int extraJail) { return rh_tech_mask_with(extraJail); }
    public static int JailPart(int i) => rh_jail_part(i);
    public static long Snap(int field) => rh_snap_get(field);
    public static int SnapJailPart(int i) => rh_snap_jail_part(i);
    public static int SnapNewTechMask => rh_snap_new_tech_mask();

    // ---------------------------------------------------------------- پروفایل
    public static int Profile(int field) => (int)Math.Min(int.MaxValue, rh_profile_get(field));
    public static long Profile64(int field) => rh_profile_get(field);
    public static void ProfileAdd(int field, int delta) => rh_profile_add(field, delta);
    public static void ProfileMax(int field, int v) => rh_profile_max(field, v);

    // ---------------------------------------------------------------- مسیریابی
    public static bool SetMap(string map, int cols, int rows) => rh_map_set(map, cols, rows) == 1;

    public static List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
    {
        var list = new List<Vector2Int>();
        int n = rh_find_path(from.x, from.y, to.x, to.y, pathBuf, pathBuf.Length / 2);
        for (int i = 0; i < n; i++) list.Add(new Vector2Int(pathBuf[i * 2], pathBuf[i * 2 + 1]));
        return list;
    }

    // ---------------------------------------------------------------- تک‌تری
    public static int TechInfo(int i, int field) => rh_tech_info(i, field);
    public static int TechState(int i) => rh_tech_state(i);          // 2 باز، 1 ??? بعدی، 0 قفل عمیق
    public static int TechMask => rh_tech_mask();
    public static int TechNewMask => rh_tech_new_mask();
    public static void TechMarkSeen() => rh_tech_mark_seen();
    public static int HackTimeBase => rh_bonus(3);
    public static int LootSpeedPct => rh_bonus(4);
    public static bool CanDrill => rh_bonus(5) == 1;
    public static int BagBonus => rh_bonus(6);
    public static int SellBonusPct => rh_bonus(7);
    public static int HeatDecaySeconds => rh_bonus(8);
    public static bool HasIntel => rh_bonus(9) == 1;
    public static int LawyerPct => rh_bonus(10);
    public static int CopStartReduction => rh_bonus(12);
    public static float ArmorOf(int level) => rh_vest_stat(level, 0);   // با اثر Kevlar Contract

    // ---------------------------------------------------------------- اسلحه و جلیقه
    public static bool OwnsWeapon(int i) => (rh_profile_get(WeaponMask) & (1L << i)) != 0;
    public static int WeaponState(int i) => rh_weapon_state(i);      // 0 ناشناخته، 1 لول خونه کم، 2 قابل خرید، 3 داری
    public static int VestState(int lv) => rh_vest_state(lv);
    public static int BuyWeapon(int i) => rh_buy_weapon(i);
    public static int BuyVest(int level) => rh_buy_vest(level);
    public static int VestHouse(int lv) => (int)rh_vest_stat(lv, 2);
    public static int VestTech(int lv) => (int)rh_vest_stat(lv, 3);

    // ---------------------------------------------------------------- خونه و لند
    public static int HouseCost(int level) => rh_house_info(level, 0);
    public static int HouseReqTier(int level) => rh_house_info(level, 1);
    public static int HouseReqRuns(int level) => rh_house_info(level, 2);
    public static int TierRuns(int tier) => rh_tier_runs(tier);
    public static int HouseUpgrade() => rh_house_upgrade();
    public static int LandTier(int i) => rh_land_info(i, 0);
    public static int LandHouseReq(int i) => rh_land_info(i, 1);
    public static int LandLootX100(int i) => rh_land_info(i, 2);
    public static int LandCopStart(int i) => rh_land_info(i, 3);
    public static int LandMarketLo(int i) => rh_land_info(i, 5);
    public static int LandMarketHi(int i) => rh_land_info(i, 6);
    public static bool LandUnlocked(int i) => rh_land_unlocked(i) == 1;

    // ---------------------------------------------------------------- لوت و اقتصاد
    public static int ContainerTimeX100(int kind) => rh_container_info(kind, 0);
    public static int ContainerItems(int kind) => rh_container_info(kind, 1);
    public static bool ContainerNeedsDrill(int kind) => rh_container_info(kind, 2) == 1;
    public static int LootPick(int shopType, int kind, int rnd) => rh_loot_pick(shopType, kind, rnd);
    public static int AlarmStars(int shopType, int land) => rh_alarm_stars(shopType, land);
    public static int HackTimeX100(int shopType) => rh_hack_time(shopType);
    public static int SellPrice(int value, int marketX100, int landX100) => rh_sell_price(value, marketX100, landX100);
    public static void BankRun(int cash, int land) => rh_bank_run(cash, land);
}
