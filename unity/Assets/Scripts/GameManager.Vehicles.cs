using UnityEngine;

public partial class GameManager
{
    internal DriveCar personalCar;
    static readonly string[] VehicleNames = { "G63 Rental", "G63 AMG", "911 GT3", "911 GT3 Sport", "911 GT3 Track", "GHammer Pickup", "Mitsubishi Evo" };
    static readonly int[] VehiclePrices = { 0, 4500, 9500, 14500, 26000, 8500, 12000 };
    // Ownership is scoped to the server and cycle, without changing the native save format.
    string VehicleKey => "RH.vehicles."+serverSlot+"."+NativeCore.Profile64(NativeCore.Created)+"."+NativeCore.Profile(NativeCore.CycleNo);
    int VehicleMask => PlayerPrefs.GetInt(VehicleKey+".owned",1)|1;
    int VehicleSelected { get { int i=Mathf.Clamp(PlayerPrefs.GetInt(VehicleKey+".selected",0),0,VehicleNames.Length-1); return (VehicleMask & (1<<i))!=0?i:0; } }
    static readonly string[] VehiclePaintNames={"Original","Red","Blue","White","Black","Silver","Green","Orange","Purple"};
    static readonly Color[] VehiclePaintColors={Color.white,new Color(.74f,.045f,.055f),new Color(.055f,.26f,.80f),new Color(.92f,.92f,.90f),new Color(.065f,.075f,.085f),new Color(.60f,.65f,.70f),new Color(.09f,.43f,.22f),new Color(.94f,.31f,.045f),new Color(.44f,.16f,.67f)};
    string VehiclePaintKey(int type)=>VehicleKey+".paint."+type;
    int VehiclePaintIndex(int type)=>Mathf.Clamp(PlayerPrefs.GetInt(VehiclePaintKey(type),0),0,VehiclePaintNames.Length-1);
    void SetVehiclePaint(int type,int index)
    {
        PlayerPrefs.SetInt(VehiclePaintKey(type),index);PlayerPrefs.Save();
        if(personalCar!=null&&personalCar.Type==type)personalCar.SetPaint(index);
    }
    void SpawnPersonalCar()
    {
        int type=VehicleSelected;
        var go=new GameObject(VehicleNames[type]); go.transform.SetParent(root,false);
        go.transform.position=CellToWorld(new Vector2Int(2,ExitRow))+new Vector3(0,0,1.1f);
        go.transform.rotation=Quaternion.Euler(0,90,0);
        personalCar=go.AddComponent<DriveCar>(); personalCar.Init(this,type,VehicleNames[type]);
        Color paint=type==0?new Color(.15f,.65f,.8f):type==1?new Color(.22f,.37f,.24f):new Color(.8f,.12f,.15f);
        Color glass=new Color(.12f,.22f,.29f), rubber=new Color(.055f,.055f,.06f);
        var def=type<=1?PropModels.Load("Models/Props/defender"):null;
        if(ExternalVehicle(go.transform,type,personalCar)) { }
        else if(def!=null) PropModels.Spawn(def,go.transform,Vector3.zero,0,1f,true,paint,false);
        else
        {
            float low=type==2?.48f:.63f;
            ArtBox("Chassis",go.transform,new Vector3(0,low,0),new Vector3(1.75f,.52f,4.05f),CarPaint(paint));
            ArtBox("Hood",go.transform,new Vector3(0,low+.22f,1.12f),new Vector3(1.7f,.22f,1.55f),CarPaint(paint));
            ArtBox("Cabin",go.transform,new Vector3(0,low+.45f,-.3f),new Vector3(1.48f,.63f,1.85f),TexMat("vehicle_glass",glass,.91f,.08f));
            ArtBox("Roof",go.transform,new Vector3(0,low+.79f,-.3f),new Vector3(1.52f,.1f,1.8f),CarPaint(paint));
            foreach(float side in new[]{-1f,1f})
            { ArtBox("Mirror",go.transform,new Vector3(side*.94f,low+.50f,.52f),new Vector3(.20f,.14f,.25f),CarPaint(paint));
              ArtBox("Door handle",go.transform,new Vector3(side*.88f,low+.15f,-.1f),new Vector3(.035f,.045f,.23f),TexMat("vehicle_glass",glass,.65f,.4f)); }
            foreach(float x in new[]{-.9f,.9f}) foreach(float z in new[]{-1.25f,1.25f})
            {
                var hub=new GameObject("WheelPivot").transform; hub.SetParent(go.transform,false); hub.localPosition=new Vector3(x,.34f,z);
                var wheel=Prim(PrimitiveType.Cylinder,"Wheel",hub,Vector3.zero,new Vector3(.68f,.15f,.68f),rubber); wheel.transform.localRotation=Quaternion.Euler(0,0,90);
                var rim=Prim(PrimitiveType.Cylinder,"Rim",hub,new Vector3(Mathf.Sign(x)*.15f,0,0),new Vector3(.4f,.035f,.4f),new Color(.48f,.5f,.53f)); rim.transform.localRotation=Quaternion.Euler(0,0,90);
                personalCar.AddWheel(hub,z>0);
            }
            foreach(float x in new[]{-.62f,.62f})
            {
                Prim(PrimitiveType.Cube,"Headlight",go.transform,new Vector3(x,low+.12f,2.04f),new Vector3(.42f,.18f,.05f),new Color(1,.95f,.73f));
                Prim(PrimitiveType.Cube,"TailLight",go.transform,new Vector3(x,low+.12f,-2.04f),new Vector3(.42f,.18f,.05f),new Color(.8f,.04f,.06f));
            }
            Prim(PrimitiveType.Cube,"Front bumper",go.transform,new Vector3(0,.36f,2.08f),new Vector3(1.83f,.16f,.15f),rubber);
            Prim(PrimitiveType.Cube,"Rear bumper",go.transform,new Vector3(0,.36f,-2.08f),new Vector3(1.83f,.16f,.15f),rubber);
        }
        var box=go.AddComponent<BoxCollider>(); float bodyHeight=type==5?1.68f:1.55f; box.center=new Vector3(0,.045f+bodyHeight*.5f,0); box.size=new Vector3(1.85f,bodyHeight,4.3f);
    }
    void DrawVehiclePanel(float W,float H)
    {
        var r=PanelRect(W,H,740,650,"GARAGE / VEHICLE DEALER",sMid);
        Text(new Rect(r.x+28,r.y+55,680,28),"Cash: $"+NativeCore.Profile(NativeCore.Money)+"   |   One active vehicle per land",sLabel,Color.white);
        for(int i=0;i<VehicleNames.Length;i++)
        {
            float y=r.y+92+i*51; bool owned=(VehicleMask&(1<<i))!=0,selected=VehicleSelected==i;
            Fill(new Rect(r.x+24,y,692,48),new Color(.16f,.18f,.23f));
            Text(new Rect(r.x+38,y+4,420,25),VehicleNames[i]+"  /  "+(new[]{72,94,122,144,169,108,148}[i])+" km/h",sLabel,Color.white);
            Text(new Rect(r.x+38,y+25,420,20),i==0?"Free rental - always available":owned?"Owned":"$"+VehiclePrices[i],sSmall,Color.white);
            GUI.enabled=!selected && (owned || NativeCore.Profile(NativeCore.Money)>=VehiclePrices[i]);
            if(GUI.Button(new Rect(r.x+520,y+5,176,38),selected?"SELECTED":owned?"SELECT":"BUY & SELECT",sBtn))
            {
                if(!owned) { NativeCore.ProfileAdd(NativeCore.Money,-VehiclePrices[i]); PlayerPrefs.SetInt(VehicleKey+".owned",VehicleMask|(1<<i)); }
                PlayerPrefs.SetInt(VehicleKey+".selected",i); PlayerPrefs.Save(); Toast(VehicleNames[i]+" selected for your next land.");
            }
            GUI.enabled=true;
        }
        int paintType=VehicleSelected,selectedPaint=VehiclePaintIndex(paintType);
        Text(new Rect(r.x+28,r.y+463,684,24),"PAINT / "+VehicleNames[paintType]+" - "+VehiclePaintNames[selectedPaint]+" (free)",sLabel,Color.white);
        for(int i=0;i<VehiclePaintNames.Length;i++)
        {
            var swatch=new Rect(r.x+28+i*76,r.y+492,70,40);
            Color old=GUI.backgroundColor;GUI.backgroundColor=i==0?new Color(.38f,.42f,.46f):VehiclePaintColors[i];
            if(GUI.Button(swatch,selectedPaint==i?"SELECTED":"",sBtn))SetVehiclePaint(paintType,i);
            GUI.backgroundColor=old;
            Text(new Rect(swatch.x,swatch.yMax+1,swatch.width,22),VehiclePaintNames[i],sSmallC,Color.white);
        }
        Text(new Rect(r.x+24,r.y+562,690,25),"Paint is saved per vehicle. Press "+GameOptions.KeyLabel(10)+" near your car.",sSmall,Color.white);
        ClosePanelButton(r);
    }
}
