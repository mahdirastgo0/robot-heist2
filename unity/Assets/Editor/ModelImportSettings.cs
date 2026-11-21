#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// تنظیمات خودکار import برای مدل‌های Resources/Models
public class ModelImportSettings : AssetPostprocessor
{
    static bool Ours(string p) { return p.Contains("/Resources/Models/"); }

    void OnPreprocessModel()
    {
        if (!Ours(assetPath)) return;
        var mi = (ModelImporter)assetImporter;
        mi.importBlendShapes = false;                   // حالت‌های صورت لازم نیست (سبک‌تر)
        mi.importAnimation = false;
        // انیمیشن آماده نداریم؛ حرکت استخوان‌ها رو خودمون با BonePoser میدیم (نوع Generic میمونه تا استخوان‌ها سالم بمونن)
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.optimizeGameObjects = false;
        mi.importCameras = false;
        mi.importLights = false;
    }

    void OnPreprocessTexture()
    {
        if (!Ours(assetPath) && !assetPath.Contains("/Resources/Textures/")) return;
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
