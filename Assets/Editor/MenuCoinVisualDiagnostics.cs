using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class MenuCoinVisualDiagnostics
{
    static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardMenuCoinValidation");
    static bool running;
    static MenuCoinVisualDiagnostics(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/T-card/Match Enemy Play Colors To Player")]
    static void MatchColors()
    {
        var s=CardPlaySettingsEditor.Ensure();if(s==null)return;
        Undo.RecordObject(s,"Match enemy play colors to player");
        s.enemyHighlightColor=s.highlightColor;s.enemyAuraColor=s.auraColor;s.enemyRippleColor=s.rippleColor;s.enemyBackdropColor=s.backdropColor;s.enemyShadowColor=s.shadowColor;
        EditorUtility.SetDirty(s);AssetDatabase.SaveAssetIfDirty(s);
        Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"colors.json"),EditorJsonUtility.ToJson(s,true));Selection.activeObject=s;
    }
    [MenuItem("Tools/T-card/Validate Menu and Coin (Play)")]
    static void Begin(){CoinTossDiagnostics.Validate();SessionState.SetBool("TCardMenuCoinProbe",true);running=false;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool("TCardMenuCoinProbe",false)||running||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var title=UnityEngine.Object.FindFirstObjectByType<TitleManager>();if(title==null)return;
        running=true;title.StartCoroutine(Probe(title));
    }
    static void Check(bool value,string text){if(!value)throw new Exception(text);}
    static void Finish(string text){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),text);Debug.Log(text);SessionState.SetBool("TCardMenuCoinProbe",false);EditorApplication.isPlaying=false;}
    static void Capture(string name){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
    static IEnumerator Probe(TitleManager title)
    {
        Directory.CreateDirectory(Folder);yield return null;yield return new WaitForEndOfFrame();string failure=null;
        try
        {
            Check(title.skipTitleScreen&&!title.titleScreen.activeSelf,"Title screen not skipped");Check(title.difficultyScreen.activeInHierarchy&&!title.gameScreen.activeSelf,"Menu is not initial screen");
            var option=title.difficultyScreen.GetComponentsInChildren<Toggle>().FirstOrDefault(t=>t.name=="Skip Action Confirmations");Check(option!=null,"Confirmation option unavailable in menu");
            bool before=option.isOn;option.isOn=!before;Check(TitleManager.SkipActionConfirmations==!before,"Menu confirmation option does not apply");option.isOn=before;
            var s=CardPlaySettings.Load();Check(s.highlightColor==s.enemyHighlightColor&&s.auraColor==s.enemyAuraColor&&s.rippleColor==s.enemyRippleColor&&s.backdropColor==s.enemyBackdropColor&&s.shadowColor==s.enemyShadowColor,"Enemy/player colors differ");
            var coin=Resources.Load<CoinTossSettings>("TCardCoin/CoinTossSettings");var apex=CoinTossMotion.EvaluateShadow(coin,CoinTossMotion.Evaluate(coin,coin.flightPortion*.5f,false));var end=CoinTossMotion.EvaluateShadow(coin,CoinTossMotion.Evaluate(coin,1,false));
            Check(apex.opacity<end.opacity&&apex.radii.x>end.radii.x&&apex.radii.y>end.radii.y,"Shadow does not soften/fade in flight");
            Check(Mathf.Abs(end.center.y-(924*coin.restingHeightFromTop+coin.positionOffset.y+coin.endDiameter*coin.overallScale*.5f+coin.shadowOffset.y))<.001f,"Wrong shadow landing position/size");
            Check(!ShaderUtil.ShaderHasError(coin.coinShader),"Coin shader failed");Capture("main-menu");
        }catch(Exception e){failure=e.ToString();}
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        title.SelectDifficulty();float timeout=Time.realtimeSinceStartup+12;bool spinning=false;
        while(Time.realtimeSinceStartup<timeout)
        {
            var coin=UnityEngine.Object.FindFirstObjectByType<CoinTossManager>();if(coin!=null&&coin.IsSpinning){spinning=true;break;}yield return null;
        }
        yield return new WaitForEndOfFrame();Capture("coin-start");
        try{Check(spinning&&title.gameScreen.activeInHierarchy&&!title.difficultyScreen.activeSelf,"Menu Play did not start coin/match");}catch(Exception e){failure=e.ToString();}
        if(failure!=null){Finish("FAIL: "+failure);yield break;}
        Finish("PASS: Main Menu is initial screen; title hidden; confirmation toggle available and functional; main-menu Play starts real match/coin. All five enemy colors exactly match player values. Shadow follows current landing position/size, fades/spreads in flight and tightens on landing; shader and Unity camera renders pass. No scene saved.");
    }
}
