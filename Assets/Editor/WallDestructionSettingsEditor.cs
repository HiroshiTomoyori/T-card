using UnityEditor;
using UnityEngine;

public static class WallDestructionSettingsEditor
{
    [MenuItem("Tools/T-card/Wall Destruction Settings",false,-3)]
    public static void Open(){Selection.activeObject=WallDestructionSettings.Load();EditorGUIUtility.PingObject(Selection.activeObject);}
}
public sealed class WallDestructionFrameImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(assetPath.StartsWith("Assets/Resources/TCardWallDestruction/Frames/",System.StringComparison.Ordinal))
        { var t=(TextureImporter)assetImporter;t.textureShape=TextureImporterShape.Texture2D;AEAttackFrameImporter.Configure(t); }
    }
}

