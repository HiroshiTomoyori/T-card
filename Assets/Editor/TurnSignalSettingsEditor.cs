using UnityEditor;
using UnityEngine;

public static class TurnSignalSettingsEditor
{
    public const string Path = "Assets/Resources/TCardTurnSignals/TurnSignalSettings.asset";
    [MenuItem("Tools/T-card/Turn Signal Settings", false, -5)]
    public static void Open()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<TurnSignalSettings>(Path);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}

public sealed class TurnSignalFrameImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/TCardTurnSignals/Frames/", System.StringComparison.Ordinal)) return;
        Configure((TextureImporter)assetImporter);
    }
    public static void Configure(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.compressionQuality = 100;
        // Desktop BC7 preserves the text/alpha much better than automatic DXT5.
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
            name = "Standalone", overridden = true, maxTextureSize = 2048,
            format = TextureImporterFormat.BC7, compressionQuality = 100
        });
    }
}
