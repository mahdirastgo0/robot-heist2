using UnityEngine;
using UnityEngine.Rendering;

public partial class GameManager
{
    // Fit imported render meshes inside gameplay dimensions. Their colliders never override gameplay physics.
    static bool ArtBounds(Transform frame, GameObject go, out Bounds b)
    {
        b=new Bounds(); bool found=false;
        foreach(var r in go.GetComponentsInChildren<Renderer>())
        {
            if(!r.enabled)continue;
            Bounds w=r.localBounds;
            for(int i=0;i<8;i++)
            {
                Vector3 corner=w.center+Vector3.Scale(w.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector3 q=frame.InverseTransformPoint(r.transform.TransformPoint(corner));
                if(!found){b=new Bounds(q,Vector3.zero);found=true;}else b.Encapsulate(q);
            }
        }
        return found && b.size.sqrMagnitude>.000001f;
    }
    static bool FitArt(GameObject go, Vector3 maxSize, float yaw)
    {
        Transform t=go.transform, frame=t.parent; t.localRotation=Quaternion.Euler(0,yaw,0);
        if(!ArtBounds(frame,go,out Bounds b))return false;
        float k=Mathf.Min(maxSize.x/Mathf.Max(.001f,b.size.x),maxSize.y/Mathf.Max(.001f,b.size.y),maxSize.z/Mathf.Max(.001f,b.size.z));
        t.localScale*=k;
        if(!ArtBounds(frame,go,out b))return false;
        t.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
        return true;
    }
    void RemoveArt(GameObject go){if(go!=null){go.SetActive(false);Destroy(go);}}
    Material ImportedMaterial(string label, Texture2D tex, Texture2D normal, Color color, float smooth, float metal=0f)
    {
        var m=new Material(PipeShader()){name=label,color=color};
        if(tex!=null)m.mainTexture=tex;
        if(normal!=null&&m.HasProperty("_BumpMap")){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");}
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",smooth);
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metal);
        return m;
    }
    // Material roles are explicitly authored in each OBJ/MTL, not inferred from generic names.
    readonly System.Collections.Generic.Dictionary<string,Material> carSurfaceCache = new System.Collections.Generic.Dictionary<string,Material>();
    Material VehicleSurface(string role,int type)
    {
        string key=type+":"+role;
        if(carSurfaceCache.TryGetValue(key,out Material cached)&&cached!=null)return cached;
        Color c=new Color(.10f,.11f,.12f); float gloss=.25f, metal=.04f;
        Texture2D tex=null, normal=null;
        switch(role)
        {
            case "paint": c=type==0?new Color(.16f,.48f,.70f):type==1?new Color(.24f,.39f,.29f):new Color(.72f,.085f,.065f); gloss=.76f;metal=.12f;break;
            case "glass": c=new Color(.12f,.19f,.25f);gloss=.86f;break;
            case "headlamp": c=new Color(.88f,.91f,.94f);gloss=.78f;metal=.15f;break;
            case "taillamp": c=new Color(.72f,.045f,.035f);gloss=.68f;break;
            case "alloy": c=new Color(.55f,.58f,.62f);gloss=.65f;metal=.4f;break;
            case "rubber": c=new Color(.075f,.075f,.08f);gloss=.15f;break;
            case "interior": c=new Color(.20f,.21f,.23f);gloss=.18f;break;
            case "plate": c=new Color(.70f,.71f,.68f);break;
            case "caliper": c=new Color(.65f,.045f,.035f);gloss=.4f;break;
            case "livery": c=Color.white; gloss=.52f;metal=.05f;
                tex=Resources.Load<Texture2D>("ExternalArt/Models/Formula_Diffuse");
                normal=Resources.Load<Texture2D>("ExternalArt/Models/Formula_Normal");break;
        }
        var m=ImportedMaterial("RH "+role,tex,normal,c,gloss,metal);
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);
        if(m.HasProperty("_Color"))m.SetColor("_Color",c);
        if(tex!=null&&m.HasProperty("_BaseMap"))m.SetTexture("_BaseMap",tex);
        if(tex!=null&&m.HasProperty("_MainTex"))m.SetTexture("_MainTex",tex);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",gloss);
        carSurfaceCache[key]=m;return m;
    }
    static string VehicleRole(string value)
    {
        if(string.IsNullOrEmpty(value))return null;
        int start=value.IndexOf("RH_",System.StringComparison.OrdinalIgnoreCase);
        if(start<0)return null;
        start+=3;int end=start;
        while(end<value.Length&&char.IsLetter(value[end]))end++;
        return end>start?value.Substring(start,end-start).ToLowerInvariant():null;
    }
    void ApplyVehicleSurfaces(GameObject go,int type)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                string role=VehicleRole(mats[i]!=null?mats[i].name:null);
                if(role==null&&mats.Length==1)role=VehicleRole(r.name);
                // Unknown material slots keep the imported material, never blanket repaint.
                if(role!=null)mats[i]=VehicleSurface(role,type);
                else if(mats[i]==null)mats[i]=VehicleSurface("trim",type);
            }
            r.sharedMaterials=mats;
        }
    }
    bool ImportedSofa(Vector3 pos)
    {
        var anchor=new GameObject("Imported sofa").transform;anchor.SetParent(root,false);anchor.position=pos;
        var sizeFrame=new GameObject("Sofa size frame").transform;sizeFrame.SetParent(anchor,false);
        var go=ExternalModel("Couch",sizeFrame);
        bool livingRoom=go==null;
        if(livingRoom)
        {
            go=ExternalModel("LivingRoom",sizeFrame);
            if(go!=null)foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                // Source FBX contains a sectional and a separate straight couch.
                // Only the straight couch's seat, back, arm and two cushions belong here.
                bool sofa=false;
                for(Transform t=r.transform;t!=null&&t!=go.transform;t=t.parent)
                    if(t.name=="Sofa.004"||t.name=="Sofa.005"||t.name=="Sofa.006"||t.name=="Sofa.007"||t.name=="Sofa.008"){sofa=true;break;}
                r.enabled=sofa;
            }
        }
        if(go==null){RemoveArt(anchor.gameObject);return false;}
        go.transform.localRotation=Quaternion.Euler(0,livingRoom?90f:0f,0);
        if(!ArtBounds(sizeFrame,go,out Bounds b)||b.size.x<.0001f||b.size.y<.0001f||b.size.z<.0001f){RemoveArt(anchor.gameObject);return false;}
        // Center the chosen mesh, then size it in the unrotated parent axes.
        go.transform.localPosition-=new Vector3(b.center.x,b.min.y,b.center.z);
        sizeFrame.localScale=new Vector3(3.6f/b.size.x,1.08f/b.size.y,1.35f/b.size.z);
        var cloth=ImportedMaterial("Sofa textile",null,null,new Color(.30f,.34f,.39f),.18f);
        var cushion=ImportedMaterial("Sofa cushions",null,null,new Color(.44f,.47f,.50f),.20f);
        foreach(var r in go.GetComponentsInChildren<Renderer>())if(r.enabled)
        {
            var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)
                if(livingRoom||mats[i]==null||mats[i].mainTexture==null)mats[i]=(r.name.Contains(".007")||r.name.Contains(".008"))?cushion:cloth;
            r.sharedMaterials=mats;
        }
        var c=anchor.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.54f,0);c.size=new Vector3(3.6f,1.08f,1.35f);return true;
    }
    void SpawnCoastalPedestrian(Vector3 p, float length)
    {
        var prefab=Resources.Load<GameObject>("ExternalArt/People/Nathan");if(prefab==null)return;
        var anchor=new GameObject("Coastal pedestrian").transform;anchor.SetParent(root,false);anchor.position=p;
        var go=Instantiate(prefab,anchor,false);
        foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
        if(!FitArt(go,new Vector3(1.4f,1.8f,1.4f),0)){RemoveArt(anchor.gameObject);return;}
        var tex=Resources.Load<Texture2D>("ExternalArt/People/Nathan_Albedo");var norm=Resources.Load<Texture2D>("ExternalArt/People/Nathan_Normal");
        if(tex!=null){var m=ImportedMaterial("Nathan",tex,norm,Color.white,.18f);foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=m;r.sharedMaterials=mats;}}
        Animation a=go.GetComponentInChildren<Animation>();
        if(a==null||a.GetClipCount()==0){RemoveArt(anchor.gameObject);return;}
        foreach(AnimationState s in a){a.clip=s.clip;s.wrapMode=WrapMode.Loop;s.speed=1.15f;break;}
        a.wrapMode=WrapMode.Loop;a.Play();
        var walk=anchor.gameObject.AddComponent<CoastalWalker>();walk.Init(p,length);
    }
    Material MakeCoastalWater(Color deep)
    {
        var sh=Resources.Load<Shader>("ExternalArt/CoastalWater");
        if(sh==null)return TexMat("water_surface",deep,.82f,.08f);
        var m=new Material(sh);m.SetColor("_Deep",new Color(.045f,.28f,.43f));m.SetColor("_Shallow",new Color(.09f,.58f,.57f));
        var n=Resources.Load<Texture2D>("ExternalArt/Textures/water_surface_Normal");if(n!=null)m.SetTexture("_BumpMap",n);
        m.SetVector("_Island0",IslandCenter(0));m.SetVector("_Island1",IslandCenter(1));m.SetVector("_Island2",IslandCenter(2));
        m.SetVector("_HalfSize",new Vector4(IslandWidth/2f+17.9f,IslandDepth/2f+17.9f,0,0));return m;
    }
    GameObject[] importedCityPrefabs, localCityPrefabs;
    readonly System.Collections.Generic.Dictionary<string,Material> architectureMaterials = new System.Collections.Generic.Dictionary<string,Material>();
    Material ArchitectureMaterial(string name)
    {
        string k=name.ToLowerInvariant();
        if(architectureMaterials.TryGetValue(k,out Material material))return material;
        if(k.Contains("glass"))material=TexMat("facade_glass",new Color(.68f,.80f,.87f),.88f,.20f);
        else if(k.Contains("brick"))material=TexMat("brick",new Color(.95f,.87f,.81f),.10f);
        else if(k.Contains("stucco"))material=TexMat("plaster",new Color(.94f,.88f,.75f),.11f);
        else if(k.Contains("stone"))material=TexMat("concrete",new Color(.84f,.83f,.78f),.14f);
        else if(k.Contains("roof"))material=TexMat("brick",new Color(.61f,.38f,.28f),.12f);
        else if(k.Contains("wood"))material=TexMat("wood",new Color(.56f,.44f,.32f),.23f);
        else material=TexMat("metal",new Color(.26f,.28f,.29f),.34f,.4f);
        architectureMaterials[k]=material;return material;
    }
    bool ImportedCityBuilding(Vector3 ctr,float half,float height)
    {
        var prefabs=importedCityPrefabs??(importedCityPrefabs=Resources.LoadAll<GameObject>("ExternalArt/City"));
        bool local=prefabs.Length==0;
        if(local)prefabs=localCityPrefabs??(localCityPrefabs=Resources.LoadAll<GameObject>("ExternalArt/CityLocal"));
        if(prefabs.Length==0)return false;
        int index=(int)(Mathf.Abs(ctr.x*31f+ctr.z*17f)/4f)%prefabs.Length;
        var anchor=new GameObject(local?"Detailed coastal architecture":"Imported city building").transform;
        anchor.SetParent(scn,false);anchor.localPosition=ctr;
        var go=Instantiate(prefabs[index],anchor,false);foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
        if(local)
        {
            // Preserve the footprint even when a land's original building was shorter or taller.
            // Generated meshes have their vertical axis aligned with the parent.
            var t=go.transform;t.localRotation=Quaternion.Euler(0,90f*(index%4),0);
            if(!ArtBounds(anchor,go,out Bounds bounds)){RemoveArt(anchor.gameObject);return false;}
            float horizontal=Mathf.Min(half*2/bounds.size.x,half*2/bounds.size.z);
            t.localScale=Vector3.Scale(t.localScale,new Vector3(horizontal,height/bounds.size.y,horizontal));
            if(!ArtBounds(anchor,go,out bounds)){RemoveArt(anchor.gameObject);return false;}
            t.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        }
        else if(!FitArt(go,new Vector3(half*2,height,half*2),90f*(index%4))){RemoveArt(anchor.gameObject);return false;}
        if(local)foreach(var r in go.GetComponentsInChildren<Renderer>())
        {var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]!=null)mats[i]=ArchitectureMaterial(mats[i].name);r.sharedMaterials=mats;}
        // Keep the blocked native grid and actual building collision consistent.
        var col=anchor.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,height/2,0);col.size=new Vector3(half*2,height,half*2);
        return true;
    }
}
