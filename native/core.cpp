// RobotHeistCore v2 - هسته‌ی نیتیو بازی (C++17)
//
//  * مسیریابی BFS پلیس‌ها روی گرید هر لند
//  * سرورها: هر سرور یه فایل ذخیره‌ی جدا داره و بازیکن توش از صفر شروع میکنه
//  * سیکل ۳ روزه: بعد از هر سیکل همه‌ی پیشرفت (پول، اسلحه، جلیقه، لول خونه، لندها) ریست میشه
//    و بر اساس کارهایی که کردی «Jail Time» بهت داده میشه
//  * درخت تکنولوژی: نودهای ناشناخته (???) با Jail Time کل باز میشن (این‌ها بعد از ریست میمونن)
//  * لول خونه: اسلحه/جلیقه/لندها فقط با لول خونه‌ی کافی خریدنی/قابل ورود هستن
//  * لوت: هر ظرف (قفسه، کابینت، صندوق، ویترین، گاوصندوق) جدول جایزه‌ی خودش رو داره
#include <algorithm>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <queue>
#include <string>
#include <vector>

#ifdef _WIN32
#define RH_API extern "C" __declspec(dllexport)
#else
#define RH_API extern "C" __attribute__((visibility("default")))
#endif

typedef long long i64;

namespace {

// ============================================================ گرید مسیریابی
int g_cols = 0, g_rows = 0;
std::vector<char> g_wall;

bool walkable(int x, int y) {
    return x >= 0 && y >= 0 && x < g_cols && y < g_rows && !g_wall[y * g_cols + x];
}

// ============================================================ پروفایل
constexpr int kLandCount = 6;
constexpr int kTechCount = 25;
constexpr int kJailParts = 10;

// ترتیب باید دقیقاً با NativeCore.cs یکی باشه
enum Field : int {
    // ---- مخصوص هر سیکل (با ریست پاک میشن) ----
    F_MONEY = 0, F_WEAPONS, F_VEST, F_HOUSE,
    F_RUNS, F_EARNED, F_KILLS, F_BEST_RUN,
    F_SHOPS, F_LOOTED, F_REGISTERS, F_ALARMS, F_DISARMED, F_MAX_WANTED, F_BUSTED, F_LOOT_VALUE,
    F_LAND0,                                   // 16..21 تعداد خروج موفق از هر لند
    F_CYCLE_END = F_LAND0 + kLandCount,        // 22
    // ---- دائمی (بعد از ریست میمونن) ----
    F_JAIL = F_CYCLE_END, F_CYCLE_NO, F_CYCLE_START, F_TECH_SEEN, F_LAST_JAIL,
    F_LIFE_KILLS, F_LIFE_LOOTED, F_LIFE_EARNED, F_CREATED,
    F_COUNT
};

const char* const kFieldNames[F_COUNT] = {
    "money", "weapons", "vest", "house",
    "runs", "earned", "kills", "best_run",
    "shops", "looted", "registers", "alarms", "disarmed", "max_wanted", "busted", "loot_value",
    "land0", "land1", "land2", "land3", "land4", "land5",
    "jail", "cycle_no", "cycle_start", "tech_seen", "last_jail",
    "life_kills", "life_looted", "life_earned", "created"};
static_assert(sizeof(kFieldNames) / sizeof(kFieldNames[0]) == F_COUNT, "field names mismatch");

i64 g_p[F_COUNT];
std::string g_savePath;
i64 g_cycleLen = 3LL * 24 * 3600;  // ۳ روز

void setCycleDefaults(i64* p) {
    for (int i = 0; i < F_CYCLE_END; i++) p[i] = 0;
    p[F_WEAPONS] = 1;  // فقط تپانچه
    p[F_HOUSE] = 1;
}

void setAllDefaults() {
    for (int i = 0; i < F_COUNT; i++) g_p[i] = 0;
    setCycleDefaults(g_p);
    g_p[F_CYCLE_NO] = 1;
}

bool writeProfile(const std::string& path, const i64* p) {
    if (path.empty()) return false;
    std::ofstream out(path, std::ios::trunc);
    if (!out) return false;
    out << "version 2\n";
    for (int i = 0; i < F_COUNT; i++) out << kFieldNames[i] << ' ' << p[i] << '\n';
    return (bool)out;
}

void save() { writeProfile(g_savePath, g_p); }

// برمی‌گردونه false اگه فایل نیست یا نسخه‌ش قدیمیه
bool readProfile(const std::string& path, i64* p) {
    std::ifstream in(path);
    if (!in) return false;
    std::string key;
    i64 val;
    int version = 0;
    while (in >> key >> val) {
        if (key == "version") { version = (int)val; continue; }
        for (int i = 0; i < F_COUNT; i++)
            if (key == kFieldNames[i]) { p[i] = std::max<i64>(0, val); break; }
    }
    return version == 2;
}

// ============================================================ درخت تکنولوژی
enum Branch { B_WEAPON = 0, B_ARMOR, B_TOOLS, B_ECON, B_LEGAL };
enum Kind { K_WEAPON = 1, K_VEST, K_HACK, K_SPEED, K_DRILL, K_BAG, K_SELL, K_HEAT, K_INTEL,
            K_LAWYER = 10,    // وقتی دستگیر میشی ٪ پول ذخیره‌نشده‌ی این دور رو نگه میداری
            K_WIRE,           // ستاره‌ی آلارم ۱ کمتر (حداقل ۱)
            K_SCANNER,        // پلیس اولیه‌ی لند ۱ کمتر
            K_STARTCASH,      // پول شروع هر سیکل
            K_STASH,          // ٪ پولی که از ریست جون سالم به در میبره
            K_STARTVEST,      // جلیقه‌ی شروع هر سیکل
            K_ARMORUP };      // ٪ زره‌ی بیشتر

struct Tech { int branch, row, jail, parent, kind, value; };
const Tech kTech[kTechCount] = {
    //            branch   row jail parent kind      value
    /* 0  Pistol          */ {B_WEAPON, 0, 0, -1, K_WEAPON, 0},
    /* 1  SMG             */ {B_WEAPON, 1, 0, 0, K_WEAPON, 1},
    /* 2  Shotgun         */ {B_WEAPON, 2, 20, 1, K_WEAPON, 2},
    /* 3  Assault Rifle   */ {B_WEAPON, 3, 60, 2, K_WEAPON, 3},
    /* 4  LMG             */ {B_WEAPON, 4, 150, 3, K_WEAPON, 4},
    /* 5  Light Vest      */ {B_ARMOR, 0, 0, -1, K_VEST, 1},
    /* 6  Medium Vest     */ {B_ARMOR, 1, 15, 5, K_VEST, 2},
    /* 7  Heavy Vest      */ {B_ARMOR, 2, 90, 6, K_VEST, 3},
    /* 8  Alarm Jammer    */ {B_TOOLS, 0, 0, -1, K_HACK, 500},    // زمان هک آلارم (صدم ثانیه)
    /* 9  Jammer Mk II    */ {B_TOOLS, 1, 25, 8, K_HACK, 300},
    /* 10 Fast Hands      */ {B_TOOLS, 2, 40, 9, K_SPEED, 75},    // زمان جستجو ×0.75
    /* 11 Thermal Drill   */ {B_TOOLS, 3, 100, 10, K_DRILL, 1},
    /* 12 Insider Intel   */ {B_TOOLS, 4, 180, 11, K_INTEL, 1},
    /* 13 Bigger Bag      */ {B_ECON, 0, 30, -1, K_BAG, 2},
    /* 14 Fence Contacts  */ {B_ECON, 1, 50, 13, K_SELL, 10},     // +۱۰٪ قیمت فروش
    /* 15 Heat Sink       */ {B_ECON, 2, 80, 14, K_HEAT, 8},      // کم شدن ستاره هر ۸ ثانیه
    /* 16 Bag Mk II       */ {B_ECON, 3, 110, 15, K_BAG, 2},
    /* 17 Cartel Fence    */ {B_ECON, 4, 200, 16, K_SELL, 25},
    /* 18 Public Defender */ {B_LEGAL, 0, 10, -1, K_LAWYER, 30},   // دستگیر شدی: ۳۰٪ پول این دور برات میمونه
    /* 19 Wire Cutters    */ {B_LEGAL, 1, 45, 18, K_WIRE, 1},
    /* 20 Fed Informant   */ {B_LEGAL, 2, 75, 19, K_SCANNER, 1},
    /* 21 Parole Cash     */ {B_LEGAL, 3, 120, 20, K_STARTCASH, 1500},
    /* 22 Pardon          */ {B_LEGAL, 4, 260, 21, K_STASH, 25},
    /* 23 Care Package    */ {B_ARMOR, 3, 130, 7, K_STARTVEST, 2},
    /* 24 Kevlar Contract */ {B_ARMOR, 4, 240, 23, K_ARMORUP, 25},
};

bool techUnlocked(int i, int depth = 0) {
    if (i < 0 || i >= kTechCount || depth > kTechCount) return false;
    const Tech& t = kTech[i];
    if (g_p[F_JAIL] < t.jail) return false;
    return t.parent < 0 || techUnlocked(t.parent, depth + 1);
}

int techMask() {
    int m = 0;
    for (int i = 0; i < kTechCount; i++)
        if (techUnlocked(i)) m |= (1 << i);
    return m;
}

// ============================================================ اسلحه، جلیقه، خونه، لند
struct Weapon { float damage, interval, mag, reload, spread, range, price, pellets, house, tech; };
const int kWeaponCount = 5;
const Weapon kWeapons[kWeaponCount] = {
    //  dmg   interval mag   reload spread range price   pellets house tech
    {28.f, 0.28f, 15.f, 1.0f, 1.0f, 75.f, 0.f, 1.f, 1.f, 0.f},        // 0 Pistol
    {14.f, 0.075f, 35.f, 1.3f, 3.0f, 60.f, 1500.f, 1.f, 1.f, 1.f},    // 1 SMG
    {11.f, 0.80f, 8.f, 1.8f, 6.0f, 40.f, 3000.f, 8.f, 2.f, 2.f},      // 2 Shotgun
    {24.f, 0.11f, 30.f, 1.7f, 1.5f, 100.f, 5500.f, 1.f, 2.f, 3.f},    // 3 Assault Rifle
    {28.f, 0.07f, 100.f, 3.0f, 2.5f, 110.f, 12000.f, 1.f, 3.f, 4.f}   // 4 LMG
};

struct Vest { float armor, price; int house, tech; };
const int kVestCount = 4;
const Vest kVests[kVestCount] = {{0.f, 0.f, 1, -1}, {60.f, 800.f, 1, 5}, {110.f, 2500.f, 2, 6}, {170.f, 6000.f, 3, 7}};

// ارتقای خونه: سطح ۲ و ۳ (هزینه، تیر لند لازم، تعداد خروج موفق از اون تیر)
struct HouseUp { int cost, reqTier, reqRuns; };
const HouseUp kHouse[4] = {{0, 0, 0}, {0, 0, 0}, {8000, 1, 3}, {26000, 2, 3}};
const int kHouseMax = 3;

struct Land { int tier, houseReq, lootX100, copStart, alarmBonus, marketLoX100, marketHiX100; };
const Land kLands[kLandCount] = {
    {1, 1, 100, 3, 0, 100, 150},  // 0 Maple Heights
    {1, 1, 105, 3, 0, 100, 150},  // 1 Harbor Strip
    {1, 1, 115, 4, 0, 100, 155},  // 2 Old Town
    {2, 2, 150, 5, 1, 110, 170},  // 3 Midtown
    {2, 2, 160, 5, 1, 110, 170},  // 4 Industrial Zone
    {3, 3, 230, 6, 1, 120, 190},  // 5 Financial Core
};

// ============================================================ آیتم‌ها و ظرف‌ها
struct Item { int value; int shop; int special; };
const int kItemCount = 19;
const Item kItems[kItemCount] = {
    {220, 0, 0}, {180, 0, 0}, {420, 0, 0}, {1350, 0, 1},                  // 0-3   Cash Roll, Cigarettes, Whiskey, [Vintage Wine]
    {670, 1, 0}, {1200, 1, 0}, {900, 1, 0}, {1050, 1, 0}, {3300, 1, 1},    // 4-8   Phone, Laptop, Console, Camera, [Prototype Chip]
    {1200, 2, 0}, {1800, 2, 0}, {2400, 2, 0}, {1650, 2, 0}, {6750, 2, 1}, // 9-13  Necklace, Watch, Ring, Pearls, [Royal Ruby]
    {3000, 3, 0}, {4500, 3, 0}, {5250, 3, 0}, {6300, 3, 0}, {12000, 3, 1} // 14-18 Cash Bundle, Gold Bar, Bonds, Deposit Box, [Cartel Ledger]
};
const int kRegisterItem[4] = {0, 0, 0, 14};  // پول نقد صندوق: Cash Roll یا Cash Bundle (بانک)

// نوع ظرف: 0 قفسه، 1 کابینت، 2 صندوق‌فروش، 3 ویترین، 4 گاوصندوق
struct Container { int timeX100, items, needsDrill, rarePct; int w[4]; };
const Container kContainers[5] = {
    {120, 1, 0, 2, {50, 30, 15, 5}},    // Shelf
    {180, 1, 0, 6, {20, 35, 30, 15}},   // Cabinet
    {250, 1, 0, 0, {0, 0, 0, 0}},       // Register (آیتم ثابت)
    {220, 1, 0, 10, {5, 20, 35, 40}},   // Display
    {600, 2, 1, 55, {0, 10, 30, 60}},   // Safe (متّه‌ی حرارتی لازم)
};
const int kShopAlarmStars[4] = {1, 1, 2, 2};
const int kShopHackPct[4] = {80, 100, 130, 170};

// وزن جریمه‌ی هر کار (به دقیقه Jail Time)
i64 jailPart(int i, const i64* p) {
    switch (i) {
        case 0: return p[F_SHOPS] * 2;                         // ورود به مغازه
        case 1: return p[F_LOOTED] * 1;                        // ظرف‌های خالی‌شده
        case 2: return p[F_REGISTERS] * 2;                     // خالی کردن صندوق (اضافه بر بالا)
        case 3: return p[F_ALARMS] * 3;                        // آلارم فعال‌شده
        case 4: return p[F_DISARMED] * 1;                      // آلارم غیرفعال‌شده (حرفه‌ای)
        case 5: return p[F_KILLS] * 4;                         // پلیس کشته‌شده
        case 6: return std::max<i64>(0, p[F_MAX_WANTED] - 1) * 5;  // بالاترین ستاره
        case 7: return p[F_BUSTED] * 10;                       // دستگیر شدن
        case 8: return p[F_RUNS] * 3;                          // خروج موفق از شهر
        case 9: return p[F_LOOT_VALUE] / 1000;                 // هر ۱۰۰۰$ لوت
    }
    return 0;
}

i64 jailPreview(const i64* p) {
    i64 s = 0;
    for (int i = 0; i < kJailParts; i++) s += jailPart(i, p);
    return s;
}

// عکس لحظه‌ی آخرین ریست (برای صفحه‌ی خلاصه)
i64 g_snap[F_CYCLE_END];
i64 g_snapJail[kJailParts];
int g_snapNewTech = 0;

int bonusMin(int kind, int base) {
    int v = base;
    for (int i = 0; i < kTechCount; i++)
        if (kTech[i].kind == kind && techUnlocked(i)) v = std::min(v, kTech[i].value);
    return v;
}
int bonusSum(int kind) {
    int v = 0;
    for (int i = 0; i < kTechCount; i++)
        if (kTech[i].kind == kind && techUnlocked(i)) v += kTech[i].value;
    return v;
}

}  // namespace

// ============================================================ API: عمومی
RH_API int rh_api_version() { return 2; }
RH_API int rh_field_count() { return F_COUNT; }

// گرید هر لند (بدون دست زدن به پروفایل)
RH_API int rh_map_set(const char* map, int cols, int rows) {
    if (!map || cols <= 0 || rows <= 0 || (int)std::strlen(map) < cols * rows) return 0;
    g_cols = cols;
    g_rows = rows;
    g_wall.assign(cols * rows, 0);
    for (int i = 0; i < cols * rows; i++) g_wall[i] = (map[i] == '#');
    return 1;
}

// جفت‌های x,y (بدون سلول شروع). برمی‌گردونه تعداد جفت‌ها؛ ۰ یعنی مسیری نیست
RH_API int rh_find_path(int fx, int fy, int tx, int ty, int* out_xy, int max_pairs) {
    if (!out_xy || max_pairs <= 0) return 0;
    if (!walkable(fx, fy) || !walkable(tx, ty) || (fx == tx && fy == ty)) return 0;

    const int N = g_cols * g_rows;
    std::vector<int> prev(N, -1);
    std::vector<char> seen(N, 0);
    std::queue<int> q;
    int start = fy * g_cols + fx, goal = ty * g_cols + tx;
    q.push(start);
    seen[start] = 1;
    static const int dx[4] = {0, 0, -1, 1}, dy[4] = {-1, 1, 0, 0};

    while (!q.empty()) {
        int c = q.front();
        q.pop();
        if (c == goal) break;
        int cx = c % g_cols, cy = c / g_cols;
        for (int d = 0; d < 4; d++) {
            int nx = cx + dx[d], ny = cy + dy[d];
            if (!walkable(nx, ny)) continue;
            int n = ny * g_cols + nx;
            if (seen[n]) continue;
            seen[n] = 1;
            prev[n] = c;
            q.push(n);
        }
    }
    if (!seen[goal]) return 0;

    std::vector<int> rev;
    for (int c = goal; c != start; c = prev[c]) rev.push_back(c);
    std::reverse(rev.begin(), rev.end());
    int n = std::min<int>((int)rev.size(), max_pairs);
    for (int i = 0; i < n; i++) {
        out_xy[i * 2] = rev[i] % g_cols;
        out_xy[i * 2 + 1] = rev[i] / g_cols;
    }
    return n;
}

// ============================================================ API: سرور / پروفایل
// سرور = یه فایل ذخیره. اگه فایل نبود (یا قدیمی بود) سرور تازه از صفر ساخته میشه.
// خروجی: 1 = سرور تازه ساخته شد، 2 = سرور قبلی لود شد، 0 = خطا
RH_API int rh_server_open(const char* path, i64 now) {
    if (!path || !*path) return 0;
    g_savePath = path;
    setAllDefaults();
    bool loaded = readProfile(g_savePath, g_p);
    if (!loaded) {
        setAllDefaults();
        g_p[F_CYCLE_START] = now;
        g_p[F_CREATED] = now;
        g_p[F_TECH_SEEN] = techMask();   // نودهای شروع «جدید» حساب نشن
        save();
        return 1;
    }
    if (g_p[F_CYCLE_NO] < 1) g_p[F_CYCLE_NO] = 1;
    if (g_p[F_HOUSE] < 1) g_p[F_HOUSE] = 1;
    g_p[F_WEAPONS] |= 1;
    return 2;
}

// خوندن یه فیلد از فایل یه سرور بدون باز کردنش؛ -1 = فایل نیست
RH_API i64 rh_peek(const char* path, int field) {
    if (!path || field < 0 || field >= F_COUNT) return -1;
    i64 tmp[F_COUNT];
    for (int i = 0; i < F_COUNT; i++) tmp[i] = 0;
    setCycleDefaults(tmp);
    tmp[F_CYCLE_NO] = 1;
    if (!readProfile(path, tmp)) return -1;
    return tmp[field];
}

RH_API int rh_server_delete(const char* path) {
    if (!path) return 0;
    return std::remove(path) == 0 ? 1 : 0;
}

RH_API i64 rh_profile_get(int field) { return (field >= 0 && field < F_COUNT) ? g_p[field] : 0; }

RH_API void rh_profile_add(int field, i64 delta) {
    if (field < 0 || field >= F_COUNT) return;
    g_p[field] = std::max<i64>(0, g_p[field] + delta);
    save();
}

RH_API void rh_profile_max(int field, i64 v) {
    if (field < 0 || field >= F_COUNT) return;
    if (v > g_p[field]) { g_p[field] = v; save(); }
}

// ============================================================ API: سیکل ۳ روزه
RH_API void rh_cycle_set_length(i64 sec) { g_cycleLen = std::max<i64>(60, sec); }
RH_API i64 rh_cycle_length() { return g_cycleLen; }

RH_API i64 rh_cycle_remaining(i64 now) {
    i64 left = g_p[F_CYCLE_START] + g_cycleLen - now;
    return std::max<i64>(0, left);
}

// jail time تخمینی سیکل فعلی
RH_API int rh_jail_preview() { return (int)jailPreview(g_p); }
RH_API int rh_jail_part(int i) { return (i >= 0 && i < kJailParts) ? (int)jailPart(i, g_p) : 0; }
RH_API int rh_jail_part_count() { return kJailParts; }

// اگه سیکل تموم شده باشه ریست میکنه. خروجی = تعداد سیکل‌های گذشته (۰ = هیچ اتفاقی نیفتاد)
// ریست واقعی سیکل: jail time میگیره، همه‌ی پیشرفت پاک میشه، امتیازهای ماندگار درخت اعمال میشن.
// cycles = چند سیکل بهش اضافه بشه، newStart = شروع سیکل تازه
static void doWipe(i64 cycles, i64 newStart) {
    // عکس قبل از ریست (برای صفحه‌ی خلاصه / دادگاه)
    for (int i = 0; i < F_CYCLE_END; i++) g_snap[i] = g_p[i];
    for (int i = 0; i < kJailParts; i++) g_snapJail[i] = jailPart(i, g_p);
    int before = techMask();

    i64 jail = jailPreview(g_p);
    g_p[F_LIFE_KILLS] += g_p[F_KILLS];
    g_p[F_LIFE_LOOTED] += g_p[F_LOOTED];
    g_p[F_LIFE_EARNED] += g_p[F_EARNED];
    g_p[F_JAIL] += jail;
    g_p[F_LAST_JAIL] = jail;
    g_p[F_CYCLE_NO] += cycles;
    g_p[F_CYCLE_START] = newStart;
    i64 oldMoney = g_p[F_MONEY];
    setCycleDefaults(g_p);
    // امتیازهای درخت (با jail time جدید حساب میشن): نگه‌داشتن بخشی از پول، پول و جلیقه‌ی شروع
    g_p[F_MONEY] = oldMoney * bonusSum(K_STASH) / 100 + bonusSum(K_STARTCASH);
    int sv = bonusSum(K_STARTVEST);
    if (sv > 0) g_p[F_VEST] = std::min(sv, kVestCount - 1);

    g_snapNewTech = techMask() & ~before;
    save();
}

// اگه سیکل تموم شده باشه ریست میکنه. خروجی = تعداد سیکل‌های گذشته (۰ = هیچ اتفاقی نیفتاد)
RH_API int rh_cycle_check(i64 now) {
    if (g_p[F_CYCLE_START] <= 0) { g_p[F_CYCLE_START] = now; save(); return 0; }
    if (now < g_p[F_CYCLE_START]) { g_p[F_CYCLE_START] = now; save(); return 0; }  // ساعت عقب رفته
    i64 elapsed = now - g_p[F_CYCLE_START];
    if (elapsed < g_cycleLen) return 0;
    i64 n = elapsed / g_cycleLen;
    doWipe(n, g_p[F_CYCLE_START] + n * g_cycleLen);
    return (int)n;
}

// تسلیم شدن زودتر: همین الان خودتو به فدرال‌ها معرفی میکنی. دادگاه همون لحظه برگزار میشه و یه سیکل ۷۲ ساعته‌ی تازه از همین لحظه شروع میشه.
// 1 = انجام شد، 0 = هنوز کاری نکردی که بشه اعتراف کرد (jail time = ۰)
RH_API int rh_surrender(i64 now) {
    if (jailPreview(g_p) <= 0) return 0;
    doWipe(1, now);
    return 1;
}

// چند نود تک‌تری با jail time فعلی + extra دقیقه باز میشه؟ (فقط پیش‌نمایش؛ چیزی تغییر نمیکنه)
RH_API int rh_tech_mask_with(int extra) {
    i64 saved = g_p[F_JAIL];
    g_p[F_JAIL] += std::max(0, extra);
    int m = techMask();
    g_p[F_JAIL] = saved;
    return m;
}

RH_API i64 rh_snap_get(int field) { return (field >= 0 && field < F_CYCLE_END) ? g_snap[field] : 0; }
RH_API int rh_snap_jail_part(int i) { return (i >= 0 && i < kJailParts) ? (int)g_snapJail[i] : 0; }
RH_API int rh_snap_new_tech_mask() { return g_snapNewTech; }

// ============================================================ API: درخت تکنولوژی
RH_API int rh_tech_count() { return kTechCount; }

// field: 0 branch, 1 row, 2 jail لازم، 3 parent، 4 kind، 5 value
RH_API int rh_tech_info(int i, int field) {
    if (i < 0 || i >= kTechCount) return -1;
    const Tech& t = kTech[i];
    const int v[6] = {t.branch, t.row, t.jail, t.parent, t.kind, t.value};
    return (field >= 0 && field < 6) ? v[field] : -1;
}

// 2 = باز، 1 = ناشناخته ولی بعدیه (پدرش بازه)، 0 = هنوز عمیق‌تر / قفل
RH_API int rh_tech_state(int i) {
    if (i < 0 || i >= kTechCount) return 0;
    if (techUnlocked(i)) return 2;
    int p = kTech[i].parent;
    return (p < 0 || techUnlocked(p)) ? 1 : 0;
}

RH_API int rh_tech_mask() { return techMask(); }
RH_API int rh_tech_new_mask() { return techMask() & ~(int)g_p[F_TECH_SEEN]; }
RH_API void rh_tech_mark_seen() { g_p[F_TECH_SEEN] = techMask(); save(); }

// اثر نودهای باز. kind: 3 hack(×۰.۰۱ s)، 4 loot speed(%)، 5 drill، 6 bag، 7 sell %، 8 heat decay (s)، 9 intel،
// 10 وکیل(%)، 11 سیم‌چین، 12 خبرچین، 13 پول شروع، 14 نگه‌داشتن پول(%)، 15 جلیقه‌ی شروع، 16 زره‌ی بیشتر(%)
RH_API int rh_bonus(int kind) {
    switch (kind) {
        case K_HACK: { int v = bonusMin(K_HACK, 99999); return v == 99999 ? 700 : v; }
        case K_SPEED: return bonusMin(K_SPEED, 100);
        case K_DRILL: return bonusSum(K_DRILL) > 0 ? 1 : 0;
        case K_BAG: return bonusSum(K_BAG);
        case K_SELL: return bonusSum(K_SELL);
        case K_HEAT: return bonusMin(K_HEAT, 12);
        case K_INTEL: return bonusSum(K_INTEL) > 0 ? 1 : 0;
        case K_LAWYER: case K_WIRE: case K_SCANNER: case K_STARTCASH: case K_STASH: case K_STARTVEST: case K_ARMORUP:
            return bonusSum(kind);
    }
    return 0;
}

// ============================================================ API: اسلحه و جلیقه
RH_API int rh_weapon_count() { return kWeaponCount; }

// stat: 0 damage, 1 interval, 2 mag, 3 reload, 4 spread, 5 range, 6 price, 7 pellets, 8 house, 9 tech
RH_API float rh_weapon_stat(int idx, int stat) {
    if (idx < 0 || idx >= kWeaponCount) return 0.f;
    const Weapon& w = kWeapons[idx];
    const float v[10] = {w.damage, w.interval, w.mag, w.reload, w.spread, w.range, w.price, w.pellets, w.house, w.tech};
    return (stat >= 0 && stat < 10) ? v[stat] : 0.f;
}

// stat: 0 armor, 1 price, 2 house, 3 tech
RH_API float rh_vest_stat(int level, int stat) {
    if (level < 0 || level >= kVestCount) return 0.f;
    const Vest& v = kVests[level];
    const float s[4] = {v.armor * (100 + bonusSum(K_ARMORUP)) / 100.f, v.price, (float)v.house, (float)v.tech};
    return (stat >= 0 && stat < 4) ? s[stat] : 0.f;
}

// وضعیت: 0 = ناشناخته (تک‌تری)، 1 = لول خونه کم، 2 = قابل خرید، 3 = داری
RH_API int rh_weapon_state(int idx) {
    if (idx < 0 || idx >= kWeaponCount) return 0;
    if (g_p[F_WEAPONS] & (1LL << idx)) return 3;
    if (!techUnlocked((int)kWeapons[idx].tech)) return 0;
    if (g_p[F_HOUSE] < (i64)kWeapons[idx].house) return 1;
    return 2;
}

RH_API int rh_vest_state(int level) {
    if (level < 0 || level >= kVestCount) return 0;
    if (g_p[F_VEST] >= level) return 3;
    if (kVests[level].tech >= 0 && !techUnlocked(kVests[level].tech)) return 0;
    if (g_p[F_HOUSE] < (i64)kVests[level].house) return 1;
    return 2;
}

// 1 = خرید موفق، 0 = پول نیست، -1 = داری/نامعتبر، -2 = ناشناخته، -3 = لول خونه کم
RH_API int rh_buy_weapon(int idx) {
    if (idx < 0 || idx >= kWeaponCount) return -1;
    int st = rh_weapon_state(idx);
    if (st == 3) return -1;
    if (st == 0) return -2;
    if (st == 1) return -3;
    i64 price = (i64)kWeapons[idx].price;
    if (g_p[F_MONEY] < price) return 0;
    g_p[F_MONEY] -= price;
    g_p[F_WEAPONS] |= (1LL << idx);
    save();
    return 1;
}

RH_API int rh_buy_vest(int level) {
    if (level < 1 || level >= kVestCount) return -1;
    int st = rh_vest_state(level);
    if (st == 3) return -1;
    if (st == 0) return -2;
    if (st == 1) return -3;
    i64 price = (i64)kVests[level].price;
    if (g_p[F_MONEY] < price) return 0;
    g_p[F_MONEY] -= price;
    g_p[F_VEST] = level;
    save();
    return 1;
}

// ============================================================ API: خونه
RH_API int rh_house_max() { return kHouseMax; }

// field: 0 هزینه، 1 تیر لازم، 2 تعداد خروج موفق لازم  (level = سطح مقصد: 2 یا 3)
RH_API int rh_house_info(int level, int field) {
    if (level < 2 || level > kHouseMax) return -1;
    const HouseUp& h = kHouse[level];
    const int v[3] = {h.cost, h.reqTier, h.reqRuns};
    return (field >= 0 && field < 3) ? v[field] : -1;
}

// خروج‌های موفق از لندهای یه تیر در این سیکل
RH_API int rh_tier_runs(int tier) {
    int s = 0;
    for (int i = 0; i < kLandCount; i++)
        if (kLands[i].tier == tier) s += (int)g_p[F_LAND0 + i];
    return s;
}

// 1 = ارتقا انجام شد، 0 = پول نیست، -1 = بیشترین سطح، -2 = شرط خروج‌های موفق برقرار نیست
RH_API int rh_house_upgrade() {
    int next = (int)g_p[F_HOUSE] + 1;
    if (next > kHouseMax) return -1;
    const HouseUp& h = kHouse[next];
    if (rh_tier_runs(h.reqTier) < h.reqRuns) return -2;
    if (g_p[F_MONEY] < h.cost) return 0;
    g_p[F_MONEY] -= h.cost;
    g_p[F_HOUSE] = next;
    save();
    return 1;
}

// ============================================================ API: لندها
RH_API int rh_land_count() { return kLandCount; }

// field: 0 tier، 1 لول خونه لازم، 2 ضریب ارزش لوت (×۱۰۰)، 3 پلیس اولیه، 4 آلارم اضافه، 5 بازار min، 6 بازار max
RH_API int rh_land_info(int i, int field) {
    if (i < 0 || i >= kLandCount) return -1;
    const Land& l = kLands[i];
    const int v[7] = {l.tier, l.houseReq, l.lootX100, l.copStart, l.alarmBonus, l.marketLoX100, l.marketHiX100};
    return (field >= 0 && field < 7) ? v[field] : -1;
}

RH_API int rh_land_unlocked(int i) {
    return (i >= 0 && i < kLandCount && g_p[F_HOUSE] >= kLands[i].houseReq) ? 1 : 0;
}

// ============================================================ API: آیتم‌ها و ظرف‌ها
RH_API int rh_item_count() { return kItemCount; }
RH_API int rh_item_value(int id) { return (id >= 0 && id < kItemCount) ? kItems[id].value : 0; }
RH_API int rh_item_special(int id) { return (id >= 0 && id < kItemCount) ? kItems[id].special : 0; }

// field: 0 زمان جستجو (×۱۰۰ ثانیه، با اثر Fast Hands)، 1 تعداد آیتم، 2 متّه لازمه؟، 3 شانس کمیاب %
RH_API int rh_container_info(int kind, int field) {
    if (kind < 0 || kind > 4) return -1;
    const Container& c = kContainers[kind];
    switch (field) {
        case 0: return c.timeX100 * rh_bonus(K_SPEED) / 100;
        case 1: return c.items;
        case 2: return c.needsDrill;
        case 3: return c.rarePct;
    }
    return -1;
}

// آیتم یه ظرف؛ rnd: 0..9999. -1 = ورودی نامعتبر
RH_API int rh_loot_pick(int shop_type, int container_kind, int rnd) {
    if (shop_type < 0 || shop_type > 3 || container_kind < 0 || container_kind > 4) return -1;
    rnd = std::max(0, std::min(9999, rnd));
    if (container_kind == 2) return kRegisterItem[shop_type];

    std::vector<int> regular;
    int special = -1;
    for (int i = 0; i < kItemCount; i++) {
        if (kItems[i].shop != shop_type) continue;
        if (kItems[i].special) special = i; else regular.push_back(i);
    }
    std::sort(regular.begin(), regular.end(), [](int a, int b) { return kItems[a].value < kItems[b].value; });

    const Container& c = kContainers[container_kind];
    int rareRoll = rnd / 100;   // 0..99
    int tierRoll = rnd % 100;   // 0..99
    if (special >= 0 && rareRoll < c.rarePct) return special;
    int acc = 0, total = c.w[0] + c.w[1] + c.w[2] + c.w[3];
    if (total <= 0 || regular.empty()) return special >= 0 ? special : -1;
    int pick = tierRoll * total / 100;
    for (int k = 0; k < 4 && k < (int)regular.size(); k++) {
        acc += c.w[k];
        if (pick < acc) return regular[k];
    }
    return regular.back();
}

// ستاره‌ی تحت‌تعقیب وقتی آلارم مغازه به صدا در میاد
RH_API int rh_alarm_stars(int shop_type, int land) {
    if (shop_type < 0 || shop_type > 3) return 1;
    int bonus = (land >= 0 && land < kLandCount) ? kLands[land].alarmBonus : 0;
    return std::max(1, std::min(4, kShopAlarmStars[shop_type] + bonus - bonusSum(K_WIRE)));
}

// زمان هک آلارم (×۱۰۰ ثانیه)
RH_API int rh_hack_time(int shop_type) {
    if (shop_type < 0 || shop_type > 3) return 500;
    return rh_bonus(K_HACK) * kShopHackPct[shop_type] / 100;
}

// ============================================================ API: اقتصاد
// قیمت فروش در بلک‌مارکت: value × بازار × لند × (۱ + جایزه‌ی Fence)
RH_API int rh_sell_price(int value, int market_x100, int land_x100) {
    i64 v = std::max(0, value);
    v = v * std::max(0, market_x100) / 100;
    v = v * std::max(0, land_x100) / 100;
    v = v * (100 + rh_bonus(K_SELL)) / 100;
    return (int)std::min<i64>(v, 2000000000LL);
}

// وقتی بازیکن سالم به بیس برسه، پول این دور تو حسابش ذخیره میشه
RH_API void rh_bank_run(int cash, int land) {
    cash = std::max(0, cash);
    g_p[F_MONEY] += cash;
    g_p[F_EARNED] += cash;
    g_p[F_RUNS] += 1;
    g_p[F_BEST_RUN] = std::max<i64>(g_p[F_BEST_RUN], cash);
    if (land >= 0 && land < kLandCount) g_p[F_LAND0 + land] += 1;
    save();
}
