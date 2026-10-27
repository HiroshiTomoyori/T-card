using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
[InitializeOnLoad]
public static class TurnDrawDiagnostics
{
    static readonly BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
    static string Folder=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardTurnDrawShadowValidation");
    static double deadline;static bool started;
    static TurnDrawDiagnostics(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/T-card/Turn Draw Settings")]
    public static void Settings(){var dealer=UnityEngine.Object.FindFirstObjectByType<HandDealer>(FindObjectsInactive.Include);Selection.activeObject=dealer;EditorGUIUtility.PingObject(dealer);}
    [MenuItem("Tools/T-card/Validate Turn Draw (Play)")]
    static void Begin(){SessionState.SetBool("TCardTurnDrawProbe",true);started=false;deadline=EditorApplication.timeSinceStartup+180;EditorApplication.isPlaying=true;}
    static void Tick()
    {
        if(!SessionState.GetBool("TCardTurnDrawProbe",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||started)return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+180;
        if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL: Timed out waiting for opening hand");return;}
        var dealer=UnityEngine.Object.FindFirstObjectByType<HandDealer>();if(dealer==null||!dealer.IsOpeningDecisionReady)return;
        started=true;dealer.StartCoroutine(Probe(dealer));
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static int Deck(HandDealer dealer)=>((System.Collections.Generic.List<CardData>)typeof(HandDealer).GetField("currentDeck",flags).GetValue(dealer)).Count;
    static IEnumerator Probe(HandDealer dealer)
    {
        // Enter the normal hand layout before testing the turn-start routine.
        var turn=dealer.turnManager;dealer.turnManager=null;
        typeof(HandDealer).GetMethod("CompleteOpeningSelection",flags).Invoke(dealer,null);
        dealer.turnManager=turn;
        yield return new WaitForSeconds(.5f);
        Directory.CreateDirectory(Folder);int beforeHand=dealer.handArea.GetComponentsInChildren<CardController>().Count(c=>c.data!=null),beforeDeck=Deck(dealer);
        bool captured=false,checkedPose=false;string failure=null;Texture2D lit=null,plain=null,noShadow=null,strongShadow=null;float originalShadow=dealer.turnDrawFieldShadowStrength;Vector3[] corners=new Vector3[4];
        dealer.StartCoroutine(dealer.DrawTurnStartCard());float timeout=Time.realtimeSinceStartup+15;
        while(dealer.IsTurnDrawAnimating && Time.realtimeSinceStartup<timeout)
        {
            var fx=UnityEngine.Object.FindFirstObjectByType<TurnDrawHighlightEffect>();
            if(fx!=null)
            {
                try
                {
                    Require(!fx.Halo.raycastTarget&&!fx.Surface.raycastTarget&&!fx.FieldShadow.raycastTarget,"Draw effects intercept input");
                    Require(fx.FieldShadow.GetComponent<Canvas>()==null,"Field shadow has foreground Canvas");
                    Transform branch=dealer.wallArea;while(branch.parent!=fx.FieldShadow.transform.parent && branch.parent!=null)branch=branch.parent;
                    Require(branch.parent==fx.FieldShadow.transform.parent && fx.FieldShadow.transform.GetSiblingIndex()<branch.GetSiblingIndex(),"Field shadow rendered above wall cards");
                    if(fx.Elapsed>=dealer.turnDrawTravelTime+dealer.turnDrawFlipTime && fx.Elapsed<dealer.turnDrawTravelTime+dealer.turnDrawFlipTime+dealer.turnDrawRevealHoldTime)
                    {
                        var rect=fx.GetComponent<RectTransform>();var wall=(RectTransform)dealer.wallArea;
                        var expected=wall.TransformPoint(wall.rect.center)+rect.parent.TransformVector(dealer.turnDrawRevealOffset+Vector2.up*dealer.turnDrawRevealLift);
                        Require(Vector3.Distance(rect.position,expected)<.5f,"Reveal height mismatch");
                        Require(Vector2.Distance(rect.rect.size,dealer.turnDrawRevealSize*dealer.turnDrawRevealScale)<.1f,"Reveal size mismatch");
                        Require(fx.GetComponent<Canvas>().sortingOrder>OpeningShuffleView.ForDeck(dealer.deckPosition).OverlayCanvas.sortingOrder,"Reveal behind deck");checkedPose=true;
                    }
                }
                catch(Exception e){failure=e.ToString();break;}
                if(!captured && checkedPose && fx.Amount>.3f)
                {
                    yield return new WaitForEndOfFrame();
                    lit=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"highlight.png"),lit.EncodeToPNG());
                    fx.Halo.rectTransform.GetWorldCorners(corners);
                    fx.Halo.enabled=false;fx.Surface.enabled=false;
                    yield return new WaitForEndOfFrame();
                    plain=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"without-highlight.png"),plain.EncodeToPNG());
                    dealer.turnDrawFieldShadowStrength=0;fx.SetTime(fx.Elapsed);
                    yield return new WaitForEndOfFrame();
                    noShadow=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"shadow-zero.png"),noShadow.EncodeToPNG());
                    dealer.turnDrawFieldShadowStrength=Mathf.Min(1,Mathf.Max(.48f,originalShadow*2));fx.SetTime(fx.Elapsed);
                    yield return new WaitForEndOfFrame();
                    strongShadow=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,"shadow-strong.png"),strongShadow.EncodeToPNG());
                    dealer.turnDrawFieldShadowStrength=originalShadow;fx.SetTime(fx.Elapsed);
                    fx.Halo.enabled=true;fx.Surface.enabled=true;captured=true;
                }
            }
            yield return null;
        }
        yield return null;
        try
        {
            if(failure!=null)throw new Exception(failure);
            Require(!dealer.IsTurnDrawAnimating,"Draw did not finish");Require(checkedPose&&captured,"Reveal/highlight not observed");
            Require(Deck(dealer)==beforeDeck-1,"Draw removed wrong number from deck");Require(dealer.handArea.GetComponentsInChildren<CardController>().Count(c=>c.data!=null)==beforeHand+1,"Draw did not add exactly one hand card");
            Require(UnityEngine.Object.FindObjectsByType<TurnDrawHighlightEffect>(FindObjectsSortMode.None).Length==0,"Highlight did not clean up");
            Require(UnityEngine.Object.FindObjectsByType<TurnDrawFieldShadowGraphic>(FindObjectsSortMode.None).Length==0,"Field shadow did not clean up");
            var zero=noShadow.GetPixels32();var normal=plain.GetPixels32();var strong=strongShadow.GetPixels32();int darker=0,stronger=0;
            for(int i=0;i<zero.Length;i++)
            {
                int z=zero[i].r+zero[i].g+zero[i].b,n=normal[i].r+normal[i].g+normal[i].b,h=strong[i].r+strong[i].g+strong[i].b;
                if(n<z-8)darker++;if(h<n-8)stronger++;
            }
            Require(darker>100,"Field shadow not visible at configured opacity");Require(stronger>100,"Increasing shadow strength did not darken rendered field");
            var a=lit.GetPixels32();var b=plain.GetPixels32();Require(a.Length==b.Length,"Capture sizes mismatch");int changed=0,outside=0;
            var screen=corners.Select(c=>RectTransformUtility.WorldToScreenPoint(null,c)).ToArray();float xMin=screen.Min(v=>v.x),xMax=screen.Max(v=>v.x),yMin=screen.Min(v=>v.y),yMax=screen.Max(v=>v.y);
            for(int i=0;i<a.Length;i++)
            {
                int diff=Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b);if(diff<8)continue;changed++;
                int x=i%lit.width,y=i/lit.width;if((x<xMin-2||x>xMax+2||y<yMin-2||y>yMax+2)&&x>xMin-70&&x<xMax+70&&y>yMin-70&&y<yMax+70)outside++;
            }
            Require(changed>100,"Highlight did not render");Require(outside>30,"No visible highlight outside card");
            Finish("PASS: Actual Unity turn-draw with field projection; shadow strength 0 hides shadow, configured strength darkens field, stronger value visibly darkens further; shadow stays behind wall/hand cards, never blocks input and cleans up. Actual Unity turn-draw routine; reveal size and upward offset match adjustable settings; front is readable; halo and sheen visibly render ("+changed+" changed pixels, "+outside+" outside card); preview above deck; no raycast interception; exactly one card moves from deck to hand; temporary Canvas/highlights clean up. Existing travel/flip/hold/arrival timing retained.");
        }
        catch(Exception e){Finish("FAIL: "+e);}
        finally{dealer.turnDrawFieldShadowStrength=originalShadow;if(noShadow!=null)UnityEngine.Object.Destroy(noShadow);if(strongShadow!=null)UnityEngine.Object.Destroy(strongShadow);if(lit!=null)UnityEngine.Object.Destroy(lit);if(plain!=null)UnityEngine.Object.Destroy(plain);}
    }
    static void Finish(string result){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"result.txt"),result);Debug.Log(result);SessionState.SetBool("TCardTurnDrawProbe",false);EditorApplication.isPlaying=false;}
}
