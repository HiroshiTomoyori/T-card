using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class KeepPlayModeEditsDiagnostics
{
    const string Key="TCard.KeepPlayModeEdits.Probe.";
    const string ProbeName="TCard Play Edit Validation";
    const string AssetPath="Assets/Editor/PlayModeEditValidation.asset";
    static string Report=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardPlayModeEditsValidation/result.txt");
    static KeepPlayModeEditsDiagnostics(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/T-card/Validate Keep Play Mode Edits")]
    static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        SessionState.SetBool(Key+"Enabled",KeepPlayModeEdits.Enabled);KeepPlayModeEdits.Enabled=true;
        var go=new GameObject(ProbeName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(BoxCollider));
        go.GetComponent<RectTransform>().anchoredPosition=new Vector2(11,22);
        go.GetComponent<BoxCollider>().size=new Vector3(2,3,4);
        var asset=ScriptableObject.CreateInstance<PlayModeEditProbeAsset>();AssetDatabase.CreateAsset(asset,AssetPath);AssetDatabase.SaveAssetIfDirty(asset);
        SessionState.SetString(Key+"Stage","enter");SessionState.SetFloat(Key+"Due",(float)EditorApplication.timeSinceStartup+180);
        EditorApplication.isPlaying=true;
    }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Edit(Object target,Action<SerializedObject> action)
    {using(var so=new SerializedObject(target)){action(so);so.ApplyModifiedProperties();}Undo.FlushUndoRecordObjects();}
    static void Tick()
    {
        string stage=SessionState.GetString(Key+"Stage","");if(string.IsNullOrEmpty(stage)||EditorApplication.isCompiling)return;
        if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Due",0)){Finish("FAIL: timeout");return;}
        try
        {
            if(stage=="enter"&&EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode==false)
            {
                // Delay until EnteredPlayMode has built the edit-to-play object map.
                SessionState.SetString(Key+"Stage","edit");SessionState.SetFloat(Key+"Next",(float)EditorApplication.timeSinceStartup+.6f);return;
            }
            if(stage=="edit"&&EditorApplication.isPlaying&&EditorApplication.timeSinceStartup>=SessionState.GetFloat(Key+"Next",0))
            {
                var go=GameObject.Find(ProbeName);Check(go!=null,"Unsaved test scene object lost");
                var rect=go.GetComponent<RectTransform>();var image=go.GetComponent<Image>();var box=go.GetComponent<BoxCollider>();
                var asset=AssetDatabase.LoadAssetAtPath<PlayModeEditProbeAsset>(AssetPath);
                var sprite=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("eeb82c69e8883a04fbc450ecad2628e5")).OfType<Sprite>().First();
                Edit(rect,so=>so.FindProperty("m_AnchoredPosition").vector2Value=new Vector2(31,42));
                Edit(box,so=>so.FindProperty("m_Size").vector3Value=new Vector3(5,6,7));
                Edit(image,so=>{so.FindProperty("m_Color").colorValue=new Color(.2f,.4f,.6f,.8f);so.FindProperty("m_Sprite").objectReferenceValue=sprite;});
                Edit(asset,so=>{so.FindProperty("speed").floatValue=2.25f;so.FindProperty("offset").vector3Value=new Vector3(4,5,6);so.FindProperty("tint").colorValue=Color.magenta;var a=so.FindProperty("values");a.arraySize=3;a.GetArrayElementAtIndex(2).floatValue=9;so.FindProperty("curve").animationCurveValue=AnimationCurve.EaseInOut(0,2,1,3);var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.red,0),new GradientColorKey(Color.blue,1)},new[]{new GradientAlphaKey(.2f,0),new GradientAlphaKey(.8f,1)});so.FindProperty("gradient").gradientValue=gradient;so.FindProperty("node").managedReferenceValue=new PlayModeEditProbeNode{value=5,text="changed"};});
                Check(KeepPlayModeEdits.PendingCount>=12,"Inspector edits not captured: "+KeepPlayModeEdits.PendingCount);
                // Runtime writes must not replace the captured manual values or add unedited fields.
                box.size=new Vector3(90,90,90);box.isTrigger=true;image.color=Color.black;image.raycastTarget=false;
                rect.anchoredPosition=new Vector2(900,900);asset.speed=99;asset.values[2]=99;asset.offset=Vector3.one*99;
                SessionState.SetString(Key+"Stage","stop");EditorApplication.isPlaying=false;return;
            }
            if(stage=="stop"&&!EditorApplication.isPlayingOrWillChangePlaymode&&KeepPlayModeEdits.LastAppliedCount>0&&KeepPlayModeEdits.PendingCount==0)
            {
                var go=GameObject.Find(ProbeName);Check(go!=null,"Edit counterpart missing");var image=go.GetComponent<Image>();var box=go.GetComponent<BoxCollider>();var asset=AssetDatabase.LoadAssetAtPath<PlayModeEditProbeAsset>(AssetPath);
                Check(go.GetComponent<RectTransform>().anchoredPosition==new Vector2(31,42),"RectTransform position not preserved");
                Check(box.size==new Vector3(5,6,7),"Component dimensions not preserved");Check(!box.isTrigger&&image.raycastTarget,"Runtime-only field leaked into Edit Mode");
                Check(Vector4.Distance(image.color,new Color(.2f,.4f,.6f,.8f))<.001f,"Color not preserved");Check(image.sprite!=null,"Asset reference not preserved");
                Check(asset.speed==2.25f&&asset.offset==new Vector3(4,5,6)&&asset.tint==Color.magenta,"Asset values not restored from manual edits");
                Check(asset.values.Length==3&&asset.values[2]==9,"Array resize/value not preserved");Check(Mathf.Abs(asset.curve.Evaluate(0)-2)<.001f,"Animation curve not preserved");Check(Mathf.Abs(asset.gradient.Evaluate(0).a-.2f)<.01f,"Gradient not preserved");Check(asset.node.value==5&&asset.node.text=="changed","Managed reference not preserved");
                Undo.PerformUndo();
                Check(go.GetComponent<RectTransform>().anchoredPosition==new Vector2(11,22)&&box.size==new Vector3(2,3,4),"Stop-time Undo did not restore edit values");
                Finish("PASS: actual Play/Stop plus domain reload; unsaved scene object mapping; manual RectTransform position, component size, color and Sprite reference; ScriptableObject numbers, vectors, color, arrays, curves, gradients and managed references; subsequent runtime overwrites ignored; unedited gameplay fields excluded; single Undo restores scene values. Probe removed, no scene saved.");
            }
        }
        catch(Exception e){Finish("FAIL: "+e);}
    }
    static void Finish(string result)
    {
        SessionState.SetString(Key+"Stage","");if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>Cleanup(result);}else Cleanup(result);
    }
    static void Cleanup(string result)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=()=>Cleanup(result);return;}
        var go=GameObject.Find(ProbeName);if(go!=null)Object.DestroyImmediate(go);
        if(AssetDatabase.LoadAssetAtPath<PlayModeEditProbeAsset>(AssetPath)!=null)AssetDatabase.DeleteAsset(AssetPath);
        KeepPlayModeEdits.Enabled=SessionState.GetBool(Key+"Enabled",true);
        Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,result);Debug.Log(result);
    }
}
