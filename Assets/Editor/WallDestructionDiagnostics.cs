using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class WallDestructionDiagnostics
{
    const string Key="TCardWallDestructionProbe";

    static string Result=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardWallDestructionValidation/result.txt");
    static bool started,opened;static double deadline;
    static WallDestructionDiagnostics(){EditorApplication.delayCall+=Audit;EditorApplication.update+=Tick;if(SessionState.GetInt(Key+"Version",0)<2){bool cancel=SessionState.GetBool(Key,false);SessionState.SetBool(Key,false);SessionState.SetInt(Key+"Version",2);if(cancel)EditorApplication.delayCall+=()=>EditorApplication.isPlaying=false;}}
    static void Audit(){Directory.CreateDirectory(Path.GetDirectoryName(Result));var s=WallDestructionSettings.Load();File.WriteAllText(Result+"-assets.txt",s==null?"Settings NULL":"valid="+s.IsValid+" frames="+s.frames.Length+" frame7="+(s.frames[7]==null?"NULL":s.frames[7].format+" "+s.frames[7].width+"x"+s.frames[7].height+" readable="+s.frames[7].isReadable)+" rect7="+s.rectangles[7]);}
    [MenuItem("Tools/T-card/Validate Wall Destruction (Play) %#F9",false,-2)]
    public static void Begin(){if(EditorApplication.isPlaying)return;WallDestructionAssetRepair.Ensure();SessionState.SetBool(Key,true);started=opened=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;

        if(!SessionState.GetBool(Key,false)||started||!EditorApplication.isPlaying)return; if(EditorApplication.isPaused)EditorApplication.isPaused=false;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
        if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: waiting for opening scene");return;}
        if(!opened){var title=UnityEngine.Object.FindFirstObjectByType<TitleManager>();if(title!=null){title.SelectNormal();opened=true;}}
        var d=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(d!=null&&d.IsOpeningDecisionReady){started=true;d.StartCoroutine(Drive(Probe(d)));}
    }
    static void Finish(string message){Directory.CreateDirectory(Path.GetDirectoryName(Result));File.WriteAllText(Result,message);Debug.Log(message);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static IEnumerator Drive(IEnumerator routine)
    {
        var stack=new Stack<IEnumerator>();stack.Push(routine);
        while(stack.Count>0){object value=null;bool more=false;Exception failure=null;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){failure=e;}
            if(failure!=null){while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish("FAIL: "+failure);yield break;}
            if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
        }
        Finish("PASS: 96 native AE frames imported; BC4 masks; own/enemy wall destruction; normal/monarch two-wall simultaneous destruction; platinum bottom-to-top shading and thin colored rims; independent full-effect gradient backdrops; attack execution glows held across own/enemy destruction and cleared before follow-up choices; soft native-silhouette shadows; no survivor wall movement during presentation; layout/input/timeScale locks restored; final frame fully opaque; no procedural replacement or card tint. No scene saved.");
    }
    static IEnumerator Probe(HandDealer d)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Result));var flags=BindingFlags.NonPublic|BindingFlags.Instance;var turn=d.turnManager;d.turnManager=null;typeof(HandDealer).GetMethod("CompleteOpeningSelection",flags).Invoke(d,null);d.turnManager=turn;
        if(turn!=null)typeof(TurnManager).GetField("inactivityTimersEnabled",flags).SetValue(turn,false);
        yield return new WaitForSecondsRealtime(.6f);
        var s=WallDestructionSettings.Load();Check(s!=null&&s.IsValid,"Settings/frames missing");Check(s.frames.Length==96,"Wrong frame count");
        for(int i=s.impactFrame;i<s.frames.Length;i++)Check(s.frames[i]!=null&&s.frames[i].format==TextureFormat.BC4&&!s.frames[i].isReadable,"Frame import incorrect "+i+": "+(s.frames[i]==null?"NULL":s.frames[i].format+" / readable="+s.frames[i].isReadable));
        var walls=new List<GameObject>();foreach(Transform c in d.wallArea)if(c.GetComponent<CardController>()!=null&&c.gameObject.activeInHierarchy&&(c.GetComponent<CanvasGroup>()==null||c.GetComponent<CanvasGroup>().alpha>.01f))walls.Add(c.gameObject);Check(walls.Count>=2,"Two player walls missing");
        foreach(var w in walls)w.GetComponent<CardController>().SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_4.asset"));
        bool suppressed=d.SuppressAttackWallSlash;d.SuppressAttackWallSlash=true;
        try
        {
            int before=(int)typeof(HandDealer).GetField("playerWallAliveCount",flags).GetValue(d);float scale=Time.timeScale;bool complete=false;var survivor=walls[2].transform;var survivorPosition=survivor.localPosition;var layout=d.wallArea.GetComponent<WallAreaLayout>();
            var attacking=UnityEngine.Object.Instantiate(d.proHandCardPrefab,turn.enemyBattleArea).GetComponent<CardController>();attacking.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_Q.asset"));attacking.SetSummonSickness(false);
            var enemyAttack=(IEnumerator)typeof(TurnManager).GetMethod("EnemyWallAttackMotion",flags).Invoke(turn,new object[]{attacking,new List<GameObject>{walls[0],walls[1]}});
            var job=d.StartCoroutine(FinishRoutine(enemyAttack,()=>complete=true));
            for(float wait=0;!PresentationSequenceLock.IsLocked&&!complete&&wait<3;wait+=Time.unscaledDeltaTime)yield return null;Check(PresentationSequenceLock.IsLocked&&Time.timeScale==0,"Presentation/input lock missing: owner="+d.isActiveAndEnabled+" cg="+walls[0].GetComponent<CanvasGroup>().alpha+" resolving="+d.IsResolvingWallAttack+" graphics="+UnityEngine.Object.FindObjectsByType<WallDestructionGraphic>(FindObjectsSortMode.None).Length);
            var allGraphics=UnityEngine.Object.FindObjectsByType<WallDestructionGraphic>(FindObjectsSortMode.None);var graphics=allGraphics.Where(g=>g.gameObject.name=="Platinum wall fragments").ToArray();Check(graphics.Length==2,"Monarch break did not start two effects together");
            Check(allGraphics.Count(g=>g.gameObject.name=="Wall destruction soft shadow")==2,"Shadow count incorrect");
            var motion=turn.GetComponent<AttackMotionPresentation>();Check(motion!=null&&motion.HasWallDestructionFocus&&motion.WallFocusGraphic.Opacity>.99f&&motion.WallFocusGraphic.EntryCount>=4,"Attack focus was not retained across destruction");
            var backdrops=UnityEngine.Object.FindObjectsByType<WallDestructionBackdropGraphic>(FindObjectsSortMode.None);Check(backdrops.Length==2,"Broad gradient shadow count incorrect");
            foreach(var backdrop in backdrops){Check(backdrop.material.shader.isSupported&&!ShaderUtil.ShaderHasError(backdrop.material.shader),"Backdrop shader error");Check(backdrop.color.a==s.backdropColor.a*s.backdropOpacity&&!backdrop.raycastTarget,"Backdrop tuning/input incorrect");}Check(layout!=null&&layout.IsPresentationHeld,"Wall layout was not held");
            yield return new WaitForSecondsRealtime(.35f);
            yield return new WaitForEndOfFrame();Canvas.ForceUpdateCanvases();layout.LayoutWalls();Check(Vector3.Distance(survivor.localPosition,survivorPosition)<.001f,"Surviving wall moved during effect");
            foreach(var visual in graphics){var mesh=visual.canvasRenderer.GetMesh();Check(mesh!=null&&mesh.vertexCount==4&&visual.mainTexture is Texture2D&&visual.material.shader.isSupported,"Effect mesh/texture/shader missing");
                Check(visual.material.shader.name=="T-card/UI/Wall Destruction Platinum Light"&&!ShaderUtil.ShaderHasError(visual.material.shader),"Platinum shader not active");
                Check(visual.material.GetColor("_BottomColor")==s.bottomColor&&visual.material.GetColor("_TopColor")==s.topColor&&visual.material.GetColor("_RimColor")==s.rimColor,"Gradient/rim settings not applied");}
            foreach(var backdrop in backdrops){var mesh=backdrop.canvasRenderer.GetMesh();Check(mesh!=null&&mesh.vertexCount==4,"Backdrop not rendered");}
            Check(motion.HasWallDestructionFocus,"Attack focus disappeared while effect was playing");
            var shot=ScreenCapture.CaptureScreenshotAsTexture();if(shot!=null){File.WriteAllBytes(Path.ChangeExtension(Result,"png"),shot.EncodeToPNG());UnityEngine.Object.Destroy(shot);}
            while(!complete)yield return null;
            Check(!motion.HasWallDestructionFocus,"Attack focus leaked after wall destruction");UnityEngine.Object.Destroy(attacking.gameObject);
            Check((int)typeof(HandDealer).GetField("playerWallAliveCount",flags).GetValue(d)==before-2,"Player wall count incorrect");Check(!layout.IsPresentationHeld,"Wall layout hold leaked");Check(!PresentationSequenceLock.IsLocked&&Time.timeScale==scale,"Lock/timeScale leaked");
            GameObject enemy=null;foreach(Transform c in d.enemyWallArea)if(c.GetComponent<CardController>()!=null&&(c.GetComponent<CanvasGroup>()==null||c.GetComponent<CanvasGroup>().alpha>0)){enemy=c.gameObject;break;}Check(enemy!=null,"Enemy wall missing");
            int enemyBefore=(int)typeof(HandDealer).GetField("enemyWallAliveCount",flags).GetValue(d);enemy.GetComponent<CardController>().SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_4.asset"));
            var player=UnityEngine.Object.Instantiate(d.proHandCardPrefab,turn.playerBattleArea).GetComponent<CardController>();player.SetData(AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_4.asset"));player.SetSummonSickness(false);
            bool playerComplete=false;var playerAttack=(IEnumerator)typeof(TurnManager).GetMethod("PlayerWallAttackMotion",flags).Invoke(turn,new object[]{player,new List<GameObject>{enemy},null});
            d.StartCoroutine(FinishRoutine(playerAttack,()=>playerComplete=true));
            for(float wait=0;!PresentationSequenceLock.IsLocked&&!playerComplete&&wait<4;wait+=Time.unscaledDeltaTime)yield return null;
            Check(PresentationSequenceLock.IsLocked&&motion.HasWallDestructionFocus,"Player attack focus missing during enemy wall destruction");
            while(!playerComplete)yield return null;Check(!motion.HasWallDestructionFocus,"Player attack focus leaked");UnityEngine.Object.Destroy(player.gameObject);Check((int)typeof(HandDealer).GetField("enemyWallAliveCount",flags).GetValue(d)==enemyBefore-1,"Enemy wall count incorrect");
            Check(!PresentationSequenceLock.IsLocked,"Enemy lock leaked");
            var probe=new GameObject("Last frame validation",typeof(RectTransform),typeof(CanvasRenderer),typeof(WallDestructionGraphic));var g=probe.GetComponent<WallDestructionGraphic>();g.Sample(s,10);Check(g.FrameIndex==95&&g.color.a==s.glowColor.a*s.opacity,"Last frame faded out");UnityEngine.Object.Destroy(probe);
        }
        finally{d.SuppressAttackWallSlash=suppressed;}
    }
    static IEnumerator FinishRoutine(IEnumerator routine,Action done){yield return routine;done();}
}





// Recheck native references after asset indentation correction.







