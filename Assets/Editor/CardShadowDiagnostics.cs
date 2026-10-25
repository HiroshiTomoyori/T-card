using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class CardShadowDiagnostics
{
    [InitializeOnLoadMethod] static void Queue()
    {
        string report=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardShadowValidation/result.txt");
        if(File.Exists(report) && File.ReadAllText(report).StartsWith("PASS:"))return;
        if(SessionState.GetBool("TCardShadowCheckedV1",false))return;
        EditorApplication.delayCall+=()=>{
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetBool("TCardShadowCheckedV1",true);Validate();
        };
    }
    [MenuItem("Tools/T-card/Card Shadow Settings")]
    public static void OpenSettings()
    {
        var asset=Resources.Load<CardShadowSettings>("TCardCards/CardShadowSettings");
        Selection.activeObject=asset;EditorGUIUtility.PingObject(asset);
    }
    [MenuItem("Tools/T-card/Validate Card Shadows")]
    public static void Validate()
    {
        string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardShadowValidation");Directory.CreateDirectory(folder);
        var scene=EditorSceneManager.NewPreviewScene();RenderTexture target=null;Texture2D capture=null;CardShadowSettings settings=null;
        try
        {
            var original=Resources.Load<CardShadowSettings>("TCardCards/CardShadowSettings");
            if(original==null || original.shadowShader==null || ShaderUtil.ShaderHasError(original.shadowShader))throw new Exception("Settings or shader unavailable.");
            settings=UnityEngine.Object.Instantiate(original);
            foreach(var prefabPath in new[]{"Assets/Prefabs/CardPrefab.prefab","Assets/Plugins/CardControllerPro/Prefabs/TCardProCard.prefab"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);var c=prefab.GetComponent<CardController>();
                if(c==null || c.artworkImage==null || c.artworkImage.transform==c.transform)throw new Exception("Unsupported card artwork: "+prefabPath);
            }
            var camGo=new GameObject("Shadow preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,scene);
            var camera=camGo.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=330;
            camera.transform.position=new Vector3(0,0,-1000);camera.nearClipPlane=.1f;camera.farClipPlane=10;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.91f,.86f,.74f);camera.cullingMask=1<<31;
            target=new RenderTexture(720,660,24);camera.targetTexture=target;
            var canvasGo=new GameObject("Card shadow preview",typeof(Canvas));SceneManager.MoveGameObjectToScene(canvasGo,scene);canvasGo.layer=31;
            var canvas=canvasGo.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            string[] names={"Club_8","Diamond_10","Spade_Q","Spade_K","Spade_A"};
            var shadows=new System.Collections.Generic.List<CardBoundaryShadow>();
            for(int row=0;row<2;row++)for(int i=0;i<5;i++)
            {
                var data=AssetDatabase.LoadAssetAtPath<CardData>("Assets/CardData/"+names[i]+".asset");if(data==null)throw new Exception("Missing card "+names[i]);
                var root=new GameObject(names[i],typeof(RectTransform),typeof(CardController));root.layer=31;root.transform.SetParent(canvasGo.transform,false);
                var rr=(RectTransform)root.transform;rr.sizeDelta=new Vector2(156,240);rr.anchoredPosition=new Vector2(-170+i*85,165-row*330-Mathf.Abs(i-2)*3);rr.localRotation=Quaternion.Euler(0,0,(i-2)*1.3f);
                var imageGo=new GameObject("Artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));imageGo.layer=31;imageGo.transform.SetParent(root.transform,false);
                var image=imageGo.GetComponent<Image>();image.rectTransform.sizeDelta=new Vector2(156,240);image.sprite=data.artwork;image.raycastTarget=false;
                root.GetComponent<CardController>().artworkImage=image;
                if(row==1){var shadow=root.AddComponent<CardBoundaryShadow>();shadow.settings=settings;shadow.RefreshShadow();shadow.RefreshShadow();shadows.Add(shadow);if(root.GetComponentsInChildren<CardShadowGraphic>().Length!=1)throw new Exception("Duplicate shadow");if(root.GetComponentInChildren<CardShadowGraphic>().raycastTarget)throw new Exception("Shadow blocks card input");}
            }
            capture=new Texture2D(720,660,TextureFormat.RGB24,false);Capture(camera,target,capture);var on=capture.GetPixels32();File.WriteAllBytes(Path.Combine(folder,"comparison.png"),capture.EncodeToPNG());
            settings.strength=0;settings.contactStrength=0;foreach(var s in shadows)s.RefreshShadow();Capture(camera,target,capture);var off=capture.GetPixels32();int changed=0;
            for(int i=0;i<on.Length;i++)if(Math.Abs(on[i].r-off[i].r)>2)changed++;
            if(changed<100)throw new Exception("Shadow opacity does not change rendering: "+changed);
            settings.shadowsEnabled=false;foreach(var s in shadows){s.RefreshShadow();if(s.GetComponentInChildren<CardShadowGraphic>()!=null)throw new Exception("Shadow toggle failed");}
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: C#/shader compiled; standard and Pro card artwork supported; overlapping project sprites rendered; shadow settings change "+changed+" pixels; no duplicate shadows; no input blocking; enable toggle works. Scene edits were not saved.");
            Debug.Log("Card shadow validation passed: "+folder);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.ClosePreviewScene(scene);if(capture!=null)UnityEngine.Object.DestroyImmediate(capture);if(target!=null)UnityEngine.Object.DestroyImmediate(target);if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);}
    }
    static void Capture(Camera camera,RenderTexture target,Texture2D capture)
    {
        Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,720,660),0,0);capture.Apply();RenderTexture.active=previous;
    }
}
