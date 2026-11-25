using UnityEngine;
using System.Collections.Generic;

public partial class GameManager
{
    void AddRiotGear(Humanoid hu)
    {
        var frame=hu.transform;
        var bones=BonePoser.Index(frame);
        if(!bones.TryGetValue("Benjamin_Head",out Transform head)||!bones.TryGetValue("Benjamin_Spine01",out Transform chest))return;
        Color armor=new Color(.10f,.13f,.17f), edge=new Color(.19f,.23f,.28f), visor=new Color(.11f,.20f,.26f);
        // A scale-independent holder aligned with the character before attaching to its animated bone.
        Transform Mount(string name,Transform bone,Vector3 pos)
        {
            var anchor=new GameObject(name).transform;anchor.SetParent(frame,false);
            anchor.localPosition=pos;anchor.SetParent(bone,true);return anchor;
        }
        Vector3 h=frame.InverseTransformPoint(head.position);
        var helmet=Mount("Riot helmet",head,new Vector3(h.x,h.y+.115f,h.z));
        Prim(PrimitiveType.Sphere,"Helmet shell",helmet,Vector3.zero,new Vector3(.265f,.255f,.285f),armor);
        Prim(PrimitiveType.Sphere,"Face visor",helmet,new Vector3(0,-.055f,.105f),new Vector3(.235f,.125f,.10f),visor);
        foreach(float side in new[]{-1f,1f})Prim(PrimitiveType.Sphere,"Ear guard",helmet,new Vector3(side*.12f,-.035f,0),new Vector3(.065f,.12f,.13f),edge);
        float sh=hu.poser!=null?hu.poser.ShoulderMid.y:1.45f;
        var vest=Mount("Riot body armor",chest,new Vector3(0,sh-.22f,0));
        var shell=Mat(armor);var trim=Mat(edge);
        ArtBox("Chest plate",vest,new Vector3(0,0,.14f),new Vector3(.38f,.36f,.095f),shell);
        ArtBox("Back plate",vest,new Vector3(0,0,-.135f),new Vector3(.38f,.36f,.095f),shell);
        for(int i=0;i<3;i++)ArtBox("Armor pouch",vest,new Vector3((i-1)*.112f,-.105f,.205f),new Vector3(.09f,.13f,.075f),trim);
        foreach(string side in new[]{"L","R"})
        {
            if(bones.TryGetValue("Benjamin_"+side+"_UpperArm",out Transform arm)&&bones.TryGetValue("Benjamin_"+side+"_Forearm",out Transform elbow))
            {
                var p=frame.InverseTransformPoint(Vector3.Lerp(arm.position,elbow.position,.18f));
                var mount=Mount("Shoulder protector",arm,p);Prim(PrimitiveType.Sphere,"Shoulder pad",mount,Vector3.zero,new Vector3(.185f,.18f,.19f),armor);
            }
            if(bones.TryGetValue("Benjamin_"+side+"_Calf",out Transform calf)&&bones.TryGetValue("Benjamin_"+side+"_Foot",out Transform foot))
            {
                var p=frame.InverseTransformPoint(Vector3.Lerp(calf.position,foot.position,.38f));
                var mount=Mount("Shin protector",calf,p);ArtBox("Shin plate",mount,new Vector3(0,0,.065f),new Vector3(.13f,.27f,.085f),shell);
            }
        }
    }
}
