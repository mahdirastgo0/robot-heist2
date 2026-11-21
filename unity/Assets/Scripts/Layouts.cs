using UnityEngine;

// چیدمان داخل مغازه‌ها. این فایل از work/layouts.py تولید شده و چیدمان‌ها از نظر برخورد و دسترسی بازیکن بررسی شدن.
// مختصات نسبت به مرکز ساختمان: x = شرق، z = شمال (در ورودی جنوبه، z = -5.4)
public struct FDef
{
    public int style, kind;        // style: شکل ظاهری | kind: نوع ظرف لوت (-1 = فقط دکور)
    public float x, z, yaw, len, dep;
    public string name;
    public FDef(int style, int kind, float x, float z, float yaw, float len, float dep, string name)
    { this.style = style; this.kind = kind; this.x = x; this.z = z; this.yaw = yaw; this.len = len; this.dep = dep; this.name = name; }
}

public static class Layouts
{
    public const int S_GONDOLA = 0, S_WALLSHELF = 1, S_FRIDGE = 2, S_CABINET = 3, S_COUNTER = 4, S_DISPLAY = 5, S_SAFE = 6, S_DEPOSIT = 7, S_BENCH = 8, S_PARTITION = 9, S_DESK = 10, S_ISLAND = 11, S_VAULTDOOR = 12;

    public static readonly FDef[][] Shop =
    {
        new FDef[]   // Grocery
        {
            new FDef(S_COUNTER, 2, 3.6f, -3.4f, 0f, 2.8f, 0.9f, "Cash Register"),
            new FDef(S_CABINET, 1, -3.5f, -5.0f, 0f, 2.6f, 0.6f, "Cabinet"),
            new FDef(S_CABINET, 1, -5.05f, -2.4f, 90f, 2.0f, 0.6f, "Cabinet"),
            new FDef(S_GONDOLA, 0, -4.0f, 1.2f, 90f, 4.0f, 0.8f, "Shelves"),
            new FDef(S_GONDOLA, 0, -2.0f, 1.2f, 90f, 4.0f, 0.8f, "Shelves"),
            new FDef(S_GONDOLA, 0, 2.0f, 1.4f, 90f, 2.4f, 0.9f, "Produce Stand"),
            new FDef(S_WALLSHELF, 0, -3.6f, 5.05f, 180f, 3.2f, 0.6f, "Shelves"),
            new FDef(S_WALLSHELF, 0, 0.0f, 5.05f, 180f, 3.2f, 0.6f, "Shelves"),
            new FDef(S_WALLSHELF, 0, 3.6f, 5.05f, 180f, 3.2f, 0.6f, "Shelves"),
            new FDef(S_FRIDGE, 1, 5.05f, 0.3f, 270f, 2.2f, 0.6f, "Fridge"),
            new FDef(S_FRIDGE, 1, 5.05f, 2.7f, 270f, 2.2f, 0.6f, "Fridge"),
        },
        new FDef[]   // Electronics
        {
            new FDef(S_COUNTER, 2, -3.6f, -3.4f, 0f, 2.8f, 0.9f, "Cash Register"),
            new FDef(S_ISLAND, 3, -2.4f, 0.8f, 0f, 2.8f, 1.2f, "Display Table"),
            new FDef(S_ISLAND, 3, 2.4f, 0.8f, 0f, 2.8f, 1.2f, "Display Table"),
            new FDef(S_ISLAND, 3, -2.4f, 3.1f, 0f, 2.8f, 1.2f, "Display Table"),
            new FDef(S_ISLAND, 3, 2.4f, 3.1f, 0f, 2.8f, 1.2f, "Display Table"),
            new FDef(S_WALLSHELF, 0, -3.4f, 5.05f, 180f, 3.2f, 0.6f, "Shelving"),
            new FDef(S_WALLSHELF, 0, 0.0f, 5.05f, 180f, 3.2f, 0.6f, "Shelving"),
            new FDef(S_WALLSHELF, 0, 3.4f, 5.05f, 180f, 3.2f, 0.6f, "Shelving"),
            new FDef(S_CABINET, 1, -5.05f, 1.9f, 90f, 3.0f, 0.6f, "Stock Cabinet"),
            new FDef(S_CABINET, 1, 5.05f, 1.9f, 270f, 3.0f, 0.6f, "Stock Cabinet"),
            new FDef(S_CABINET, 1, 5.05f, -3.4f, 270f, 2.0f, 0.6f, "Stock Cabinet"),
        },
        new FDef[]   // Jewelry
        {
            new FDef(S_COUNTER, 2, 3.4f, -3.4f, 0f, 3.0f, 0.9f, "Cash Register"),
            new FDef(S_DISPLAY, 3, -5.0f, -0.5f, 90f, 3.0f, 0.8f, "Jewelry Case"),
            new FDef(S_DISPLAY, 3, -5.0f, 2.9f, 90f, 3.0f, 0.8f, "Jewelry Case"),
            new FDef(S_DISPLAY, 3, 5.0f, 0.3f, 270f, 3.0f, 0.8f, "Jewelry Case"),
            new FDef(S_DISPLAY, 3, 5.0f, 3.4f, 270f, 3.0f, 0.8f, "Jewelry Case"),
            new FDef(S_ISLAND, 3, -2.3f, 1.2f, 0f, 2.4f, 1.0f, "Jewelry Island"),
            new FDef(S_ISLAND, 3, 2.3f, 1.2f, 0f, 2.4f, 1.0f, "Jewelry Island"),
            new FDef(S_CABINET, 1, -3.0f, 5.05f, 180f, 2.6f, 0.6f, "Cabinet"),
            new FDef(S_CABINET, 1, 0.2f, 5.05f, 180f, 2.4f, 0.6f, "Cabinet"),
            new FDef(S_SAFE, 4, 3.6f, 4.85f, 180f, 1.4f, 1.0f, "Safe"),
        },
        new FDef[]   // Bank
        {
            new FDef(S_COUNTER, 2, -4.4f, -0.8f, 0f, 2.0f, 1.0f, "Teller Counter"),
            new FDef(S_COUNTER, 2, -2.4f, -0.8f, 0f, 2.0f, 1.0f, "Teller Counter"),
            new FDef(S_DEPOSIT, 1, -3.7f, 5.1f, 180f, 2.6f, 0.5f, "Deposit Boxes"),
            new FDef(S_DEPOSIT, 1, -1.1f, 5.1f, 180f, 2.6f, 0.5f, "Deposit Boxes"),
            new FDef(S_DESK, 1, -3.4f, 2.8f, 0f, 1.8f, 0.9f, "Manager's Desk"),
            new FDef(S_CABINET, 1, 5.05f, -1.8f, 270f, 2.0f, 0.6f, "Records Cabinet"),
            new FDef(S_BENCH, -1, 3.4f, -3.8f, 0f, 2.2f, 0.6f, "Bench"),
            new FDef(S_PARTITION, -1, 3.8f, 1.2f, 0f, 3.2f, 0.3f, "Vault Wall"),
            new FDef(S_PARTITION, -1, 2.2f, 2.05f, 90f, 1.1f, 0.3f, "Vault Wall"),
            new FDef(S_PARTITION, -1, 2.2f, 4.7f, 90f, 1.4f, 0.3f, "Vault Wall"),
            new FDef(S_VAULTDOOR, -1, 2.2f, 3.3f, 90f, 1.4f, 0.4f, "Vault Door"),
            new FDef(S_SAFE, 4, 4.85f, 2.2f, 270f, 1.2f, 0.9f, "Vault Safe"),
            new FDef(S_SAFE, 4, 4.85f, 4.6f, 270f, 1.2f, 0.9f, "Vault Safe"),
            new FDef(S_SAFE, 4, 3.4f, 4.9f, 180f, 1.2f, 0.9f, "Vault Safe"),
        },
    };

    // صندوقدارها: x, z, yaw
    public static readonly Vector3[][] Cashiers =
    {
        new Vector3[] { new Vector3(3.6f, -4.55f, 0f) },
        new Vector3[] { new Vector3(-3.6f, -4.55f, 0f) },
        new Vector3[] { new Vector3(3.4f, -4.55f, 0f) },
        new Vector3[] { new Vector3(-4.4f, 0.45f, 180f), new Vector3(-2.4f, 0.45f, 180f) },
    };
}
