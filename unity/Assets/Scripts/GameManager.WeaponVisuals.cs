using UnityEngine;

public partial class GameManager
{
    // Low-poly art assets with their own silhouettes, muzzle and IK anchors.
    GunSlot MakeCustomGun(Transform mount,int weapon)
    {
        var g=new GameObject(weapon==1?"Compact SMG":weapon==2?"Pump Shotgun":"Heavy LMG"); g.transform.SetParent(mount,false);
        var t=g.transform;
        Color steel=new Color(.17f,.18f,.20f), black=new Color(.07f,.08f,.09f), wood=new Color(.41f,.23f,.12f), accent=new Color(.29f,.32f,.35f);
        void B(string n,float x,float y,float z,float sx,float sy,float sz,Color c) => ArtBox(n,t,new Vector3(x,y,z),new Vector3(sx,sy,sz),TexMat(c==wood || c==wood*.78f ? "wood_grain" : "gun_finish",c,c==wood?.26f:.48f,c==wood?0f:.55f));
        void Tube(string n,float y,float z,float diameter,float length,Color c)
        { var o=Prim(PrimitiveType.Cylinder,n,t,new Vector3(0,y,z),new Vector3(diameter,length*.5f,diameter),c); o.transform.localRotation=Quaternion.Euler(90,0,0); }
        Vector3 muzzle, left;
        B("Grip",0,-.055f,-.015f,.055f,.14f,.075f,black);
        if(weapon==1)
        {
            B("Compact body",0,.085f,.075f,.085f,.12f,.30f,steel);
            B("Stock",0,.06f,-.17f,.06f,.065f,.18f,black);
            B("Stock pad",0,.035f,-.265f,.07f,.13f,.035f,black);
            B("Magazine",0,-.08f,.13f,.055f,.19f,.065f,accent);
            Tube("Short muzzle",.09f,.257f,.048f,.10f,black);
            B("Sight",0,.165f,.08f,.035f,.035f,.12f,accent);
            B("Foregrip",0,.01f,.20f,.065f,.07f,.07f,black);
            muzzle=new Vector3(0,.09f,.31f); left=new Vector3(0,.015f,.20f);
        }
        else if(weapon==2)
        {
            B("Receiver",0,.07f,.04f,.07f,.10f,.27f,steel);
            B("Wood stock",0,.015f,-.19f,.074f,.13f,.26f,wood);
            B("Butt pad",0,.015f,-.33f,.078f,.13f,.028f,black);
            Tube("Upper barrel",.085f,.40f,.037f,.53f,steel);
            Tube("Lower visual tube",.032f,.35f,.036f,.39f,black);
            B("Pump",0,.026f,.29f,.079f,.072f,.18f,wood);
            for(int i=0;i<7;i++)B("Pump detail",0,.026f,.22f+i*.023f,.082f,.076f,.008f,wood*.78f);
            B("Sight",0,.113f,.63f,.02f,.025f,.023f,accent);
            muzzle=new Vector3(0,.085f,.67f);left=new Vector3(0,.025f,.29f);
        }
        else
        {
            B("Heavy receiver",0,.085f,.10f,.115f,.15f,.35f,steel);
            B("Stock",0,.04f,-.20f,.09f,.12f,.25f,black);
            B("Belt box",.04f,-.055f,.12f,.16f,.18f,.20f,new Color(.28f,.32f,.24f));
            B("Top cover",0,.172f,.12f,.12f,.028f,.30f,accent);
            B("Fore-end",0,.075f,.36f,.105f,.10f,.25f,black);
            Tube("Long barrel",.09f,.57f,.04f,.24f,steel);
            Tube("Muzzle",.09f,.71f,.057f,.075f,black);
            foreach(float side in new[]{-1f,1f})B("Folded support",side*.07f,.01f,.42f,.023f,.027f,.26f,accent);
            for(int i=0;i<5;i++) B("Gold belt detail",.085f-i*.022f,.025f,.12f,.017f,.023f,.07f,new Color(.66f,.5f,.21f));
            muzzle=new Vector3(0,.09f,.75f);left=new Vector3(0,.03f,.31f);
        }
        Transform Anchor(string n,Vector3 pos){var a=new GameObject(n).transform;a.SetParent(t,false);a.localPosition=pos;return a;}
        return new GunSlot{go=g,muzzle=Anchor("Muzzle",muzzle),rGrip=Anchor("GripR",Vector3.zero),lGrip=Anchor("GripL",left),rLocal=Vector3.zero,lLocal=left};
    }
}
