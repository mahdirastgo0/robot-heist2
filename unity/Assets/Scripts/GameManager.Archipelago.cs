using UnityEngine;

public partial class GameManager
{
    Material seaArtMat;
    const float BeachWidth = 10f;
    int IslandStart(int island) => island * (curLand.bx * 6 + 12);
    float IslandWidth => curLand.bx * 6 * Cell;
    float IslandDepth => curLand.by * 6 * Cell;
    Vector3 IslandCenter(int island) => new Vector3(IslandStart(island) * Cell + IslandWidth / 2f, 0f, -IslandDepth / 2f);
    bool IslandCell(int x) => x >= 0 && x <= 2 * IslandStart(1) + curLand.bx * 6 && x % IslandStart(1) <= curLand.bx * 6;
    Vector3 BridgeCenter(int island) => new Vector3(IslandCenter(island).x + IslandWidth / 2f + 24f, 0f, -ExitRow * Cell);
    public bool DryLand(Vector3 p, float margin = 0f)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 c = IslandCenter(i);
            if (Mathf.Abs(p.x-c.x) <= IslandWidth/2f+7f+BeachWidth-margin && Mathf.Abs(p.z-c.z) <= IslandDepth/2f+7f+BeachWidth-margin) return true;
            if (i < 2) { c = BridgeCenter(i); if (Mathf.Abs(p.x-c.x) <= 24f && Mathf.Abs(p.z-c.z) <= 5f-margin) return true; }
        }
        return false;
    }
    void BuildArchipelago(System.Random rng)
    {
        Vector3 center = MapCenterWorld();
        Color deep = Color.Lerp(new Color(0.06f, 0.27f, 0.44f), curLand.fog, 0.18f);
        seaArtMat=MakeCoastalWater(deep);
        TexBox(center+Vector3.down*1.1f,new Vector3(1800f,.2f,1400f),seaArtMat,12f,false,root);
        waveABase = center + Vector3.down * .98f; waveBBase = center + Vector3.down * .95f;
        if(seaArtMat.shader.name!="RobotHeist/CoastalWater")
        {waveA=MakeWaveLayer(waveABase.y,WaveTileA,new Color(.85f,.95f,1f,.18f),center);waveB=MakeWaveLayer(waveBBase.y,WaveTileB,new Color(.7f,.9f,1f,.12f),center);}
        var sand = TexMat("sand", Color.white, .03f);
        Color wet = new Color(.64f,.53f,.35f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 c = IslandCenter(i);
            float sx = IslandWidth + 34f, sz = IslandDepth + 34f;
            TexBox(c + Vector3.down * .19f, new Vector3(sx,.3f,sz), sand, 5f, true);
            // Lower wet-sand ledge and foam mark the waterline without a fence.
            TexBox(c+Vector3.down*.53f,new Vector3(sx+1.8f,.35f,sz+1.8f),TexMat("sand",wet,.22f),5f,true,root);
            SpawnCoastalPedestrian(c+new Vector3(-IslandWidth/2f+9f,0f,IslandDepth/2f+13f),IslandWidth-18f);
            // Promenade trees and beach benches, outside the road grid.
            for (int j = 0; j < 5; j++)
            {
                float x = c.x - IslandWidth/2f + 8f + j*(IslandWidth-16f)/4f;
                MakeBench(new Vector3(x,0,c.z+IslandDepth/2f+10f),0);
                SpawnTree(new Vector3(x+2f,0,c.z-IslandDepth/2f-10f),rng.Next(360),.8f,rng);
            }
            Sign3D(new[]{"CENTRAL ISLAND","PALM BEACH","EAST HARBOR"}[i], c + new Vector3(0,3.8f,IslandDepth/2f+8f),0,12f,1.3f,Color.white);
            if (i == 2) continue;
            Vector3 b = BridgeCenter(i);
            TexBox(b + Vector3.down*.18f,new Vector3(48f,.36f,10f),TexMat("asphalt",Color.Lerp(curLand.asphalt,Color.white,.35f),.08f),3.5f,true);
            foreach(float z in new[]{-4.8f,4.8f})
            {
                MakeRail(b+new Vector3(0,0,z),48f,true,new Color(.24f,.27f,.3f));
                Box(b+new Vector3(0,.65f,z),new Vector3(48f,1.3f,.23f),new Color(.32f,.35f,.38f),true);
                for(int x=-18;x<=18;x+=12) Box(b+new Vector3(x,-1.6f,z*.65f),new Vector3(1.5f,3.2f,1.5f),new Color(.49f,.49f,.48f));
            }
            for(int x=-22;x<=22;x+=4) Box(b+new Vector3(x,.02f,0),new Vector3(2f,.025f,.13f),new Color(.9f,.82f,.45f));
        }
        for(int i=0;i<8;i++) MakeBoat(new Vector3(center.x-100f+i*65f,0,22f+rng.Next(8,45)),rng.Next(360),new Color(.83f,.84f,.86f),i%2==0);
    }
    Vector2Int NearestWalkCell(Vector2Int c)
    {
        c = new Vector2Int(Mathf.Clamp(c.x,0,CW-1), Mathf.Clamp(c.y,0,CH-1));
        if (blocked == null || !blocked[c.x,c.y]) return c;
        for(int r=1;r<=Mathf.Max(CW,CH);r++)
            for(int dy=-r;dy<=r;dy++)
            {
                int dx=r-Mathf.Abs(dy);
                foreach(int x in new[]{c.x-dx,c.x+dx})
                { int y=c.y+dy; if(x>=0 && y>=0 && x<CW && y<CH && !blocked[x,y]) return new Vector2Int(x,y); }
            }
        return c;
    }
}
