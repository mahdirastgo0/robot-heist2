using System.Collections.Generic;
using System.IO;
using UnityEngine;

// منطق اصلی بازی. بقیه‌ی کد:
//   GameManager.World.cs    ساخت لند (خیابون، ساختمون، پارک...)       GameManager.Interior.cs  داخل مغازه‌ها
//   GameManager.Meta.cs     سرور، سیکل ۳ روزه، تک‌تری، خونه           GameManager.UI.cs        رابط کاربری
public partial class GameManager : MonoBehaviour
{
    public const float Cell = 4f;
    public int CW = 31, CH = 31;                       // اندازه‌ی گرید لند فعلی

    public GameState State { get; private set; } = GameState.Menu;
    public UiPanel CurrentPanel { get; private set; } = UiPanel.None;
    public int Wanted { get; private set; }
    public PlayerController Player { get; private set; }
    public Camera Cam => cam;
    public bool Running => State == GameState.City && !paused;
    public bool InputBlocked => !(State == GameState.Base || State == GameState.City) || CurrentPanel != UiPanel.None || paused;
    public float LastShotTime { get; private set; } = -10f;
    public Vector3 LastShotPos { get; private set; }

    Camera cam;
    Shader shader;
    Shader lineShader;
    Transform root;
    readonly List<Spot> spots = new List<Spot>();
    readonly List<ShopInfo> shops = new List<ShopInfo>();
    readonly List<RobotCop> cops = new List<RobotCop>();
    readonly List<Vector2Int> walkCells = new List<Vector2Int>();
    readonly List<Vector3Int> blockInfos = new List<Vector3Int>();   // x,y = سلول گوشه‌ی ساختمان، z = نقش (برای مینی‌مپ)
    bool[,] blocked;
    int curLandIdx;
    LandDef curLand;

    public int BagCap => 8 + NativeCore.BagBonus;
    readonly List<int> bag = new List<int>();               // آیتم‌های داخل کیف (id آیتم)
    int runCash;
    float marketMult = 1f;
    Spot holdSpot;
    float holdProgress, calmTimer, lastSeenTime, alarmSndT, nextCycleCheck;
    bool exitWarned, paused, panicArmed;
    string hint = "", deathMsg = "", fatalMsg = "";

    class ToastMsg { public string text; public float until; }
    readonly List<ToastMsg> toasts = new List<ToastMsg>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (GameObject.Find("GameManager") == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    int BagValue()
    {
        int v = 0;
        foreach (int id in bag) v += NativeCore.ItemValue[id];
        return v;
    }

    int MarketMultX100() { return Mathf.RoundToInt(marketMult * 100f); }
    int SellOffer() { return NativeCore.SellPrice(BagValue(), MarketMultX100(), NativeCore.LandLootX100(curLandIdx)); }

    void Awake()
    {
        gameObject.AddComponent<GameAudio>();
        bool ok = NativeCore.Init();
        if (!ok) fatalMsg = "C++ core not loaded (" + NativeCore.Error + ").\nRun native/build_windows.bat, then restart Unity.";
        SetupCameraAndLight();
        if (ok) BuildBase();   // پشت منوی اصلی دیده میشه
    }

    // ---------------------------------------------------------------- Flow
    void StartBase(string msg = null, bool surrendered = false)
    {
        // سیکل تموم شده (یا خودت تسلیم شدی)؟ پیشرفت ریست میشه و jail time بهت داده میشه
        bool wiped = NativeCore.CycleCheck() > 0 || surrendered;
        surrenderedEarly = surrendered;
        BuildBase();
        State = GameState.Base;
        CurrentPanel = wiped ? UiPanel.Summary : UiPanel.None;
        if (wiped) trialStep = 0;
        paused = false;
        Wanted = 0;
        bag.Clear(); runCash = 0;
        holdSpot = null; holdProgress = 0f; bigMap = false;
        ApplyTheme(null);
        if (msg != null) Toast(msg);
        if (wiped) Toast(surrendered ? "You turned yourself in. The court has sentenced you." : "The cycle ended - everything was reset. Check your new jail time!");
    }

    public void StartLand(int idx)
    {
        if (idx < 0 || idx >= Lands.All.Length) return;
        if (!NativeCore.LandUnlocked(idx)) { Toast("Upgrade your house to enter " + Lands.All[idx].name + "."); return; }
        CurrentPanel = UiPanel.None;
        StartCoroutine(LandRoutine(idx));
    }

    void OnLandReady()
    {
        State = GameState.City;
        Wanted = 0; calmTimer = 0f;
        bag.Clear(); runCash = 0; exitWarned = false; bigMap = false;
        holdSpot = null; holdProgress = 0f;
        Toast(curLand.name + " - search the shops. Disable alarms at the red panel beside each door first!");
        Toast("Sell loot at the BLACK MARKET (purple), then reach the EXIT. Market rate: x" + marketMult.ToString("0.00"));
    }

    public void PlayerDied()
    {
        if (State != GameState.City) return;
        State = GameState.Dead;
        NativeCore.ProfileAdd(NativeCore.Busted, 1);
        int kept = runCash * NativeCore.LawyerPct / 100;
        deathMsg = "You lost $" + BagValue() + " of loot (" + bag.Count + " items) and $" + (runCash - kept) + " of unbanked cash.";
        if (kept > 0)
        {
            NativeCore.ProfileAdd(NativeCore.Money, kept);
            deathMsg += "  Your public defender saved $" + kept + ".";
        }
        bag.Clear(); runCash = 0;
        CancelHold(); hint = "";
    }

    // ---------------------------------------------------------------- Update
    void Update()
    {
        UpdateCursor();
        UpdateAudio();
        if (!NativeCore.Available) return;
        if (Player != null && Player.Human != null) Player.Human.SetBag(bag.Count / (float)BagCap);
        if (State == GameState.Menu || State == GameState.Loading) return;
        DevKeys();
        FitSigns();
        if (State == GameState.City && CurrentPanel == UiPanel.None && GameInput.MapDown()) bigMap = !bigMap;

        if (State == GameState.Dead)
        {
            if (GameInput.InteractDown()) StartBase("You woke up at your base.");
            return;
        }

        if (GameInput.EscDown())
        {
            if (bigMap) bigMap = false;
            else if (CurrentPanel != UiPanel.None) CurrentPanel = UiPanel.None;
            else paused = !paused;
        }
        if (State == GameState.Base && CurrentPanel == UiPanel.None && GameInput.TechDown()) OpenPanel(UiPanel.Tech);
        toasts.RemoveAll(t => Time.time > t.until);
        if (paused || CurrentPanel != UiPanel.None) return;

        UpdateSpots();
        if (State == GameState.City)
        {
            UpdateWanted();
            UpdateShops();
            UpdateShopVisuals();
            UpdateSea();
        }
        else if (State == GameState.Base && Time.time > nextCycleCheck)
        {
            nextCycleCheck = Time.time + 1f;
            if (NativeCore.CycleRemaining <= 0) StartBase();    // سیکل تموم شد → ریست
        }
    }

    // فقط تو Editor / Development build: جلو انداختن ساعت برای تست ریست ۳ روزه
    void DevKeys()
    {
        if (!(Application.isEditor || Debug.isDebugBuild)) return;
        if (GameInput.DevSkipDay()) { NativeCore.DevTimeOffset += 86400; Toast("DEV: clock +1 day  (" + FormatSpan(NativeCore.CycleRemaining) + " left)"); }
        if (GameInput.DevForceWipe()) { NativeCore.DevTimeOffset += NativeCore.CycleRemaining + 1; Toast("DEV: cycle ends now. Return to base to see the wipe."); }
    }

    void UpdateAudio()
    {
        var a = GameAudio.I;
        if (a == null) return;
        if (GameInput.MuteDown()) a.ToggleMusic();
        float b = 0f, c = 0f;
        switch (State)
        {
            case GameState.Menu:
            case GameState.Base: b = 0.15f; break;
            case GameState.City: c = Wanted >= 3 ? 0.08f : 0.13f; break;
            case GameState.Dead: b = 0.05f; break;
        }
        a.SetMood(b, c);
    }

    void UpdateCursor()
    {
        bool lockIt = (State == GameState.City || State == GameState.Base) && CurrentPanel == UiPanel.None && !paused;
        Cursor.lockState = lockIt ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockIt;
    }

    public void OpenPanel(UiPanel p)
    {
        CurrentPanel = p;
        if (p == UiPanel.Tech) { techNewShown = NativeCore.TechNewMask; NativeCore.TechMarkSeen(); }
        if (p == UiPanel.Surrender) surrenderConfirm = false;
        if (p == UiPanel.Map) mapSel = Mathf.Clamp(mapSel, 0, Lands.All.Length - 1);
    }

    // ---------------------------------------------------------------- Interaction
    void UpdateSpots()
    {
        hint = "";
        if (Player == null) return;
        Vector3 pp = Player.transform.position;

        Spot near = null;
        float best = float.MaxValue;
        foreach (var s in spots)
        {
            if (s.done || s.type == SpotType.Shop) continue;
            float d = s.DistTo(pp);
            if (d <= s.Reach && d < best) { best = d; near = s; }
        }
        if (near == null || near != holdSpot) CancelHold();
        if (near == null) return;

        switch (near.type)
        {
            case SpotType.Container:
                if (near.kind == 4 && !NativeCore.CanDrill) { hint = near.label + " - needs the THERMAL DRILL (see Tech Tree)"; break; }
                if (bag.Count + NativeCore.ContainerItems(near.kind) > BagCap) { hint = "Your bag is full! Sell the loot at the BLACK MARKET first."; break; }
                hint = "Hold E to search: " + near.label;
                HoldTick(near);
                break;

            case SpotType.AlarmPanel:
                if (near.shop.alarmTripped) { hint = "Too late - the alarm is already ringing!"; break; }
                hint = "Hold E to disable the alarm (" + near.shop.name + ")";
                HoldTick(near);
                break;

            case SpotType.Market:
                hint = bag.Count > 0 ? "Press E to sell " + bag.Count + " item(s)  ($" + BagValue() + " -> $" + SellOffer() + ")" : "Black Market - your bag is empty";
                if (GameInput.InteractDown()) SellLoot();
                break;

            case SpotType.Exit:
                hint = "Press E to leave the land and go to your base";
                if (GameInput.InteractDown()) TryExit();
                break;

            case SpotType.Garage:
                hint = "Press E - choose a land on the world map";
                if (GameInput.InteractDown()) OpenPanel(UiPanel.Map);
                break;

            case SpotType.WeaponDealer:
                hint = "Press E - Weapon Dealer";
                if (GameInput.InteractDown()) OpenPanel(UiPanel.Weapons);
                break;

            case SpotType.ArmorLocker:
                hint = "Press E - Armor Locker";
                if (GameInput.InteractDown()) OpenPanel(UiPanel.Armor);
                break;

            case SpotType.TechTerminal:
                hint = "Press E - Tech Terminal" + (NativeCore.TechNewMask != 0 ? "  (NEW unlocks!)" : "");
                if (GameInput.InteractDown()) OpenPanel(UiPanel.Tech);
                break;

            case SpotType.FederalDesk:
                hint = "Press E - Federal hotline: turn yourself in early";
                if (GameInput.InteractDown()) OpenPanel(UiPanel.Surrender);
                break;

            case SpotType.HouseTerminal:
                hint = "Press E - Upgrade your house";
                if (GameInput.InteractDown()) OpenPanel(UiPanel.House);
                break;
        }
    }

    void HoldTick(Spot s)
    {
        if (!GameInput.InteractHeld()) { CancelHold(); return; }
        if (holdSpot != s)
        {
            holdSpot = s; holdProgress = 0f;
            // صندوقدار ممکنه دکمه‌ی پنیک رو بزنه
            panicArmed = s.type == SpotType.Container && s.kind == 2 && !s.shop.alarmTripped && Random.value < 0.35f;
        }
        holdProgress += Time.deltaTime;
        if (panicArmed && holdProgress > s.duration * 0.5f)
        {
            panicArmed = false;
            TripAlarm(s.shop, "The cashier hit the panic button!");
        }
        if (holdProgress >= s.duration) CompleteHold(s);
    }

    void CancelHold() { holdSpot = null; holdProgress = 0f; panicArmed = false; }

    public float HoldProgress01 => holdSpot != null && holdSpot.duration > 0f ? Mathf.Clamp01(holdProgress / holdSpot.duration) : 0f;

    void CompleteHold(Spot s)
    {
        CancelHold();
        if (s.type == SpotType.Container) LootContainer(s);
        else if (s.type == SpotType.AlarmPanel) DisarmAlarm(s.shop);
    }

    void LootContainer(Spot s)
    {
        int want = NativeCore.ContainerItems(s.kind);
        var names = new List<string>();
        bool rare = false;
        for (int k = 0; k < want && bag.Count < BagCap; k++)
        {
            int id = NativeCore.LootPick(s.shopType, s.kind, Random.Range(0, 10000));
            if (id < 0 || id >= NativeCore.ItemCount) continue;
            bag.Add(id);
            NativeCore.ProfileAdd(NativeCore.LootValue, NativeCore.ItemValue[id]);
            bool sp = NativeCore.ItemSpecial[id];
            rare |= sp;
            names.Add((sp ? "[RARE] " : "") + NativeCore.ItemNames[id] + " ($" + NativeCore.ItemValue[id] + ")");
        }
        s.done = true;
        if (s.goods != null) Destroy(s.goods.gameObject);
        NativeCore.ProfileAdd(NativeCore.Looted, 1);
        if (s.kind == 2) NativeCore.ProfileAdd(NativeCore.Registers, 1);
        Toast((rare ? "RARE FIND!  " : "") + s.label + ": " + (names.Count > 0 ? string.Join(", ", names.ToArray()) : "empty"));
        if (GameAudio.I != null) { if (s.kind == 2) GameAudio.I.Cash(); else GameAudio.I.Pickup(); }

        var sh = s.shop;
        if (sh != null && sh.Left == 0 && sh.marker != null)
        {
            sh.marker.done = true;
            if (sh.marker.pad != null) sh.marker.pad.GetComponent<Renderer>().material.color = Color.gray;
            if (sh.marker.beam != null) Destroy(sh.marker.beam);
            Toast(sh.name + " is cleaned out.");
        }
    }

    void DisarmAlarm(ShopInfo sh)
    {
        sh.alarmArmed = false;
        if (sh.panel != null) sh.panel.done = true;
        NativeCore.ProfileAdd(NativeCore.Disarmed, 1);
        Toast("Alarm disabled at " + sh.name + ". You can enter quietly.");
        if (GameAudio.I != null) GameAudio.I.Click();
    }

    // آلارم مغازه: ستاره‌ی تحت تعقیب میده و پلیس‌ها به سمت در مغازه میان
    void TripAlarm(ShopInfo sh, string why)
    {
        if (sh.alarmTripped) return;
        sh.alarmTripped = true;
        sh.alarmUntil = Time.time + 40f;
        NativeCore.ProfileAdd(NativeCore.Alarms, 1);
        Toast(why + "  ALARM at " + sh.name + "!");
        if (GameAudio.I != null) GameAudio.I.Alarm();
        RaiseWanted(NativeCore.AlarmStars(sh.role, curLandIdx), sh.ctr + new Vector3(0f, 0f, -8f));
    }

    void OnShopEntered(ShopInfo sh)
    {
        sh.entered = true;
        NativeCore.ProfileAdd(NativeCore.Shops, 1);
        if (sh.alarmArmed) TripAlarm(sh, "Door sensor tripped!");
        else Toast("Quiet entry into " + sh.name + ". Search the shelves, cabinets and the register.");
    }

    void SellLoot()
    {
        if (bag.Count == 0) { Toast("Your bag is empty."); return; }
        int cash = SellOffer();
        runCash += cash;
        Toast("Sold " + bag.Count + " item(s) for $" + cash + ". Now get to the EXIT!");
        bag.Clear();
        if (GameAudio.I != null) GameAudio.I.Cash();
    }

    void TryExit()
    {
        if (bag.Count > 0 && !exitWarned)
        {
            exitWarned = true;
            Toast("Your bag still holds $" + BagValue() + " of unsold loot! Press E again to leave without it.");
            return;
        }
        int cash = runCash;
        NativeCore.BankRun(cash, curLandIdx);
        StartBase("Made it home! Banked $" + cash + ".");
    }

    // ---------------------------------------------------------------- Heat & police
    void RaiseWanted(int n, Vector3? alertPos = null)
    {
        int old = Wanted;
        Wanted = Mathf.Min(5, Wanted + n);
        NativeCore.ProfileMax(NativeCore.MaxWanted, Wanted);
        calmTimer = 0f;
        if (Wanted > old)
        {
            Toast("WANTED level " + Wanted);
            if (GameAudio.I != null) GameAudio.I.Alarm();
        }
        int before = cops.Count;
        SpawnCops(n);
        if (alertPos.HasValue)
            for (int i = before; i < cops.Count; i++) cops[i].Alert(alertPos.Value);
    }

    void UpdateWanted()
    {
        if (Time.time - lastSeenTime < 1f) calmTimer = 0f; else calmTimer += Time.deltaTime;
        if (Wanted > 0 && calmTimer > NativeCore.HeatDecaySeconds)
        {
            Wanted--; calmTimer = 0f;
            Toast("The heat is dropping... wanted level " + Wanted);
        }
    }

    public void NotifySeen() { lastSeenTime = Time.time; }

    public void CopKilled(RobotCop c)
    {
        cops.Remove(c);
        NativeCore.ProfileAdd(NativeCore.Kills, 1);
        if (Wanted < 5) RaiseWanted(1);
        else SpawnCops(1);
    }

    int CopCap => 10 + (curLand != null ? curLand.tier * 2 : 0);

    void SpawnCops(int n)
    {
        if (State != GameState.City || Player == null || walkCells.Count == 0) return;
        for (int i = 0; i < n && cops.Count < CopCap; i++)
            for (int tries = 0; tries < 30; tries++)
            {
                var c = walkCells[Random.Range(0, walkCells.Count)];
                if (Flat(CellToWorld(c) - Player.transform.position).magnitude < 25f) continue;
                CreateCop(c);
                break;
            }
    }

    public void OnPlayerShot(Vector3 pos) { LastShotTime = Time.time; LastShotPos = pos; }

    // ---------------------------------------------------------------- Shopping (base)
    void BuyWeapon(int i)
    {
        int r = NativeCore.BuyWeapon(i);
        if (r == 1) { Toast("Bought " + NativeCore.WeaponNames[i] + "! Press " + (i + 1) + " in the field to equip it."); if (GameAudio.I != null) GameAudio.I.Coin(); }
        else if (r == 0) Toast("Not enough money.");
        else if (r == -3) Toast("Your house level is too low for this weapon.");
    }

    void BuyVest(int level)
    {
        int r = NativeCore.BuyVest(level);
        if (r == 1) { Player.RefreshLoadout(); Toast("Bought " + NativeCore.VestNames[level] + "."); if (GameAudio.I != null) GameAudio.I.Coin(); }
        else if (r == 0) Toast("Not enough money.");
        else if (r == -3) Toast("Your house level is too low for this vest.");
    }

    void Toast(string text) { toasts.Add(new ToastMsg { text = text, until = Time.time + 5.5f }); if (toasts.Count > 6) toasts.RemoveAt(0); }

    // ---------------------------------------------------------------- Helpers
    public static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    public Vector3 CellToWorld(Vector2Int c) => new Vector3(c.x * Cell, 0f, -c.y * Cell);
    public Vector2Int WorldToCell(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(-p.z / Cell));
    public Vector2Int RandomWalkCell() => walkCells[Random.Range(0, walkCells.Count)];
    public List<Vector2Int> FindPath(Vector2Int a, Vector2Int b) => NativeCore.FindPath(a, b);

    static readonly RaycastHit[] hitBuf = new RaycastHit[32];

    // نزدیک‌ترین برخورد، به‌جز collider های خود تیرانداز
    public static bool RayIgnore(Transform ignore, Vector3 origin, Vector3 dir, float dist, out RaycastHit best)
    {
        best = default;
        int n = Physics.RaycastNonAlloc(origin, dir, hitBuf, dist, ~0, QueryTriggerInteraction.Ignore);
        float bd = float.MaxValue;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            var h = hitBuf[i];
            if (ignore != null && h.collider.transform.IsChildOf(ignore)) continue;
            if (h.distance < bd) { bd = h.distance; best = h; found = true; }
        }
        return found;
    }

    public void Tracer(Vector3 a, Vector3 b, Color c)
    {
        if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
        var go = new GameObject("Tracer");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(lineShader);
        lr.startColor = c; lr.endColor = new Color(c.r, c.g, c.b, 0.2f);
        lr.startWidth = 0.05f; lr.endWidth = 0.02f;
        lr.positionCount = 2;
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        Destroy(go, 0.06f);
    }
}
