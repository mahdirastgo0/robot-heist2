using System.Collections.Generic;
using UnityEngine;

public enum GameState { Menu, Base, Loading, City, Dead }
public enum UiPanel { None, Weapons, Armor, Tech, House, Map, Summary, Intro, Surrender, Vehicles }
public enum SpotType { Shop, Market, Exit, Garage, WeaponDealer, ArmorLocker, TechTerminal, HouseTerminal, FederalDesk, Container, AlarmPanel }

// یه نقطه‌ی تعاملی: دروازه، فروشنده، ظرف لوت (قفسه/کابینت/صندوق...) یا پنل آلارم
public class Spot
{
    public SpotType type;
    public Vector3 pos;
    public string label;
    public int shopType;        // 0 خواروبار، 1 الکترونیک، 2 جواهری، 3 بانک
    public float duration;      // ثانیه‌ی نگه‌داشتن E (برای ظرف‌ها و پنل آلارم)
    public bool done;
    public Color color;
    public GameObject pad, beam;

    public ShopInfo shop;       // مغازه‌ی صاحب این نقطه
    public int kind;            // نوع ظرف: 0 قفسه، 1 کابینت، 2 صندوق، 3 ویترین، 4 گاوصندوق
    public Vector2 half;        // نصف ابعاد ظرف (x,z) برای فاصله تا مستطیل
    public float yaw;
    public Transform goods;     // کالاهای روی ظرف؛ بعد از لوت نابود میشه

    public bool Minor { get { return type == SpotType.Container || type == SpotType.AlarmPanel; } }

    public float Reach
    {
        get
        {
            if (type == SpotType.Container) return 1.0f;
            if (type == SpotType.AlarmPanel) return 1.7f;
            return 3f;
        }
    }

    // فاصله‌ی افقی بازیکن تا نقطه (یا تا لبه‌ی مستطیل ظرف)
    public float DistTo(Vector3 p)
    {
        float dx = p.x - pos.x, dz = p.z - pos.z;
        if (type != SpotType.Container) return Mathf.Sqrt(dx * dx + dz * dz);
        float a = yaw * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        float lx = dx * c - dz * s, lz = dx * s + dz * c;
        float ox = Mathf.Max(Mathf.Abs(lx) - half.x, 0f), oz = Mathf.Max(Mathf.Abs(lz) - half.y, 0f);
        return Mathf.Sqrt(ox * ox + oz * oz);
    }
}

// یه مغازه‌ی قابل ورود (خواروبار، الکترونیک، جواهری، بانک)
public class ShopInfo
{
    public int role;                 // 0..3
    public string name;
    public Vector3 ctr;              // مرکز ساختمان (دنیا)
    public bool alarmArmed = true, alarmTripped, entered;
    public float alarmUntil;
    public Spot marker, panel;
    public readonly List<Spot> containers = new List<Spot>();
    public Transform interior;       // داخل مغازه؛ فقط وقتی بازیکن نزدیکه ساخته میشه
    public readonly List<Humanoid> cashiers = new List<Humanoid>();
    public Renderer led, siren;      // چراغ پنل آلارم و آژیر بالای در
    public Renderer innerSiren;

    public int Left { get { int n = 0; foreach (var c in containers) if (!c.done) n++; return n; } }
}

public class LandDef
{
    public string name, tagline;
    public int tier, bx, by;                 // تعداد بلوک‌ها
    public int[] mix;                        // تعداد هر نقش: 0 خواروبار،1 الکترونیک،2 جواهری،3 بانک،4 بلک‌مارکت،5 آپارتمان،6 پارک،7 محوطه‌ی کانتینر
    public Color asphalt, wallTint, fog, sun, lamp, grass, accent;
    public float fogDensity, sunIntensity, carDensity;
    public Vector2 mapPos;                   // جای لند روی نقشه‌ی جهان (۰ تا ۱)
    public string danger;                    // برای نمایش تو نقشه
}

public static class Lands
{
    public static readonly LandDef[] All =
    {
        new LandDef
        {
            name = "Maple Heights", tagline = "Quiet suburb. Lots of corner stores, few eyes.", tier = 1, bx = 5, by = 5, danger = "LOW",
            mix = new[] { 7, 3, 2, 1, 1, 6, 5, 0 },
            asphalt = new Color(0.82f, 0.82f, 0.85f), wallTint = new Color(1f, 0.98f, 0.94f), fog = new Color(0.78f, 0.82f, 0.86f),
            sun = new Color(1f, 0.93f, 0.8f), lamp = new Color(1f, 0.92f, 0.65f), grass = new Color(0.33f, 0.52f, 0.25f), accent = new Color(0.4f, 0.8f, 0.4f),
            fogDensity = 0.0024f, sunIntensity = 1.25f, carDensity = 0.7f, mapPos = new Vector2(0.17f, 0.64f)
        },
        new LandDef
        {
            name = "Harbor Strip", tagline = "Docks and warehouses. Electronics fall off the ships.", tier = 1, bx = 5, by = 5, danger = "LOW",
            mix = new[] { 4, 6, 1, 1, 1, 3, 4, 5 },
            asphalt = new Color(0.7f, 0.72f, 0.76f), wallTint = new Color(0.9f, 0.95f, 1f), fog = new Color(0.7f, 0.78f, 0.86f),
            sun = new Color(0.95f, 0.95f, 0.9f), lamp = new Color(0.85f, 0.95f, 1f), grass = new Color(0.3f, 0.45f, 0.3f), accent = new Color(0.3f, 0.6f, 1f),
            fogDensity = 0.0034f, sunIntensity = 1.15f, carDensity = 0.5f, mapPos = new Vector2(0.28f, 0.27f)
        },
        new LandDef
        {
            name = "Old Town", tagline = "Brick streets, jewelers, nervous shop owners.", tier = 1, bx = 5, by = 5, danger = "MEDIUM",
            mix = new[] { 5, 2, 5, 1, 1, 8, 3, 0 },
            asphalt = new Color(0.62f, 0.6f, 0.58f), wallTint = new Color(1f, 0.9f, 0.82f), fog = new Color(0.86f, 0.8f, 0.72f),
            sun = new Color(1f, 0.85f, 0.65f), lamp = new Color(1f, 0.78f, 0.45f), grass = new Color(0.36f, 0.48f, 0.24f), accent = new Color(1f, 0.75f, 0.3f),
            fogDensity = 0.003f, sunIntensity = 1.1f, carDensity = 0.8f, mapPos = new Vector2(0.38f, 0.74f)
        },
        new LandDef
        {
            name = "Midtown", tagline = "Big blocks, tall towers, two banks. Heavy police.", tier = 2, bx = 6, by = 6, danger = "HIGH",
            mix = new[] { 5, 7, 5, 2, 1, 10, 4, 2 },
            asphalt = new Color(0.66f, 0.68f, 0.72f), wallTint = new Color(0.9f, 0.95f, 1.05f), fog = new Color(0.72f, 0.76f, 0.82f),
            sun = new Color(0.98f, 0.95f, 0.9f), lamp = new Color(1f, 0.95f, 0.8f), grass = new Color(0.3f, 0.48f, 0.28f), accent = new Color(0.4f, 0.65f, 1f),
            fogDensity = 0.0034f, sunIntensity = 1.1f, carDensity = 1.1f, mapPos = new Vector2(0.58f, 0.46f)
        },
        new LandDef
        {
            name = "Industrial Zone", tagline = "Container yards and tech depots. Patrols everywhere.", tier = 2, bx = 6, by = 6, danger = "HIGH",
            mix = new[] { 3, 9, 3, 2, 1, 4, 4, 10 },
            asphalt = new Color(0.55f, 0.56f, 0.58f), wallTint = new Color(0.85f, 0.88f, 0.92f), fog = new Color(0.66f, 0.68f, 0.7f),
            sun = new Color(1f, 0.88f, 0.72f), lamp = new Color(1f, 0.7f, 0.35f), grass = new Color(0.32f, 0.4f, 0.26f), accent = new Color(1f, 0.55f, 0.2f),
            fogDensity = 0.0042f, sunIntensity = 0.95f, carDensity = 0.5f, mapPos = new Vector2(0.64f, 0.8f)
        },
        new LandDef
        {
            name = "Financial Core", tagline = "Banks, jewelers, vaults. The big score.", tier = 3, bx = 7, by = 7, danger = "EXTREME",
            mix = new[] { 4, 8, 9, 5, 1, 12, 6, 4 },
            asphalt = new Color(0.5f, 0.52f, 0.58f), wallTint = new Color(0.82f, 0.88f, 1f), fog = new Color(0.5f, 0.54f, 0.66f),
            sun = new Color(1f, 0.72f, 0.5f), lamp = new Color(0.7f, 0.85f, 1f), grass = new Color(0.25f, 0.4f, 0.3f), accent = new Color(0.8f, 0.4f, 1f),
            fogDensity = 0.0042f, sunIntensity = 0.85f, carDensity = 1.0f, mapPos = new Vector2(0.86f, 0.3f)
        },
    };
}
