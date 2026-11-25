using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
public static class GameInput
{
    static bool Read(KeyCode key,bool down=false)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if(key==KeyCode.Mouse0||key==KeyCode.Mouse1||key==KeyCode.Mouse2){var m=Mouse.current;if(m==null)return false;ButtonControl b=key==KeyCode.Mouse0?m.leftButton:key==KeyCode.Mouse1?m.rightButton:m.middleButton;return down?b.wasPressedThisFrame:b.isPressed;}
        var k=Keyboard.current;if(k==null)return false;string n=key.ToString().Replace("Alpha","Digit").Replace("Control","Ctrl").Replace("Return","Enter");if(!System.Enum.TryParse<Key>(n,true,out var v)||v==Key.None)return false;return down?k[v].wasPressedThisFrame:k[v].isPressed;
#else
        return down?Input.GetKeyDown(key):Input.GetKey(key);
#endif
    }
    static bool B(int i,bool down=false)=>Read(GameOptions.Keys[i],down);
    public static Vector2 Move()=>new Vector2((B(3)?1:0)-(B(2)?1:0),(B(0)?1:0)-(B(1)?1:0));
    public static Vector2 Look(){
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        Vector2 v=Mouse.current==null?Vector2.zero:Mouse.current.delta.ReadValue()*.08f;
#else
        Vector2 v=new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y"))*2.2f;
#endif
        return new Vector2(v.x,v.y*(GameOptions.InvertY?-1:1))*GameOptions.Sensitivity;
    }
    public static bool Fire()=>B(15); public static bool Aim()=>B(16); public static bool Walk()=>B(5); public static bool JumpDown()=>B(6,true); public static bool Brake()=>B(11); public static bool CrouchDown()=>B(7,true); public static bool CarDown()=>B(10,true); public static bool VehicleMenuDown()=>B(13,true); public static bool Sprint()=>B(4); public static bool InteractHeld()=>B(8); public static bool InteractDown()=>B(8,true); public static bool ReloadDown()=>B(9,true); public static bool EscDown()=>Read(KeyCode.Escape,true); public static bool MuteDown()=>B(22,true); public static bool TechDown()=>B(14,true); public static bool MapDown()=>B(12,true); public static bool DevSkipDay()=>Read(KeyCode.F8,true); public static bool DevForceWipe()=>Read(KeyCode.F9,true);
    public static bool HornDown()=>B(23,true);
    public static int WeaponKeyDown(){for(int i=0;i<5;i++)if(B(17+i,true))return i+1;return 0;}
}
