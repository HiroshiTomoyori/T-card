using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public static class ResourceFinalDiagnostics
{
    [InitializeOnLoadMethod] static void Queue()
    {
        var path=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardResourceFinalValidationV2/result.txt");
        if(File.Exists(path)&&File.ReadAllText(path).StartsWith("PASS:"))return;
        if(SessionState.GetBool("ResourceFinalValidated-v2",false))return;
        EditorApplication.delayCall+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetBool("ResourceFinalValidated-v2",true);Validate();};
    }
    [MenuItem("Tools/T-card/Resource UI Settings")]
    public static void OpenSettings(){var s=Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");Selection.activeObject=s;EditorGUIUtility.PingObject(s);}
    [MenuItem("Tools/T-card/Validate Final Resource UI")]
    public static void Validate()
    {
        var folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardResourceFinalValidationV2");Directory.CreateDirectory(folder);
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture rt=null;Texture2D capture=null;LiquidResourceSettings s=null;
        try
        {
            var original=Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");
            if(original==null||original.suitAtlas==null||original.chargeEffectsShader==null)throw new Exception("Resource assets missing.");
            if(ShaderUtil.ShaderHasError(original.liquidShader)||ShaderUtil.ShaderHasError(original.chargeEffectsShader))throw new Exception("Resource shader error.");
            s=UnityEngine.Object.Instantiate(original);s.panelSize=new Vector2(240,154);s.offset=Vector2.zero;
            var camGo=new GameObject("Validation camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera=camGo.GetComponent<Camera>();camera.scene=scene;camera.transform.position=new Vector3(0,0,-1000);camera.orthographic=true;camera.orthographicSize=380;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.04f);
            rt=new RenderTexture(720,760,24);camera.targetTexture=rt;capture=new Texture2D(720,760,TextureFormat.RGB24,false);
            var cg=new GameObject("Validation canvas",typeof(Canvas));SceneManager.MoveGameObjectToScene(cg,scene);cg.layer=31;var canvas=cg.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var go=new GameObject("Resource",typeof(RectTransform),typeof(LiquidResourceView));go.layer=31;go.transform.SetParent(cg.transform,false);((RectTransform)go.transform).anchoredPosition=new Vector2(0,-80);go.transform.localScale=Vector3.one*1.5f;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");var view=go.GetComponent<LiquidResourceView>();view.Initialize(s,font);view.SetAmount(6,13);view.Advance(.1f);
            Capture(camera,rt,capture);File.WriteAllBytes(Path.Combine(folder,"idle.png"),capture.EncodeToPNG());var lit=capture.GetPixels32();s.suitOpacity=0;view.Advance(0);Capture(camera,rt,capture);int suitPixels=Changed(lit,capture.GetPixels32());if(suitPixels<30)throw new Exception("Suit light not visible: "+suitPixels);s.suitOpacity=original.suitOpacity;
            view.SetTargetActive(true);view.Advance(.22f);if(!view.TargetActive)throw new Exception("Target enter failed");Capture(camera,rt,capture);File.WriteAllBytes(Path.Combine(folder,"target-inside.png"),capture.EncodeToPNG());view.SetTargetActive(false);if(view.TargetActive)throw new Exception("Target exit failed");
            var hand=new GameObject("HandArea",typeof(RectTransform),typeof(Canvas),typeof(CanvasRenderer),typeof(Image));hand.layer=31;hand.transform.SetParent(cg.transform,false);((RectTransform)hand.transform).sizeDelta=new Vector2(100,170);var hc=hand.GetComponent<Canvas>();hc.overrideSorting=true;hc.sortingOrder=100;hand.GetComponent<Image>().color=Color.green;
            view.SyncForegroundSorting(hand.transform);int first=view.ForegroundCanvas.sortingOrder;view.SyncForegroundSorting(hand.transform);if(first!=view.ForegroundCanvas.sortingOrder||first<=hc.sortingOrder)throw new Exception("Foreground order unstable or below hand.");
            Capture(camera,rt,capture);Color before=capture.GetPixel(360,380);view.PlayCharge();view.Advance(.2f);view.SyncForegroundSorting(hand.transform);Capture(camera,rt,capture);Color after=capture.GetPixel(360,380);if(after.r<=before.r+.015f)throw new Exception("Actual beam pixels did not cover the hand: "+before+" / "+after);
            File.WriteAllBytes(Path.Combine(folder,"charge-in-front-of-hand.png"),capture.EncodeToPNG());var wide=capture.GetPixels32();s.beamWidth*=.5f;view.Advance(0);Capture(camera,rt,capture);if(Changed(wide,capture.GetPixels32())<50)throw new Exception("Beam width setting does not affect render.");s.beamWidth=original.beamWidth;
            foreach(var graphic in view.ForegroundCanvas.GetComponentsInChildren<Graphic>(true))if(graphic.raycastTarget)throw new Exception("Foreground intercepts input.");
            hand.SetActive(false);view.Advance(2);view.SetAmount(7,14,true);view.Advance(.18f);Capture(camera,rt,capture);File.WriteAllBytes(Path.Combine(folder,"charge.png"),capture.EncodeToPNG());view.Advance(2);view.SetAmount(3,14);view.Advance(.8f);Capture(camera,rt,capture);File.WriteAllBytes(Path.Combine(folder,"spent.png"),capture.EncodeToPNG());
            var hit=new GameObject("Drop target",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(ResourcePhaseManager));hit.layer=31;hit.transform.SetParent(cg.transform,false);var image=hit.GetComponent<Image>();image.rectTransform.sizeDelta=new Vector2(120,90);var phase=hit.GetComponent<ResourcePhaseManager>();phase.resourceAreaImage=image;Vector2 center=RectTransformUtility.WorldToScreenPoint(camera,hit.transform.position);if(!phase.IsInsideResourceTarget(center,camera)||phase.IsInsideResourceTarget(center+Vector2.right*400,camera))throw new Exception("Drop target bounds incorrect.");
            var source=new GameObject("Ghost source",typeof(RectTransform),typeof(CardController));source.layer=31;source.transform.SetParent(cg.transform,false);
            var art=new GameObject("Artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));art.layer=31;art.transform.SetParent(source.transform,false);var artImage=art.GetComponent<Image>();artImage.sprite=AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/Spade_A.asset").artwork;source.GetComponent<CardController>().artworkImage=artImage;
            var drop=view.DropCard(source);drop.MoveNext();view.Advance(0);var ghost=view.ForegroundCanvas.GetComponentInChildren<Image>();if(ghost==null||ghost.raycastTarget)throw new Exception("Falling card not in non-intercepting foreground Canvas");(drop as IDisposable)?.Dispose();
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: Unity C#/both shaders compiled; exact preview suit atlas renders ("+suitPixels+" changed pixels); inside/outside target bounds; target enter/exit; stable foreground Canvas above hand sorting order 100; actual beam pixels paint over hand; width adjustment changes image; falling card is in foreground Canvas; no raycast interception; charge and spend rendered. Existing scene and saved UI tuning were preserved.");Debug.Log("Final resource UI validated: "+folder);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.ClosePreviewScene(scene);if(rt!=null)UnityEngine.Object.DestroyImmediate(rt);if(capture!=null)UnityEngine.Object.DestroyImmediate(capture);if(s!=null)UnityEngine.Object.DestroyImmediate(s);}
    }
    static int Changed(Color32[] a,Color32[] b){int n=0;for(int i=0;i<a.Length;i++)if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>5)n++;return n;}
    static void Capture(Camera camera,RenderTexture rt,Texture2D capture){Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;capture.ReadPixels(new Rect(0,0,720,760),0,0);capture.Apply();RenderTexture.active=old;}
}
