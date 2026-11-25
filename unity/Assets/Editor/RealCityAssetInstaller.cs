#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEditor;
using UnityEngine;

// Optional, user-triggered download of individually free CC0 models. No web requests in Play Mode.
public static class RealCityAssetInstaller
{
    const string Folder="Assets/Resources/ExternalArt/City";
    static readonly string[] Names={"brownstone-house","colonial-house"};
    [MenuItem("Robot Heist/Art/Download free detailed buildings (Grab3D)")]
    static void Download()
    {
        if(EditorApplication.isPlaying)return;
        if(!EditorUtility.DisplayDialog("Install detailed buildings","Download the two free textured OBJ packs (CC0) from Grab3D and use them in residential blocks?","Download","Cancel"))return;
        int installed=0;
        try
        {
            for(int i=0;i<Names.Length;i++)
            {
                string name=Names[i],url="https://grab3d.com/dl/"+name+"/"+name+"-obj.zip";
                EditorUtility.DisplayProgressBar("Downloading free city models",name,(float)i/Names.Length);
                string temp=Path.GetTempFileName();
                try
                {
                    using(var client=new WebClient()){client.Headers[HttpRequestHeader.UserAgent]="RobotHeist Unity asset installer";client.DownloadFile(url,temp);}
                    ImportArchive(temp,name,"https://grab3d.com/models/buildings/"+name+"/");installed++;
                }
                catch(Exception e){Debug.LogWarning("Could not install "+name+": "+e.Message);}
                finally{File.Delete(temp);}
            }
        }
        finally{EditorUtility.ClearProgressBar();AssetDatabase.Refresh();}
        EditorUtility.DisplayDialog("City art",installed+" of "+Names.Length+" models installed. Newly imported models are selected on the next city load. If a download fails, download its OBJ ZIP from Grab3D and use the local ZIP importer.","OK");
    }
    [MenuItem("Robot Heist/Art/Import detailed building OBJ ZIP")]
    static void Local()
    {
        if(EditorApplication.isPlaying)return;
        string file=EditorUtility.OpenFilePanel("Choose a single textured building OBJ ZIP","","zip");
        if(string.IsNullOrEmpty(file))return;
        try{ImportArchive(file,Path.GetFileNameWithoutExtension(file),"User supplied; verify license before distributing.");AssetDatabase.Refresh();}
        catch(Exception e){Debug.LogError("Building import failed: "+e.Message);}
    }
    static void ImportArchive(string file,string name,string source)
    {
        string folder=Path.GetFullPath(Path.Combine(Folder,name));
        string stage=Path.Combine(Path.GetTempPath(),"rh-city-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        try
        {
            int models=0;long total=0;
            using(var fs=File.OpenRead(file))using(var zip=new ZipArchive(fs,ZipArchiveMode.Read))
            foreach(var entry in zip.Entries)
            {
                string relative=entry.FullName.Replace('\\','/');string ext=Path.GetExtension(relative).ToLowerInvariant();
                if(ext!=".obj"&&ext!=".mtl"&&ext!=".fbx"&&ext!=".png"&&ext!=".jpg"&&ext!=".jpeg"&&ext!=".tga"&&ext!=".txt"&&ext!=".json")continue;
                if(entry.Length<=0||entry.Length>70L*1024*1024)continue;
                total+=entry.Length;if(total>200L*1024*1024)throw new IOException("Asset archive too large");
                string target=Path.GetFullPath(Path.Combine(stage,relative));
                if(!target.StartsWith(stage+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Unsafe archive path");
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                using(var input=entry.Open())using(var output=File.Create(target))input.CopyTo(output);
                if(ext==".obj"||ext==".fbx")models++;
            }
            if(models==0)throw new IOException("No OBJ or FBX model in archive");
            Directory.CreateDirectory(folder);
            foreach(var path in Directory.GetFiles(stage,"*",SearchOption.AllDirectories))
            {
                string dest=Path.Combine(folder,path.Substring(stage.Length+1));Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(path,dest,true);
            }
            File.WriteAllText(Path.Combine(folder,"SOURCE.txt"),source+"\n"+(source.StartsWith("https://grab3d.com/")?"Grab3D, CC0 1.0.\n":""));
        }
        finally{Directory.Delete(stage,true);}
    }
}
#endif
