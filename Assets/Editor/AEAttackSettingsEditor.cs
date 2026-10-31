using UnityEditor;
using UnityEngine;

public static class AEAttackSettingsEditor
{
    [MenuItem("Tools/T-card/AE Attack Effect Settings",false,-4)]
    public static void Open()
    {
        Selection.activeObject=Resources.Load<AEAttackSettings>("TCardAttackEffects/AEAttackSettings");
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}
public sealed class AEAttackFrameImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/TCardAttackEffects/Frames/",System.StringComparison.Ordinal))return;
        Configure((TextureImporter)assetImporter);
    }
    public static void Configure(TextureImporter t)
    {
        t.textureType=TextureImporterType.Default;t.sRGBTexture=false;t.alphaSource=TextureImporterAlphaSource.None;
        t.alphaIsTransparency=false;t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;t.isReadable=false;
        t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=4096;
        t.textureCompression=TextureImporterCompression.CompressedHQ;t.compressionQuality=100;
        t.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=4096,format=TextureImporterFormat.BC4,compressionQuality=100});
    }
}
