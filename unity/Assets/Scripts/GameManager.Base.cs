using UnityEngine;

// بیس (خونه‌ی بازیکن): فروشنده‌ی اسلحه، کمد جلیقه، ترمینال تک‌تری، میز ارتقای خونه و گاراژ (نقشه‌ی جهان)
public partial class GameManager
{
    void BuildBase()
    {
        ResetRoot();
        int house = Mathf.Clamp(NativeCore.Profile(NativeCore.House), 1, NativeCore.HouseMax);
        TexBox(new Vector3(0f, -0.1f, 0f), new Vector3(24f, 0.2f, 18f), TexMat("wood", Color.white, 0.2f), 2.4f, true);
        var baseWall = TexMat("plaster", new Color(0.95f, 0.88f, 0.8f), 0.06f);
        TexBox(new Vector3(0f, 2f, 9.25f), new Vector3(24.5f, 4f, 0.5f), baseWall, 4f, true);
        TexBox(new Vector3(0f, 2f, -9.25f), new Vector3(24.5f, 4f, 0.5f), baseWall, 4f, true);
        TexBox(new Vector3(-12.25f, 2f, 0f), new Vector3(0.5f, 4f, 18f), baseWall, 4f, true);
        TexBox(new Vector3(12.25f, 2f, 0f), new Vector3(0.5f, 4f, 18f), baseWall, 4f, true);

        // قالیچه
        Box(new Vector3(-3f, 0.02f, -3f), new Vector3(7f, 0.03f, 4.5f), new Color(0.45f, 0.12f, 0.12f));
        Box(new Vector3(-3f, 0.035f, -3f), new Vector3(6.2f, 0.03f, 3.7f), new Color(0.6f, 0.2f, 0.18f));

        // کاناپه و تلویزیون
        Color sofa = new Color(0.25f, 0.28f, 0.38f);
        Box(new Vector3(-3f, 0.35f, -5.3f), new Vector3(3.2f, 0.7f, 1.1f), sofa, true);
        Box(new Vector3(-3f, 0.9f, -5.75f), new Vector3(3.2f, 0.8f, 0.25f), sofa);
        Box(new Vector3(-4.7f, 0.55f, -5.3f), new Vector3(0.3f, 0.5f, 1.1f), sofa * 0.9f);
        Box(new Vector3(-1.3f, 0.55f, -5.3f), new Vector3(0.3f, 0.5f, 1.1f), sofa * 0.9f);
        Box(new Vector3(-3f, 0.25f, -3.2f), new Vector3(1.6f, 0.5f, 0.9f), new Color(0.35f, 0.25f, 0.18f), true);   // میز
        Box(new Vector3(-3f, 1.7f, -8.9f), new Vector3(3f, 1.7f, 0.15f), new Color(0.05f, 0.05f, 0.07f));          // تلویزیون
        Box(new Vector3(-3f, 1.7f, -8.82f), new Vector3(2.7f, 1.4f, 0.05f), new Color(0.2f, 0.45f, 0.7f));

        // پنجره‌ها و پوستر
        Box(new Vector3(-11.95f, 2.1f, -2f), new Vector3(0.1f, 1.8f, 3f), new Color(1f, 0.8f, 0.5f));
        Box(new Vector3(-11.95f, 2.1f, 3.5f), new Vector3(0.1f, 1.8f, 3f), new Color(1f, 0.8f, 0.5f));
        Box(new Vector3(-11.9f, 2.1f, -2f), new Vector3(0.08f, 0.1f, 3.1f), new Color(0.1f, 0.1f, 0.12f));
        Box(new Vector3(-3f, 2.6f, 8.95f), new Vector3(1.6f, 2f, 0.05f), new Color(0.8f, 0.2f, 0.3f));
        Box(new Vector3(4f, 2.6f, 8.95f), new Vector3(1.6f, 2f, 0.05f), new Color(0.2f, 0.5f, 0.8f));

        // گلدان
        foreach (var p in new[] { new Vector3(-10.5f, 0f, 7.8f), new Vector3(10.5f, 0f, 7.8f), new Vector3(10.5f, 0f, -7.8f) })
        {
            Cyl(p + new Vector3(0f, 0.4f, 0f), new Vector3(0.7f, 0.4f, 0.7f), new Color(0.55f, 0.3f, 0.2f));
            Ball(p + new Vector3(0f, 1.2f, 0f), Vector3.one * 1.1f, new Color(0.2f, 0.5f, 0.25f));
        }

        // میز فروشنده‌ی اسلحه (قفسه‌ی اسلحه پشتش) و کمد جلیقه
        MakeCounter(new Vector3(-6f, 0f, 6.5f), new Color(0.8f, 0.2f, 0.2f), "WEAPON DEALER", SpotType.WeaponDealer);
        for (int i = 0; i < 5; i++)
        {
            Box(new Vector3(-8f + i * 1.0f, 2.2f, 8.9f), new Vector3(0.85f, 0.12f, 0.1f), new Color(0.12f, 0.12f, 0.14f));
            Box(new Vector3(-8f + i * 1.0f, 2.1f, 8.9f), new Vector3(0.12f, 0.25f, 0.1f), new Color(0.12f, 0.12f, 0.14f));
        }
        Box(new Vector3(-6f, 1.8f, 8.95f), new Vector3(5.4f, 0.1f, 0.3f), new Color(0.3f, 0.2f, 0.12f));
        MakeCounter(new Vector3(0f, 0f, 6.5f), new Color(0.25f, 0.45f, 0.95f), "ARMOR LOCKER", SpotType.ArmorLocker);
        for (int i = 0; i < 3; i++)
            Box(new Vector3(-1f + i * 1.0f, 2.0f, 8.85f), new Vector3(0.7f, 0.9f, 0.25f), new Color(0.2f + 0.1f * i, 0.28f, 0.22f));

        // ترمینال تک‌تری: میز با دو مانیتور
        Color techCol = new Color(0.2f, 0.9f, 0.95f);
        MakeCounter(new Vector3(6f, 0f, 6.5f), techCol, "TECH TERMINAL", SpotType.TechTerminal);
        Box(new Vector3(5.3f, 1.55f, 6.5f), new Vector3(1.1f, 0.7f, 0.08f), new Color(0.04f, 0.04f, 0.06f));
        Box(new Vector3(5.3f, 1.55f, 6.45f), new Vector3(0.95f, 0.55f, 0.04f), new Color(0.1f, 0.55f, 0.6f));
        Box(new Vector3(6.7f, 1.55f, 6.5f), new Vector3(1.1f, 0.7f, 0.08f), new Color(0.04f, 0.04f, 0.06f));
        Box(new Vector3(6.7f, 1.55f, 6.45f), new Vector3(0.95f, 0.55f, 0.04f), new Color(0.15f, 0.45f, 0.25f));
        Box(new Vector3(6f, 1.2f, 6.2f), new Vector3(1.3f, 0.03f, 0.4f), new Color(0.1f, 0.1f, 0.12f));   // کیبورد

        // میز نقشه‌ی خونه (ارتقای خونه)
        Color houseCol = new Color(1f, 0.75f, 0.25f);
        Box(new Vector3(5f, 0.55f, -7f), new Vector3(3.2f, 1.1f, 1.5f), new Color(0.4f, 0.28f, 0.18f), true);
        Box(new Vector3(5f, 1.12f, -7f), new Vector3(3.0f, 0.05f, 1.3f), new Color(0.35f, 0.55f, 0.85f));      // نقشه
        for (int i = 0; i < 4; i++) Box(new Vector3(4.2f + i * 0.5f, 1.14f, -7f), new Vector3(0.02f, 0.02f, 1.2f), new Color(0.9f, 0.95f, 1f));
        MakeSpot(SpotType.HouseTerminal, new Vector3(5f, 0f, -5f), houseCol, "UPGRADE HOUSE");

        // خط فدرال: تلفن روی میز کنار پنجره. بدون صبر کردن برای پایان سیکل خودتو معرفی کن
        Color fedCol = new Color(0.45f, 0.65f, 1f);
        Box(new Vector3(-10.2f, 0.4f, 1f), new Vector3(1.3f, 0.8f, 1.3f), new Color(0.2f, 0.22f, 0.3f), true);
        Box(new Vector3(-10.2f, 0.84f, 1f), new Vector3(1.5f, 0.06f, 1.5f), new Color(0.8f, 0.8f, 0.84f));
        Box(new Vector3(-10.2f, 0.95f, 1f), new Vector3(0.5f, 0.15f, 0.32f), new Color(0.08f, 0.08f, 0.1f));          // تلفن
        Box(new Vector3(-10.2f, 1.05f, 1f), new Vector3(0.62f, 0.05f, 0.14f), new Color(0.15f, 0.15f, 0.2f));        // گوشی
        Box(new Vector3(-10.2f, 1.02f, 0.78f), new Vector3(0.08f, 0.08f, 0.08f), new Color(1f, 0.15f, 0.12f));       // چراغ
        Box(new Vector3(-11.9f, 1.9f, 1f), new Vector3(0.06f, 0.5f, 0.9f), new Color(0.15f, 0.2f, 0.55f));          // تابلوی فدرال
        MakeSpot(SpotType.FederalDesk, new Vector3(-8.7f, 0f, 1f), fedCol, "FEDERAL HOTLINE");

        // در گاراژ (شرق) با خطوط و علامت هشدار
        Box(new Vector3(11.9f, 1.8f, 0f), new Vector3(0.3f, 3.6f, 7f), new Color(0.15f, 0.5f, 0.25f));
        for (int i = 0; i < 6; i++)
            Box(new Vector3(11.7f, 0.5f + i * 0.6f, 0f), new Vector3(0.1f, 0.06f, 6.8f), new Color(0.3f, 0.7f, 0.4f));
        for (int i = 0; i < 6; i++)
            Box(new Vector3(8.2f, 0.012f, -3f + i * 1.2f), new Vector3(0.6f, 0.02f, 0.6f), i % 2 == 0 ? new Color(0.95f, 0.8f, 0.1f) : new Color(0.1f, 0.1f, 0.1f));
        MakeSpot(SpotType.Garage, new Vector3(9.3f, 0f, 0f), new Color(0.2f, 1f, 0.4f), "GARAGE -> WORLD MAP");

        // جعبه‌ها
        Box(new Vector3(-9f, 0.6f, -6f), new Vector3(1.6f, 1.2f, 1.6f), new Color(0.5f, 0.35f, 0.2f), true);
        Box(new Vector3(-7.2f, 0.4f, -6.4f), new Vector3(1.1f, 0.8f, 1.1f), new Color(0.55f, 0.4f, 0.22f), true);
        Box(new Vector3(8f, 0.6f, -6.5f), new Vector3(1.6f, 1.2f, 1.6f), new Color(0.5f, 0.35f, 0.2f), true);
        Box(new Vector3(8f, 1.5f, -6.5f), new Vector3(1.0f, 0.6f, 1.0f), new Color(0.55f, 0.4f, 0.22f));

        BuildHouseDecor(house);
        CombineScenery();
        CreatePlayer(new Vector3(0f, 0.1f, 0f), 0f);
        Sign3D(NativeCore.HouseNames[house].ToUpper() + "  -  LEVEL " + house, new Vector3(0f, 3.1f, -8.9f), 180f, 7f, 0.55f, new Color(1f, 0.82f, 0.35f));
    }

    // هر لول خونه چیزای بیشتری داره: Lv2 کتابخونه و فرش طلایی، Lv3 مجسمه و ویترین جایزه
    void BuildHouseDecor(int house)
    {
        if (house >= 2)
        {
            Color wood = new Color(0.35f, 0.24f, 0.15f);
            Box(new Vector3(-11.4f, 1.3f, -7f), new Vector3(0.6f, 2.6f, 2.8f), wood, true);
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 5; j++)
                    Box(new Vector3(-11.08f, 0.5f + i * 0.6f, -8.0f + j * 0.45f), new Vector3(0.05f, 0.45f, 0.3f), new Color(0.4f + 0.1f * ((i + j) % 3), 0.2f, 0.2f + 0.15f * (j % 2)));
            Box(new Vector3(0f, 0.025f, -3f), new Vector3(2.4f, 0.02f, 5.2f), new Color(0.75f, 0.6f, 0.2f));
            Box(new Vector3(-0.2f, 0.5f, -1.2f), new Vector3(0.5f, 1.0f, 0.5f), new Color(0.3f, 0.3f, 0.34f), true);
            Ball(new Vector3(-0.2f, 1.2f, -1.2f), new Vector3(0.5f, 0.5f, 0.5f), new Color(1f, 0.92f, 0.7f));   // لامپ ایستاده
        }
        if (house >= 3)
        {
            Color gold = new Color(1f, 0.82f, 0.25f);
            Prim(PrimitiveType.Cylinder, "StatueBase", scn, new Vector3(0f, 0.35f, -7.4f), new Vector3(1.4f, 0.35f, 1.4f), new Color(0.2f, 0.2f, 0.22f), true);
            Prim(PrimitiveType.Capsule, "Statue", scn, new Vector3(0f, 1.7f, -7.4f), new Vector3(0.7f, 0.9f, 0.7f), gold);
            Ball(new Vector3(0f, 2.8f, -7.4f), Vector3.one * 0.5f, gold);
            Box(new Vector3(11.5f, 0.6f, 6.3f), new Vector3(1.2f, 1.2f, 4f), new Color(0.12f, 0.12f, 0.15f), true);
            Box(new Vector3(11.5f, 1.3f, 6.3f), new Vector3(1.0f, 0.04f, 3.8f), new Color(0.6f, 0.85f, 0.95f));
            for (int i = 0; i < 4; i++) Ball(new Vector3(11.5f, 1.45f, 4.9f + i * 0.9f), Vector3.one * 0.3f, gold);
            Box(new Vector3(-3f, 3.3f, 8.95f), new Vector3(6f, 0.1f, 0.1f), gold);
        }
    }

    void MakeCounter(Vector3 pos, Color c, string label, SpotType type)
    {
        Box(pos + Vector3.up * 0.55f, new Vector3(3.5f, 1.1f, 1.2f), c, true);
        Box(pos + Vector3.up * 1.13f, new Vector3(3.7f, 0.1f, 1.4f), new Color(0.85f, 0.85f, 0.85f));
        MakeSpot(type, pos + new Vector3(0f, 0f, -2.2f), c, label);
    }
}
