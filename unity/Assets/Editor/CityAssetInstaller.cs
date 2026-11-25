#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;

// Optional official Kenney CC0 pack; user-initiated, never downloads while playing.
public static class CityAssetInstaller
{
    const string PackUrl="https://kenney.nl/media/pages/assets/city-kit-commercial/a742d900eb-1753115042/kenney_city-kit-commercial_2.1.zip";
    const string Folder="Assets/Resources/ExternalArt/City";
    [MenuItem("Robot Heist/Art/Install Kenney CC0 commercial buildings")]
    static void Download()
    {
        if(EditorApplication.isPlaying)return;
        if(!EditorUtility.DisplayDialog("Install free city models","Download the official Kenney CC0 City Kit (Commercial) and install its FBX models?","Download","Cancel"))return;
        string temp=Path.GetTempFileName();
        try
        {
            using(var wc=new WebClient())wc.DownloadFile(PackUrl,temp);
            Install(temp);
        }
        catch(Exception e){Debug.LogWarning("City model download failed: "+e.Message);EditorUtility.DisplayDialog("Download unavailable","Download the ZIP from the official Kenney page, then use Robot Heist > Art > Import local Kenney city ZIP.","OK");Application.OpenURL("https://kenney.nl/assets/city-kit-commercial");}
        finally{File.Delete(temp);}
    }
    [MenuItem("Robot Heist/Art/Import local Kenney city ZIP")]
    static void Local()
    {
        if(EditorApplication.isPlaying)return;string p=EditorUtility.OpenFilePanel("Select Kenney City Kit (Commercial) ZIP","","zip");if(string.IsNullOrEmpty(p))return;
        try{Install(p);}catch(Exception e){Debug.LogError("City pack import failed: "+e.Message);}
    }
    static void Install(string zip)
    {
        Directory.CreateDirectory(Folder);int count=0;
        using(var input=File.OpenRead(zip))using(var z=new ZipArchive(input,ZipArchiveMode.Read))foreach(var e in z.Entries)
        {
            string name=e.FullName.Replace('\\','/');string file=Path.GetFileName(name);string ext=Path.GetExtension(file).ToLowerInvariant();
            if(ext!=".fbx"&&ext!=".png"&&ext!=".jpg")continue;
            if(ext==".fbx"&&(name.IndexOf("FBX",StringComparison.OrdinalIgnoreCase)<0||file.StartsWith(".")))continue;
            if(ext!=".fbx"&&name.IndexOf("texture",StringComparison.OrdinalIgnoreCase)<0)continue;
            if(e.Length>40*1024*1024||e.Length<=0)continue;
            // Keep only safe file names; archives cannot write outside the asset folder.
            if(file.IndexOfAny(Path.GetInvalidFileNameChars())>=0||file=="."||file=="..")continue;
            using(var src=e.Open())using(var dst=File.Create(Path.Combine(Folder,file)))src.CopyTo(dst);
            if(ext==".fbx")count++;
        }
        if(count==0)throw new IOException("No FBX models found in this archive.");
        File.WriteAllText(Path.Combine(Folder,"SOURCE.txt"),"Kenney City Kit (Commercial), Creative Commons CC0 1.0. https://kenney.nl/assets/city-kit-commercial\n");
        AssetDatabase.Refresh();EditorUtility.DisplayDialog("City kit",count+" FBX files imported. New non-interactive buildings will use these models on next map generation.","OK");
    }
    [MenuItem("Robot Heist/Art/Import exported couch FBX")]
    static void Couch()
    {
        if(EditorApplication.isPlaying)return;string p=EditorUtility.OpenFilePanel("Select couch exported from Blender","","fbx");if(string.IsNullOrEmpty(p))return;
        File.Copy(p,"Assets/Resources/ExternalArt/Models/Couch.fbx",true);AssetDatabase.Refresh();
    }
}
#endif
