using System;
using UnityEditor;
using UnityEngine;

// Serialize frame references through Unity, rather than relying on hand-written Unity YAML.
[InitializeOnLoad]
public static class WallDestructionAssetRepair
{
    [Serializable] sealed class Entry {public string file;public float x,y,width,height;}
    [Serializable] sealed class Manifest {public Entry[] frames;}
    static WallDestructionAssetRepair(){EditorApplication.delayCall+=Ensure;}
    public static void Ensure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var s=WallDestructionSettings.Load();if(s==null||(s.IsValid&&s.dataRevision==2))return;
        var text=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TCardWallDestruction/frame-manifest.json");if(text==null)return;
        var manifest=JsonUtility.FromJson<Manifest>(text.text);if(manifest?.frames==null||manifest.frames.Length!=96)return;
        var frames=new Texture2D[manifest.frames.Length];var rectangles=new Rect[frames.Length];
        for(int i=0;i<frames.Length;i++)
        {
            var e=manifest.frames[i];rectangles[i]=new Rect(e.x,e.y,e.width,e.height);
            if(e.width<=0||e.height<=0)continue;
            frames[i]=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/TCardWallDestruction/Frames/"+e.file);
            if(frames[i]==null){Debug.LogError("Missing wall destruction frame: "+e.file);return;}
        }
        s.frames=frames;s.rectangles=rectangles;s.dataRevision=2;EditorUtility.SetDirty(s);AssetDatabase.SaveAssetIfDirty(s);
        Debug.Log("Wall destruction: native frame references and crop rectangles repaired.");
    }
}

