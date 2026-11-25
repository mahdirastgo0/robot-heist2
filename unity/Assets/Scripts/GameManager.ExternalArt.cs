using UnityEngine;

public partial class GameManager
{
    // External prefabs are opt-in, static visual meshes; physics stays with the original game.
    GameObject ExternalModel(string name, Transform parent)
    {
        var prefab=Resources.Load<GameObject>("ExternalArt/Models/"+name);
        if(prefab==null)return null;
        var go=Instantiate(prefab,parent,false);
        go.transform.localPosition=Vector3.zero; go.transform.localRotation=Quaternion.identity;
        foreach(var c in go.GetComponentsInChildren<Collider>(true))c.enabled=false;
        foreach(var b in go.GetComponentsInChildren<Rigidbody>(true)){b.isKinematic=true;b.detectCollisions=false;}
        return go;
    }
    Transform FindArtAnchor(Transform root,string name)
    { foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null; }
    GunSlot ExternalGun(Transform mount,int weapon)
    {
        string[] names={"Pistol","SMG","Shotgun","Rifle","LMG"};
        var model=Resources.Load<GameObject>("ExternalArt/Weapons/"+names[weapon]);
        if(model!=null && weapon<=2)
        {
            var holder=new GameObject(names[weapon]+" imported");holder.transform.SetParent(mount,false);
            var mesh=Instantiate(model,holder.transform,false);mesh.transform.localPosition=Vector3.zero;mesh.transform.localRotation=Quaternion.identity;
            foreach(var c in mesh.GetComponentsInChildren<Collider>(true))c.enabled=false;
            // Meshes have been normalized with the grip at the origin and the barrel facing +Z.
            Vector3 muzzlePos=weapon==0?new Vector3(0,.105f,.205f):weapon==1?new Vector3(0,.13f,.241f):new Vector3(0,.095f,.736f);
            Vector3 leftPos=weapon==0?new Vector3(0,-.015f,.026f):weapon==1?new Vector3(0,.045f,.13f):new Vector3(0,.055f,.39f);
            Transform Anchor(string n,Vector3 pos){var a=new GameObject(n).transform;a.SetParent(holder.transform,false);a.localPosition=pos;return a;}
            return new GunSlot{go=holder,muzzle=Anchor("Muzzle",muzzlePos),rGrip=Anchor("GripR",Vector3.zero),lGrip=Anchor("GripL",leftPos),rLocal=Vector3.zero,lLocal=leftPos};
        }
        var go=ExternalModel(names[weapon],mount); if(go==null)return null;
        var muzzle=FindArtAnchor(go.transform,"Muzzle"); var r=FindArtAnchor(go.transform,"GripR"); var l=FindArtAnchor(go.transform,"GripL");
        if(muzzle==null||r==null||l==null){go.SetActive(false);Destroy(go);Debug.LogWarning("External gun requires Muzzle, GripR, GripL: "+names[weapon]);return null;}
        return new GunSlot{go=go,muzzle=muzzle,rGrip=r,lGrip=l,rLocal=mount.InverseTransformPoint(r.position),lLocal=mount.InverseTransformPoint(l.position)};
    }
    bool ExternalVehicle(Transform parent,int type,DriveCar car)
    {
        // These meshes are already normalized to gameplay coordinates and use explicit per-slot materials.
        // Never run old vehicle fitting or repainting code on them.
        return SpawnNewCarArt(parent,type,false,car);
    }
}
