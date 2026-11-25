using UnityEngine;
public partial class GameManager
{
    bool settingsOpen; int settingsTab, captureKey=-1; string bindNotice=""; Vector2 settingsScroll;
    void CloseSettings(){settingsOpen=false;captureKey=-1;GameOptions.Save();}
    float OptionSlider(float x,float y,float w,string name,float v,float min,float max,string value){GUI.Label(new Rect(x,y,w*.46f,28),name,sLabel);GUI.Label(new Rect(x+w*.84f,y,w*.16f,28),value,sRight);return GUI.HorizontalSlider(new Rect(x+w*.46f,y+10,w*.36f,20),v,min,max);}
    void DrawSettings(float W,float H)
    {
        Fill(new Rect(0,0,W,H),new Color(.025f,.04f,.065f,.98f)); float w=Mathf.Min(740,W-36),x=(W-w)/2;
        GUI.Label(new Rect(x,20,w,54),"SETTINGS",sBig);
        string[] tabs={"Audio","Controls","Display"}; for(int i=0;i<tabs.Length;i++){if(GUI.Button(new Rect(x+i*w/3,85,w/3-3,40),tabs[i],sBtn)){settingsTab=i;captureKey=-1;settingsScroll=Vector2.zero;}}
        if(settingsTab==0){GameOptions.Master=OptionSlider(x,155,w,"Master volume",GameOptions.Master,0,1,Mathf.RoundToInt(GameOptions.Master*100)+"%");GameOptions.Music=OptionSlider(x,210,w,"Music",GameOptions.Music,0,1,Mathf.RoundToInt(GameOptions.Music*100)+"%");GameOptions.Effects=OptionSlider(x,265,w,"Gunshots / effects",GameOptions.Effects,0,1,Mathf.RoundToInt(GameOptions.Effects*100)+"%");GameOptions.Vehicles=OptionSlider(x,320,w,"Vehicle engines",GameOptions.Vehicles,0,1,Mathf.RoundToInt(GameOptions.Vehicles*100)+"%");if(GUI.Button(new Rect(x,385,w*.48f,38),"Preview gunshot",sBtn)&&GameAudio.I!=null)GameAudio.I.PreviewGun();if(GUI.Button(new Rect(x+w*.52f,385,w*.48f,38),"Preview engine",sBtn)&&GameAudio.I!=null)GameAudio.I.PreviewEngine();GUI.Label(new Rect(x,434,w,32),"Gun preview cycles through all five weapon types.",sSmall);}
        if(settingsTab==1){GameOptions.Sensitivity=OptionSlider(x,145,w,"Mouse sensitivity",GameOptions.Sensitivity,.2f,3f,GameOptions.Sensitivity.ToString("F2"));GameOptions.InvertY=GUI.Toggle(new Rect(x,188,w,28),GameOptions.InvertY,"Invert mouse Y");
            var ev=Event.current;
            if(captureKey>=0 && ev.type==EventType.KeyDown){if(ev.keyCode==KeyCode.Escape){captureKey=-1;bindNotice="Cancelled";}else{bindNotice=GameOptions.Bind(captureKey,ev.keyCode)?"Binding saved":"Key reserved or already in use";captureKey=-1;}ev.Use();}
            settingsScroll=GUI.BeginScrollView(new Rect(x,226,w,335),settingsScroll,new Rect(0,0,w-23,GameOptions.Actions.Length*34));
            for(int i=0;i<GameOptions.Actions.Length;i++){GUI.Label(new Rect(8,i*34,w*.48f,32),GameOptions.Actions[i],sLabel);if(GUI.Button(new Rect(w*.50f,i*34,w*.44f-26,30),captureKey==i?"Press a key...":GameOptions.Keys[i].ToString())){captureKey=i;bindNotice="Press a keyboard key, or use mouse buttons below";}}
            GUI.EndScrollView(); if(captureKey>=0){for(int i=0;i<3;i++)if(GUI.Button(new Rect(x+i*100,568,96,25),"Mouse "+(i+1))){bindNotice=GameOptions.Bind(captureKey,KeyCode.Mouse0+i)?"Binding saved":"Already in use";captureKey=-1;}} GUI.Label(new Rect(x,597,w,25),bindNotice,sSmall);
        }
        if(settingsTab==2){GameOptions.Fov=OptionSlider(x,155,w,"Camera field of view",GameOptions.Fov,55,95,Mathf.RoundToInt(GameOptions.Fov)+"°");GameOptions.CameraKick=GUI.Toggle(new Rect(x,211,w,30),GameOptions.CameraKick,"Camera recoil effect");GUI.Label(new Rect(x,270,w*.43f,30),"Graphics quality",sLabel);GameOptions.Graphics=GUI.SelectionGrid(new Rect(x+w*.43f,264,w*.57f,40),GameOptions.Graphics,new[]{"Low","Medium","High"},3);}
        if(GUI.Button(new Rect(x,650,w*.45f,44),"Reset defaults",sBtn))GameOptions.Reset();if(GUI.Button(new Rect(x+w*.55f,650,w*.45f,44),"Save & back",sBtn))CloseSettings();GameOptions.Apply();
    }
}
