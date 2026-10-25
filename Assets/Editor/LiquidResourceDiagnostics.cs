using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LiquidResourceDiagnostics
{
    [InitializeOnLoadMethod]static void Queue()
    {
        // Do not render the demo again on every editor launch or domain reload.
        string project=Path.GetDirectoryName(Application.dataPath);
        string marker=Path.Combine(project,"Library","TCardLiquidResourceValidated-layer-v3.txt");
        if(File.Exists(marker))return;
        string report=Path.Combine(project,"Temp","TCardResourceLayerV3Validation","result.txt");
        if(File.Exists(report)&&File.ReadAllText(report).StartsWith("PASS:"))
        {File.WriteAllText(marker,"Liquid resource UI installation validated.");return;}
        if(SessionState.GetBool("TCardLiquidUiLayerV3Validated",false))return;
        EditorApplication.delayCall+=()=>
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
            SessionState.SetBool("TCardLiquidUiLayerV3Validated",true);Validate();
        };
    }
    [MenuItem("Tools/T-card/Validate Liquid Resource UI")]
    public static void Validate()
    {
        string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp","TCardResourceLayerV3Validation");
        Directory.CreateDirectory(folder);
        try
        {
            var s=Resources.Load<LiquidResourceSettings>("TCardResource/LiquidResourceSettings");
            if(s==null||s.liquidShader==null)throw new Exception("Liquid resource settings/shader missing.");
            if(ShaderUtil.ShaderHasError(s.liquidShader))throw new Exception("Liquid shader compilation failed.");
            if(Mathf.Abs(LiquidResourceView.ResourceFraction(6,13)-6f/13)>1e-5f ||
                LiquidResourceView.ResourceFraction(0,0)!=0 || LiquidResourceView.ResourceFraction(-2,13)!=0 ||
                LiquidResourceView.ResourceFraction(15,13)!=1)throw new Exception("Resource level calculation failed.");
            if(LiquidResourceView.CardPose(s,0).x!=s.dropDistance || LiquidResourceView.CardPose(s,1).x!=0 ||
                LiquidResourceView.CardPose(s,1).w!=0)throw new Exception("Card landing sequence failed.");
            if(s.sourceFont==null)throw new Exception("Lora source font missing.");
            ValidateLayer();
            Render(s,folder);
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: v2: hand/background ordering and actual overlapping pixels checked; Lora loaded; C# and shader compiled; zero/empty/partial/full levels checked; fall ends on impact; actual Unity UI frames rendered.\nTimeline: 6/13 idle -> card fall -> 7/14 charge and column -> 3/14 spend -> 14/14 refill.");
            Debug.Log("Liquid Resource UI validation passed: "+folder);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+e);Debug.LogException(e);}
    }
    static void ValidateLayer()
    {
        var safe=new GameObject("Layer check",typeof(RectTransform));
        try
        {
            var board=new GameObject("GameScreen",typeof(RectTransform));board.transform.SetParent(safe.transform,false);
            var background=new GameObject("Background",typeof(RectTransform));background.transform.SetParent(board.transform,false);
            var hand=new GameObject("HandArea",typeof(RectTransform));hand.transform.SetParent(board.transform,false);
            var resource=new GameObject("Resource",typeof(RectTransform),typeof(Canvas));resource.transform.SetParent(safe.transform,false);
            resource.GetComponent<Canvas>().overrideSorting=true;
            Vector3 position=resource.transform.position;
            LiquidResourceView.PlaceBehindHand(resource.transform,hand.transform);
            if(resource.transform.parent!=hand.transform.parent || resource.transform.GetSiblingIndex()>=hand.transform.GetSiblingIndex() || resource.transform.GetSiblingIndex()<=background.transform.GetSiblingIndex() || resource.GetComponent<Canvas>().overrideSorting || Vector3.Distance(position,resource.transform.position)>.001f)
                throw new Exception("Resource must render above background and below hand, preserving position.");
        }
        finally{UnityEngine.Object.DestroyImmediate(safe);}
    }
    static void ValidateOcclusion(Canvas canvas,Camera camera,RenderTexture target,LiquidResourceView view)
    {
        var host=view.gameObject.AddComponent<Canvas>();host.overrideSorting=true;host.sortingOrder=20;
        var hand=new GameObject("HandArea",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        hand.layer=31;hand.transform.SetParent(canvas.transform,false);
        var rect=(RectTransform)hand.transform;rect.position=view.transform.position;rect.sizeDelta=new Vector2(180,150);
        hand.GetComponent<Image>().color=Color.green;
        Texture2D pixel=null;var previous=RenderTexture.active;
        try
        {
            LiquidResourceView.PlaceBehindHand(view.transform,hand.transform);
            if(host.sortingOrder!=canvas.sortingOrder)throw new Exception("Stale resource canvas sorting order.");
            Canvas.ForceUpdateCanvases();camera.Render();
            Vector2 point=RectTransformUtility.WorldToScreenPoint(camera,rect.position);
            pixel=new Texture2D(1,1,TextureFormat.RGB24,false);RenderTexture.active=target;
            pixel.ReadPixels(new Rect((int)point.x,(int)point.y,1,1),0,0);pixel.Apply();
            Color c=pixel.GetPixel(0,0);
            if(c.g<.6f || c.r>.3f)throw new Exception("Resource painted over the hand in the overlap render: "+c);
        }
        finally
        {
            RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(hand);UnityEngine.Object.DestroyImmediate(host);
            if(pixel!=null)UnityEngine.Object.DestroyImmediate(pixel);
        }
    }
    static void Render(LiquidResourceSettings s,string folder)
    {
        Scene preview=EditorSceneManager.NewPreviewScene();RenderTexture target=null;Texture2D capture=null;
        try
        {
            var cameraGo=new GameObject("Liquid resource preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,preview);
            var cam=cameraGo.GetComponent<Camera>();cam.scene=preview;cam.transform.position=new Vector3(0,0,-10000);
            cam.orthographic=true;cam.orthographicSize=430;cam.nearClipPlane=.1f;cam.farClipPlane=10;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.038f,.032f,.057f);cam.cullingMask=1<<31;
            target=new RenderTexture(640,860,24);cam.targetTexture=target;
            var canvasGo=new GameObject("Liquid resource preview canvas",typeof(Canvas));SceneManager.MoveGameObjectToScene(canvasGo,preview);canvasGo.layer=31;
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
            var root=new GameObject("Liquid Resource",typeof(RectTransform),typeof(LiquidResourceView));root.layer=31;root.transform.SetParent(canvasGo.transform,false);
            var rect=(RectTransform)root.transform;rect.anchoredPosition=new Vector2(0,-120);rect.localScale=Vector3.one*1.65f;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var view=root.GetComponent<LiquidResourceView>();view.Initialize(s,font);view.SetAmount(6,13);
            ValidateOcclusion(canvas,cam,target,view);
            var ghost=new GameObject("Preview falling card",typeof(RectTransform),typeof(Image));ghost.layer=31;ghost.transform.SetParent(root.transform,false);
            var ghostRect=(RectTransform)ghost.transform;ghostRect.sizeDelta=new Vector2(s.dropCardWidth,s.dropCardWidth*1.46f);
            var art=ghost.GetComponent<Image>();
            string[] cards=AssetDatabase.FindAssets("t:CardData",new[]{"Assets/CardData"});
            if(cards.Length>0){var card=AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(cards[0]));if(card!=null)art.sprite=card.artwork;}
            art.preserveAspect=true;art.raycastTarget=false;
            capture=new Texture2D(640,860,TextureFormat.RGB24,false);
            bool charged=false,spent=false,refilled=false;
            Canvas.ForceUpdateCanvases();
            for(int i=0;i<=192;i++)
            {
                float t=i/30f;
                if(t>=1+s.dropDuration&&!charged){view.SetAmount(7,14,true);charged=true;}
                if(t>=3&&!spent){view.SetAmount(3,14);spent=true;}
                if(t>=4.6f&&!refilled){view.SetAmount(14,14);refilled=true;}
                ghost.SetActive(t>=1&&t<1+s.dropDuration);
                if(ghost.activeSelf)
                {
                    Vector4 pose=LiquidResourceView.CardPose(s,(t-1)/s.dropDuration);
                    ghostRect.anchoredPosition=new Vector2(0,s.panelSize.y*(.06f+.85f*(6f/13)-.5f)+pose.x);
                    ghostRect.localRotation=Quaternion.Euler(0,0,pose.y);ghostRect.localScale=Vector3.one*pose.z;art.color=new Color(1,1,1,pose.w);
                }
                view.Advance(i==0?0:1f/30);
                Canvas.ForceUpdateCanvases();cam.Render();
                var previous=RenderTexture.active;RenderTexture.active=target;
                capture.ReadPixels(new Rect(0,0,640,860),0,0);capture.Apply();RenderTexture.active=previous;
                if(i==0 && capture.GetPixel(160,240).r<.3f)throw new Exception("Liquid panel was not visible in the camera render.");
                File.WriteAllBytes(Path.Combine(folder,"frame-"+i.ToString("D3")+".png"),capture.EncodeToPNG());
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            if(target!=null)UnityEngine.Object.DestroyImmediate(target);
            if(capture!=null)UnityEngine.Object.DestroyImmediate(capture);
        }
    }
}
