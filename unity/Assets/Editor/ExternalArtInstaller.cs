#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

// User-initiated editor-only downloads; no network requests in the game.
public static class ExternalArtInstaller
{
    const string Folder = "Assets/Resources/ExternalArt/Textures/";
    static readonly string[,] Sources = {
        {"asphalt","asphalt_02"}, {"sand","sand_02"}, {"brick","red_brick"},
        {"concrete","concrete_wall_009"}, {"plaster","concrete_wall_004"},
        {"wood","wooden_planks"}, {"metal","metal_plate_03"},
        {"pavement","concrete_brick_wall_001"}, {"floor_wood","oak_wood_planks"}
    };
    static byte[] Fetch(string asset,string map)
    {
        string url="https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/"+asset+"/"+asset+"_"+map+"_2k.jpg";
        var req=(HttpWebRequest)WebRequest.Create(url);
        req.Timeout=20000; req.ReadWriteTimeout=20000; req.UserAgent="RobotHeist-Unity-ArtInstaller/1.0";
        using(var response=req.GetResponse()) using(var stream=response.GetResponseStream()) using(var ms=new MemoryStream())
        { stream.CopyTo(ms); if(ms.Length>32*1024*1024)throw new IOException("Unexpected file size"); return ms.ToArray(); }
    }
    static Texture2D Decode(byte[] bytes)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!ImageConversion.LoadImage(t,bytes)) { UnityEngine.Object.DestroyImmediate(t); throw new IOException("Invalid image"); }
        return t;
    }
    static void SaveJpg(string path,byte[] bytes)
    { var t=Decode(bytes); UnityEngine.Object.DestroyImmediate(t); File.WriteAllBytes(path,bytes); }
    static void SaveSmooth(string path, byte[] rough, bool metal)
    {
        var t=Decode(rough); var pixels=t.GetPixels32(); byte m=(byte)(metal?160:0);
        for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(m,0,0,(byte)(255-pixels[i].r));
        t.SetPixels32(pixels); t.Apply(); File.WriteAllBytes(path,ImageConversion.EncodeToPNG(t)); UnityEngine.Object.DestroyImmediate(t);
    }
    [MenuItem("Robot Heist/Art/Download CC0 PBR textures (2K)")]
    public static void Install()
    {
        if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("Robot Heist","Stop Play Mode first.","OK");return;}
        if(!EditorUtility.DisplayDialog("Install optional Poly Haven textures","Downloads nine CC0 material sets from Poly Haven. Existing built-in textures remain as fallbacks. Requires internet and may take several minutes. Continue?","Download","Cancel"))return;
        Directory.CreateDirectory(Folder); int success=0; bool cancelled=false;
        try
        {
            for(int i=0;i<Sources.GetLength(0);i++)
            {
                string name=Sources[i,0], asset=Sources[i,1];
                if(EditorUtility.DisplayCancelableProgressBar("Poly Haven CC0",asset,(float)i/Sources.GetLength(0))){cancelled=true;break;}
                try
                {
                    byte[] diffuse=Fetch(asset,"diff"), normal=Fetch(asset,"nor_gl"), rough=Fetch(asset,"rough");
                    SaveJpg(Folder+name+"_Albedo.jpg",diffuse); SaveJpg(Folder+name+"_Normal.jpg",normal);
                    SaveSmooth(Folder+name+"_MetalSmooth.png",rough,name=="metal"); success++;
                    try { SaveJpg(Folder+name+"_AO.jpg",Fetch(asset,"ao")); } catch(Exception e) { Debug.LogWarning(asset+" AO unavailable: "+e.Message); }
                }
                catch(Exception e){Debug.LogWarning("Optional material "+asset+" failed; existing art retained: "+e.Message);}
            }
        }
        finally {EditorUtility.ClearProgressBar();AssetDatabase.Refresh();}
        EditorUtility.DisplayDialog("Robot Heist",success+"/"+Sources.GetLength(0)+" materials installed. "+(cancelled?"Cancelled. ":"")+"See Console for errors. Start Play Mode to use the available materials.","OK");
    }
    [MenuItem("Robot Heist/Art/Open free CC0 model library")]
    static void OpenModels(){Application.OpenURL("https://polyhaven.com/models");}
}
#endif
