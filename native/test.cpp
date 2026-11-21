#include <cassert>
#include <cstdio>
typedef long long i64;
extern "C" {
int rh_api_version(); int rh_field_count();
int rh_map_set(const char*, int, int);
int rh_find_path(int, int, int, int, int*, int);
int rh_server_open(const char*, i64); i64 rh_peek(const char*, int); int rh_server_delete(const char*);
i64 rh_profile_get(int); void rh_profile_add(int, i64); void rh_profile_max(int, i64);
void rh_cycle_set_length(i64); i64 rh_cycle_remaining(i64); int rh_cycle_check(i64); int rh_surrender(i64); int rh_tech_mask_with(int);
int rh_jail_preview(); int rh_jail_part(int); i64 rh_snap_get(int); int rh_snap_new_tech_mask();
int rh_tech_count(); int rh_tech_info(int,int); int rh_tech_state(int); int rh_tech_mask(); int rh_tech_new_mask(); void rh_tech_mark_seen();
int rh_bonus(int);
int rh_weapon_state(int); int rh_vest_state(int); int rh_buy_weapon(int); int rh_buy_vest(int);
float rh_weapon_stat(int,int); float rh_vest_stat(int,int);
int rh_house_upgrade(); int rh_tier_runs(int); int rh_land_unlocked(int); int rh_land_info(int,int);
int rh_container_info(int,int); int rh_loot_pick(int,int,int); int rh_item_value(int); int rh_item_special(int);
int rh_alarm_stars(int,int); int rh_hack_time(int); int rh_sell_price(int,int,int); void rh_bank_run(int,int);
}
enum { MONEY=0, WEAPONS=1, VEST=2, HOUSE=3, RUNS=4, KILLS=6, SHOPS=8, LOOTED=9, ALARMS=11, MAXW=13, BUSTED=14, LOOTV=15, JAIL=22, CYCLE_NO=23, CYCLE_START=24, LASTJAIL=26 };
int main() {
    assert(rh_api_version() == 2 && rh_field_count() == 31 && rh_tech_count() == 25);
    const char* map = "#####" "#..##" "#.#.#" "#...#" "#####";
    assert(rh_map_set(map, 5, 5) == 1);
    int out[64];
    assert(rh_find_path(1, 1, 3, 3, out, 32) == 4);
    assert(rh_find_path(1, 1, 0, 0, out, 32) == 0);

    const char* save = "/tmp/rh_srv_test.sav";
    rh_server_delete(save);
    assert(rh_peek(save, 0) == -1);
    const i64 T0 = 1000000;
    assert(rh_server_open(save, T0) == 1);                      // سرور تازه
    assert(rh_profile_get(CYCLE_NO) == 1 && rh_profile_get(HOUSE) == 1 && rh_profile_get(JAIL) == 0);
    assert(rh_profile_get(CYCLE_START) == T0);
    assert(rh_cycle_remaining(T0) == 3 * 86400);

    // تک‌تری شروع: فقط Pistol, SMG, Light Vest, Alarm Jammer باز
    int m = rh_tech_mask();
    assert((m & 1) && (m & 2) && (m & (1 << 5)) && (m & (1 << 8)));
    assert(!(m & (1 << 2)) && !(m & (1 << 13)));
    assert(rh_tech_new_mask() == 0);
    assert(rh_tech_state(2) == 1 && rh_tech_state(3) == 0 && rh_tech_state(4) == 0);   // Shotgun بعدیه، AR/LMG عمیق
    assert(rh_bonus(3) == 500 && rh_bonus(5) == 0 && rh_bonus(6) == 0);

    // خرید با شرط‌ها
    assert(rh_weapon_state(1) == 2 && rh_weapon_state(2) == 0);
    assert(rh_buy_weapon(1) == 0);                     // پول نیست
    assert(rh_buy_weapon(2) == -2);                    // ناشناخته
    rh_bank_run(10000, 0);
    assert(rh_profile_get(MONEY) == 10000 && rh_profile_get(RUNS) == 1 && rh_tier_runs(1) == 1);
    assert(rh_buy_weapon(1) == 1 && rh_profile_get(MONEY) == 8500 && rh_buy_weapon(1) == -1);
    assert(rh_buy_vest(1) == 1 && rh_buy_vest(2) == -2);
    assert(rh_land_unlocked(0) == 1 && rh_land_unlocked(3) == 0 && rh_land_unlocked(5) == 0);

    // ارتقای خونه: شرط ۳ خروج موفق تیر ۱
    assert(rh_house_upgrade() == -2);
    rh_bank_run(0, 1); rh_bank_run(0, 2);
    assert(rh_tier_runs(1) == 3 || rh_tier_runs(1) == 3);
    assert(rh_profile_get(MONEY) == 7700);
    assert(rh_house_upgrade() == 0);                    // ۸۰۰۰$ لازمه
    rh_bank_run(1300, 0);
    assert(rh_house_upgrade() == 1 && rh_profile_get(HOUSE) == 2 && rh_profile_get(MONEY) == 1000);
    assert(rh_land_unlocked(3) == 1 && rh_land_unlocked(5) == 0);

    // لوت
    assert(rh_container_info(0, 0) == 120 && rh_container_info(4, 2) == 1 && rh_container_info(4, 1) == 2);
    assert(rh_loot_pick(3, 2, 5000) == 14 && rh_loot_pick(0, 2, 1) == 0);      // صندوق: آیتم ثابت
    assert(rh_loot_pick(2, 4, 0) == 13 && rh_item_special(13) == 1);            // گاوصندوق جواهری: کمیاب
    assert(rh_loot_pick(1, 0, 9999) != -1 && rh_loot_pick(9, 0, 0) == -1);
    for (int s = 0; s < 4; s++) for (int k = 0; k < 5; k++) for (int r = 0; r < 10000; r += 3) {
        int id = rh_loot_pick(s, k, r); assert(id >= 0 && id < 19 && rh_item_value(id) > 0);
    }
    int cheap = rh_loot_pick(1, 0, 5000), good = rh_loot_pick(1, 3, 9999);        // قفسه: ارزون / ویترین: گرون
    assert(rh_item_value(cheap) < rh_item_value(good) || rh_item_special(good));
    assert(rh_alarm_stars(0, 0) == 1 && rh_alarm_stars(3, 0) == 2 && rh_alarm_stars(3, 5) == 3);
    assert(rh_hack_time(1) == 500 && rh_hack_time(3) == 850);
    assert(rh_sell_price(1000, 125, 100) == 1250 && rh_sell_price(1000, 100, 230) == 2300);

    // آمار برای jail time
    rh_profile_add(SHOPS, 5); rh_profile_add(LOOTED, 20); rh_profile_add(ALARMS, 2);
    rh_profile_add(KILLS, 3); rh_profile_max(MAXW, 4); rh_profile_add(BUSTED, 1); rh_profile_add(LOOTV, 12500);
    rh_profile_max(MAXW, 2);   // کمتر → نادیده
    assert(rh_profile_get(MAXW) == 4);
    // shops10 + looted20 + alarms6 + kills12 + wanted15 + busted10 + runs(3 → 9) + loot12 
    assert(rh_jail_preview() == 10 + 20 + 0 + 6 + 0 + 12 + 15 + 10 + 12 + 12);

    // هنوز سیکل تموم نشده
    assert(rh_cycle_check(T0 + 3 * 86400 - 1) == 0 && rh_profile_get(JAIL) == 0);
    // ریست
    int prevJail = rh_jail_preview();
    assert(rh_cycle_check(T0 + 3 * 86400 + 5) == 1);
    assert(rh_profile_get(JAIL) == prevJail && rh_profile_get(LASTJAIL) == prevJail);
    assert(rh_profile_get(CYCLE_NO) == 2 && rh_profile_get(CYCLE_START) == T0 + 3 * 86400);
    assert(rh_profile_get(MONEY) == 0 && rh_profile_get(WEAPONS) == 1 && rh_profile_get(VEST) == 0 && rh_profile_get(HOUSE) == 1);
    assert(rh_profile_get(SHOPS) == 0 && rh_tier_runs(1) == 0 && rh_jail_preview() == 0);
    assert(rh_snap_get(MONEY) >= 0 && rh_snap_get(KILLS) == 3);
    // jail = 97 → Shotgun(20) Medium vest(15) Jammer II(25) Bigger bag(30) Fast hands(40) Fence(50) Assault(60) Heat sink(80) باز
    int m2 = rh_tech_mask();
    assert((m2 & (1 << 2)) && (m2 & (1 << 6)) && (m2 & (1 << 9)) && (m2 & (1 << 13)) && (m2 & (1 << 10)));
    assert((m2 & (1 << 3)) && (m2 & (1 << 14)) && (m2 & (1 << 15)));
    assert((m2 & (1 << 7)) && !(m2 & (1 << 4)) && !(m2 & (1 << 11)));   // Heavy باز؛ LMG و Drill هنوز ???
    assert(rh_snap_new_tech_mask() == (m2 & ~m));
    assert(rh_tech_new_mask() == (m2 & ~m));
    rh_tech_mark_seen(); assert(rh_tech_new_mask() == 0);
    assert(rh_bonus(3) == 300 && rh_bonus(4) == 75 && rh_bonus(6) == 2 && rh_bonus(7) == 10 && rh_bonus(8) == 8 && rh_bonus(5) == 0);
    assert(rh_container_info(0, 0) == 90 && rh_hack_time(1) == 300);
    assert(rh_sell_price(1000, 100, 100) == 1100);
    // بعد از ریست هنوز Shotgun تو درخت بازه ولی لول خونه ۱ کمه
    assert(rh_weapon_state(2) == 1 && rh_buy_weapon(2) == -3);

    // چند سیکل با هم (۹ روز دور بودن)
    assert(rh_cycle_check(T0 + 3 * 86400 + 9 * 86400) == 3 && rh_profile_get(CYCLE_NO) == 5);

    // ذخیره/بارگذاری و peek
    assert(rh_peek(save, JAIL) == prevJail && rh_peek(save, CYCLE_NO) == 5);
    assert(rh_server_open(save, T0 + 999999999) == 2);
    assert(rh_profile_get(JAIL) == prevJail && rh_profile_get(CYCLE_NO) == 5);
    assert(rh_server_delete(save) == 1 && rh_peek(save, 0) == -1);

    rh_cycle_set_length(120);
    assert(rh_server_open(save, 5000) == 1 && rh_cycle_remaining(5000) == 120);
    rh_server_delete(save);

    // ---- امتیازهای ماندگار درخت (وکیل، پول شروع، جلیقه‌ی شروع، نگه‌داشتن پول)
    const char* save2 = "/tmp/rh_srv_test2.sav";
    rh_server_delete(save2);
    rh_cycle_set_length(3 * 86400);
    assert(rh_server_open(save2, 100) == 1);
    assert(rh_bonus(10) == 0 && rh_bonus(11) == 0 && rh_alarm_stars(3, 0) == 2);
    rh_profile_add(JAIL, 300);                        // همه‌ی درخت باز
    assert(rh_tech_mask() == (1 << 25) - 1);
    assert(rh_bonus(10) == 30 && rh_bonus(11) == 1 && rh_bonus(12) == 1 && rh_bonus(13) == 1500 && rh_bonus(14) == 25 && rh_bonus(15) == 2 && rh_bonus(16) == 25);
    assert(rh_alarm_stars(3, 0) == 1 && rh_alarm_stars(0, 0) == 1 && rh_alarm_stars(3, 5) == 2);   // سیم‌چین
    assert(rh_vest_stat(3, 0) == 170.f * 125 / 100.f);                                              // Kevlar Contract
    rh_bank_run(4000, 0);
    assert(rh_cycle_check(100 + 3 * 86400 + 1) == 1);
    assert(rh_profile_get(MONEY) == 4000 * 25 / 100 + 1500);                                         // Pardon + Parole Cash
    assert(rh_profile_get(VEST) == 2 && rh_profile_get(HOUSE) == 1 && rh_profile_get(WEAPONS) == 1);
    rh_server_delete(save2);

    // ---- تسلیم زودتر به فدرال‌ها
    const char* save3 = "/tmp/rh_srv_test3.sav";
    rh_server_delete(save3);
    assert(rh_server_open(save3, 1000) == 1);
    assert(rh_surrender(2000) == 0);                                    // هنوز کاری نکردی
    rh_profile_add(SHOPS, 4); rh_profile_add(LOOTED, 12); rh_profile_add(ALARMS, 1);   // 8 + 12 + 3 = 23
    rh_bank_run(5000, 0);                                               // +3 (خروج) + 5 (لوت 5000 نیست؛ LOOT_VALUE جدا) 
    int prev = rh_jail_preview();
    assert(prev == 8 + 12 + 3 + 3);
    int mask0 = rh_tech_mask(), wouldMask = rh_tech_mask_with(prev);
    assert(wouldMask != mask0 && (wouldMask & mask0) == mask0);          // پیش‌نمایش بدون تغییر واقعی
    assert(rh_tech_mask() == mask0 && rh_profile_get(JAIL) == 0);
    assert(rh_surrender(50000) == 1);
    assert(rh_profile_get(JAIL) == prev && rh_profile_get(CYCLE_NO) == 2 && rh_profile_get(CYCLE_START) == 50000);
    assert(rh_profile_get(MONEY) == 0 && rh_profile_get(SHOPS) == 0 && rh_tech_mask() == wouldMask);
    assert(rh_cycle_remaining(50000) == 3 * 86400);                     // ساعت ۷۲ ساعته از نو
    assert(rh_cycle_check(50000 + 3 * 86400 - 5) == 0);
    rh_server_delete(save3);
    puts("ALL OK");
}
