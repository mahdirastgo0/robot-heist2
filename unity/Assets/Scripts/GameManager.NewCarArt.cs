using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameManager
{
    [Serializable] sealed class WheelTable { public WheelPosition[] wheels; }
    [Serializable] sealed class WheelPosition { public float x,y,z,radius; }
    [Serializable] sealed class NewCarMaterialTable { public NewCarSurface[] materials; }
    [Serializable] sealed class NewCarSurface
    {
        public string name,texture;
        public float r,g,b,smoothness,metallic;
    }
    sealed class NewCarPart { public Mesh mesh; public Material material; public int wheel=-1; public bool knuckle; public bool paint; }
    // Shared assets: every instance references the same meshes and materials.
    readonly Dictionary<string,NewCarPart[]> newCarCache=new Dictionary<string,NewCarPart[]>();
    NewCarPart[] LoadNewCar(string id)
    {
        if(newCarCache.TryGetValue(id,out NewCarPart[] cached))return cached;
        string path="ExternalArt/NewVehicles/"+id+"/";
        TextAsset model=Resources.Load<TextAsset>(path+id), json=Resources.Load<TextAsset>(path+"materials");
        if(model==null||json==null){Debug.LogError("Missing new car art: "+id);return null;}
        var created=new List<NewCarPart>();
        try
        {
            var table=JsonUtility.FromJson<NewCarMaterialTable>(json.text);
            var materials=new Dictionary<string,NewCarSurface>();
            foreach(var item in table.materials)materials.Add(item.name,item);
            using(var stream=new MemoryStream(model.bytes,false))using(var br=new BinaryReader(stream))
            {
                if(br.ReadByte()!=82||br.ReadByte()!=72||br.ReadByte()!=67||br.ReadByte()!=49)throw new InvalidDataException("Invalid vehicle model header");
                int count=br.ReadInt32();if(count<=0||count>128)throw new InvalidDataException("Invalid part count");
                for(int part=0;part<count;part++)
                {
                    int nameLength=br.ReadInt32();if(nameLength<1||nameLength>256)throw new InvalidDataException("Invalid material name");
                    string label=System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLength));
                    int nv=br.ReadInt32(),nt=br.ReadInt32();
                    if(nv<3||nv>1000000||nt<1||nt>1500000||32L*nv+12L*nt>stream.Length-stream.Position)throw new InvalidDataException("Invalid geometry counts");
                    var v=new Vector3[nv];var n=new Vector3[nv];var uv=new Vector2[nv];var tris=new int[nt*3];
                    for(int j=0;j<nv;j++)
                    {v[j]=new Vector3(br.ReadSingle(),br.ReadSingle(),br.ReadSingle());n[j]=new Vector3(br.ReadSingle(),br.ReadSingle(),br.ReadSingle());uv[j]=new Vector2(br.ReadSingle(),br.ReadSingle());}
                    for(int j=0;j<tris.Length;j++){tris[j]=br.ReadInt32();if(tris[j]<0||tris[j]>=nv)throw new InvalidDataException("Invalid triangle index");}
                    string[] segments=label.Split('|');
                    string materialName=segments[0];
                    int wheel=segments.Length==3?int.Parse(segments[1]):-1;
                    bool knuckle=segments.Length==3&&segments[2]=="1";
                    if(wheel < -1 || wheel > 3)throw new InvalidDataException("Invalid wheel index");
                    if(!materials.TryGetValue(materialName,out NewCarSurface surface))throw new InvalidDataException("Missing material "+label);
                    var material=new Material(PipeShader()){name=materialName};
                    Color color=new Color(surface.r,surface.g,surface.b,1f);
                    if(material.HasProperty("_Color"))material.SetColor("_Color",color);
                    if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
                    if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",surface.metallic);
                    if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",surface.smoothness);
                    if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",surface.smoothness);
                    if(!string.IsNullOrEmpty(surface.texture))
                    {
                        var tex=Resources.Load<Texture2D>(path+Path.GetFileNameWithoutExtension(surface.texture));
                        if(tex!=null){if(material.HasProperty("_MainTex"))material.SetTexture("_MainTex",tex);if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",tex);}
                    }
                    var mesh=new Mesh(){name=label,indexFormat=nv>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                    mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.triangles=tris;mesh.RecalculateBounds();mesh.UploadMeshData(true);
                    created.Add(new NewCarPart{mesh=mesh,material=material,wheel=wheel,knuckle=knuckle,paint=materialName=="G63_Carpaint"||materialName=="GT3_body"||materialName=="GT3_Mat_White_Inst"||materialName=="Paint_base"||materialName=="1CarPaintClearco"});
                }
                if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing model data");
            }
            cached=created.ToArray();newCarCache[id]=cached;return cached;
        }
        catch(Exception e)
        {
            foreach(var p in created){Destroy(p.mesh);Destroy(p.material);}
            Debug.LogError("New car art failed: "+id+" : "+e.Message);return null;
        }
    }
    bool SpawnNewCarArt(Transform parent,int type,bool parked,DriveCar car=null)
    {
        string id=type==5?"Pickup":type==6?"EVO":type<=1?"G63":"GT3";
        var parts=LoadNewCar(id);if(parts==null)return false;
        var model=new GameObject(id+" source model");model.transform.SetParent(parent,false);
        var renderers=new List<Renderer>();
        var wheelSteer=new Transform[4];var wheelRoll=new Transform[4];
        bool suv=id=="G63";
        var pivotFile=Resources.Load<TextAsset>("ExternalArt/NewVehicles/"+id+"/pivots");
        var table=pivotFile!=null?JsonUtility.FromJson<WheelTable>(pivotFile.text):null;
        if((id=="EVO"||id=="Pickup")&&(table==null||table.wheels==null||table.wheels.Length!=4)){Destroy(model);Debug.LogError("Missing wheel centers: "+id);return false;}
        for(int i=0;i<4;i++)
        {
            bool front=i<2;float x=(i%2==0?-1f:1f)*(suv ? .705f : .72f);
            float y=suv ? .335f : front ? .315f : .33f;
            float z=suv?(front?1.28f:-1.15f):(front?1.12f:-1.105f);
            float radius=suv ? .338f : front ? .315f : .33f;
            if(table!=null&&table.wheels!=null&&table.wheels.Length==4){var w=table.wheels[i];x=w.x;y=w.y;z=w.z;radius=w.radius;}
            var pivot=new GameObject("Wheel steering "+i).transform;pivot.SetParent(model.transform,false);pivot.localPosition=new Vector3(x,y,z);
            var roll=new GameObject("Wheel rolling "+i).transform;roll.SetParent(pivot,false);
            wheelSteer[i]=pivot;wheelRoll[i]=roll;
            if(car!=null)car.AddAnimatedWheel(pivot,roll,front,radius);
        }
        var paintTargets=new List<Renderer>();
        foreach(var part in parts)
        {
            var child=new GameObject(part.mesh.name);child.transform.SetParent(part.wheel<0?model.transform:part.knuckle?wheelSteer[part.wheel]:wheelRoll[part.wheel],false);
            child.AddComponent<MeshFilter>().sharedMesh=part.mesh;
            var mr=child.AddComponent<MeshRenderer>();mr.sharedMaterial=part.material;
            mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;renderers.Add(mr);if(part.paint&&part.wheel<0)paintTargets.Add(mr);
        }
        if(!parked&&car!=null)car.SetPaintTargets(paintTargets.ToArray(),VehiclePaintIndex(type),VehiclePaintColors);
        // Parked meshes are culled only at long distances; the drivable mesh is never culled by this group.
        if(parked)
        {
            var lod=model.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,renderers.ToArray())});lod.RecalculateBounds();
        }
        return true;
    }
}
