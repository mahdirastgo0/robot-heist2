#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// تنظیمات خودکار import برای مدل‌های Resources/Models
public class ModelImportSettings : AssetPostprocessor
{
    public override uint GetVersion() { return 4; }
    static bool Ours(string p) { return p.Contains("/Resources/Models/"); }

    void OnPreprocessModel()
    {
        bool ext=assetPath.Contains("/Resources/ExternalArt/");
        if (!Ours(assetPath) && !ext) return;
        var mi = (ModelImporter)assetImporter;
        if(ext)
        {
            bool person=assetPath.Contains("/ExternalArt/People/");
            bool customPlayer=assetPath.Contains("/People/CustomPlayer/");
            mi.importAnimation=person&&!customPlayer;mi.animationType=customPlayer?ModelImporterAnimationType.Generic:person?ModelImporterAnimationType.Legacy:ModelImporterAnimationType.None;
            mi.importCameras=false;mi.importLights=false;mi.importBlendShapes=false;
            mi.optimizeGameObjects=false;mi.importNormals=assetPath.Contains("/ExternalArt/CityLocal/")?ModelImporterNormals.Calculate:ModelImporterNormals.Import;
            mi.importTangents=ModelImporterTangents.CalculateMikk;
            mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            return;
        }
        mi.importBlendShapes = false;                   // حالت‌های صورت لازم نیست (سبک‌تر)
        mi.importAnimation = false;
        // انیمیشن آماده نداریم؛ حرکت استخوان‌ها رو خودمون با BonePoser میدیم (نوع Generic میمونه تا استخوان‌ها سالم بمونن)
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.optimizeGameObjects = false;
        mi.importCameras = false;
        mi.importLights = false;
    }

    static void tiExternal(TextureImporter ti)
    {
        string path=ti.assetPath;
        ti.maxTextureSize=2048; ti.anisoLevel=8; ti.mipmapEnabled=true;
        ti.wrapMode=TextureWrapMode.Repeat; ti.textureCompression=TextureImporterCompression.CompressedHQ;
        if(path.Contains("_Normal"))ti.textureType=TextureImporterType.NormalMap;
        if(path.Contains("_MetalSmooth")||path.Contains("_AO")) { ti.sRGBTexture=false; ti.alphaSource=TextureImporterAlphaSource.FromInput; }
    }
    void OnPreprocessTexture()
    {
        bool external = assetPath.Contains("/Resources/ExternalArt/");
        if (!Ours(assetPath) && !assetPath.Contains("/Resources/Textures/") && !external) return;
        if(external) { tiExternal((TextureImporter)assetImporter); return; }
        var ti = (TextureImporter)assetImporter;
        if (assetPath.Contains("_Normal")) ti.textureType = TextureImporterType.NormalMap;
        if (assetPath.Contains("sky_panorama"))
        {   // آسمون: بدون mipmap و بدون فشرده‌سازی شدید تا واضح باشه
            ti.wrapModeU = TextureWrapMode.Repeat; ti.wrapModeV = TextureWrapMode.Clamp;
            ti.mipmapEnabled = false; ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            return;
        }
        ti.anisoLevel = 8;                                   // زمین از زاویه‌ی کم واضح بمونه
        if (assetPath.Contains("/Resources/Textures/")) ti.maxTextureSize = 2048;
    }
}
#endif
