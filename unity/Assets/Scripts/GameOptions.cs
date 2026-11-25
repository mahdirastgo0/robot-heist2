using UnityEngine;
using System.Collections.Generic;

public static class GameOptions
{
    public static float Master=1f, Music=.8f, Effects=1f, Vehicles=.8f, Sensitivity=1f, Fov=68f;
    public static bool InvertY=false, CameraKick=true;
    public static int Graphics=2;
    public static readonly string[] Actions={"Forward","Backward","Left","Right","Sprint","Walk","Jump","Crouch","Interact","Reload","Enter / exit car","Brake","Map","Garage","Tech","Fire","Aim","Weapon 1","Weapon 2","Weapon 3","Weapon 4","Weapon 5","Music toggle","Car horn"};
    static readonly KeyCode[] Defaults={KeyCode.W,KeyCode.S,KeyCode.A,KeyCode.D,KeyCode.LeftShift,KeyCode.LeftAlt,KeyCode.Space,KeyCode.C,KeyCode.E,KeyCode.R,KeyCode.F,KeyCode.Space,KeyCode.M,KeyCode.V,KeyCode.T,KeyCode.Mouse0,KeyCode.Mouse1,KeyCode.Alpha1,KeyCode.Alpha2,KeyCode.Alpha3,KeyCode.Alpha4,KeyCode.Alpha5,KeyCode.K,KeyCode.H};
    public static readonly KeyCode[] Keys=(KeyCode[])Defaults.Clone();
    public static void Load()
    {
        Master=PlayerPrefs.GetFloat("rh.master",1); Music=PlayerPrefs.GetFloat("rh.music",.8f); Effects=PlayerPrefs.GetFloat("rh.effects",1); Vehicles=PlayerPrefs.GetFloat("rh.vehicles",.8f);
        Sensitivity=PlayerPrefs.GetFloat("rh.sensitivity",1); Fov=PlayerPrefs.GetFloat("rh.fov",68); InvertY=PlayerPrefs.GetInt("rh.invert",0)!=0; CameraKick=PlayerPrefs.GetInt("rh.kick",1)!=0; Graphics=Mathf.Clamp(PlayerPrefs.GetInt("rh.graphics",2),0,2);
        for(int i=0;i<Keys.Length;i++) Keys[i]=(KeyCode)PlayerPrefs.GetInt("rh.key."+i,(int)Defaults[i]);
        Apply();
    }
    public static void Reset(){Master=1;Music=.8f;Effects=1;Vehicles=.8f;Sensitivity=1;Fov=68;InvertY=false;CameraKick=true;Graphics=2;System.Array.Copy(Defaults,Keys,Keys.Length);Save();}
    static int appliedGraphics=-1;
    public static string KeyLabel(int i)=>Keys[i].ToString().Replace("Alpha","").Replace("Mouse0","LMB").Replace("Mouse1","RMB").Replace("Mouse2","MMB");
    public static void Apply(){AudioListener.volume=Master;if(appliedGraphics==Graphics)return;appliedGraphics=Graphics;QualitySettings.shadowDistance=Graphics==0?45:Graphics==1?95:150;QualitySettings.antiAliasing=Graphics==0?0:Graphics==1?2:4;QualitySettings.masterTextureLimit=Graphics==0?1:0;QualitySettings.shadowResolution=Graphics==2?ShadowResolution.High:ShadowResolution.Medium;}
    public static void Save(){PlayerPrefs.SetFloat("rh.master",Master);PlayerPrefs.SetFloat("rh.music",Music);PlayerPrefs.SetFloat("rh.effects",Effects);PlayerPrefs.SetFloat("rh.vehicles",Vehicles);PlayerPrefs.SetFloat("rh.sensitivity",Sensitivity);PlayerPrefs.SetFloat("rh.fov",Fov);PlayerPrefs.SetInt("rh.invert",InvertY?1:0);PlayerPrefs.SetInt("rh.kick",CameraKick?1:0);PlayerPrefs.SetInt("rh.graphics",Graphics);for(int i=0;i<Keys.Length;i++)PlayerPrefs.SetInt("rh.key."+i,(int)Keys[i]);PlayerPrefs.Save();Apply();}
    public static bool Bind(int action,KeyCode key){if(key==KeyCode.None||key==KeyCode.Escape||key==KeyCode.F8||key==KeyCode.F9)return false;for(int i=0;i<Keys.Length;i++)if(i!=action&&Keys[i]==key&&!((action==6&&i==11)||(action==11&&i==6)))return false;Keys[action]=key;Save();return true;}
}
