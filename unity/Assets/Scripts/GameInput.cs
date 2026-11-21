using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

// ورودی؛ با Input Manager قدیمی و Input System جدید هر دو کار می‌کنه
public static class GameInput
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 Move()
    {
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1f : 0f) - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1f : 0f);
        float y = ((k.wKey.isPressed || k.upArrowKey.isPressed) ? 1f : 0f) - ((k.sKey.isPressed || k.downArrowKey.isPressed) ? 1f : 0f);
        return new Vector2(x, y);
    }
    public static Vector2 Look() => Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue() * 0.08f;
    public static bool Fire() => Mouse.current != null && Mouse.current.leftButton.isPressed;
    public static bool Sprint() => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    public static bool InteractHeld() => Keyboard.current != null && Keyboard.current.eKey.isPressed;
    public static bool InteractDown() => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    public static bool ReloadDown() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
    public static bool EscDown() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    public static bool MuteDown() => Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame;
    public static bool TechDown() => Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
    public static bool MapDown() => Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
    public static bool DevSkipDay() => Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame;
    public static bool DevForceWipe() => Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
    public static int WeaponKeyDown()
    {
        var k = Keyboard.current;
        if (k == null) return 0;
        if (k.digit1Key.wasPressedThisFrame) return 1;
        if (k.digit2Key.wasPressedThisFrame) return 2;
        if (k.digit3Key.wasPressedThisFrame) return 3;
        if (k.digit4Key.wasPressedThisFrame) return 4;
        if (k.digit5Key.wasPressedThisFrame) return 5;
        return 0;
    }
#else
    public static Vector2 Move() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public static Vector2 Look() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 2.2f;
    public static bool Fire() => Input.GetMouseButton(0);
    public static bool Sprint() => Input.GetKey(KeyCode.LeftShift);
    public static bool InteractHeld() => Input.GetKey(KeyCode.E);
    public static bool InteractDown() => Input.GetKeyDown(KeyCode.E);
    public static bool ReloadDown() => Input.GetKeyDown(KeyCode.R);
    public static bool EscDown() => Input.GetKeyDown(KeyCode.Escape);
    public static bool MuteDown() => Input.GetKeyDown(KeyCode.K);
    public static bool TechDown() => Input.GetKeyDown(KeyCode.T);
    public static bool MapDown() => Input.GetKeyDown(KeyCode.M);
    public static bool DevSkipDay() => Input.GetKeyDown(KeyCode.F8);
    public static bool DevForceWipe() => Input.GetKeyDown(KeyCode.F9);
    public static int WeaponKeyDown()
    {
        for (int i = 0; i < 5; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return i + 1;
        return 0;
    }
#endif
}
