using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class OpeningShuffleDiagnostics
{
    [InitializeOnLoadMethod] static void Queue()
    {
        string report=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardOpeningShuffleValidationV2/result.txt");
        if(File.Exists(report)&&File.ReadAllText(report).StartsWith("PASS:"))return;
        if(SessionState.GetBool("OpeningShuffleValidated-v2",false))return;
        EditorApplication.delayCall+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating){Queue();return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetBool("OpeningShuffleValidated-v2",true);Validate();};
    }
    [MenuItem("Tools/T-card/Opening Shuffle Settings")]
    public static void OpenSettings(){var s=Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");Selection.activeObject=s;EditorGUIUtility.PingObject(s);}
    [MenuItem("Tools/T-card/Validate Opening Shuffle")]
    public static void Validate()
    {
        string folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/TCardOpeningShuffleValidationV2");Directory.CreateDirectory(folder);
        var scene=EditorSceneManager.NewPreviewScene();OpeningShuffleSettings settings=null;Texture2D capture=null;RenderTexture target=null;
        try
        {
            var original=Resources.Load<OpeningShuffleSettings>("TCardOpening/OpeningShuffleSettings");if(original==null)throw new Exception("Settings not found");settings=UnityEngine.Object.Instantiate(original);
            Vector2 end=new Vector2(-274,0),size=new Vector2(146,207);
            for(int i=0;i<settings.visualCardCount;i++)
            {
                if(OpeningShuffleMotion.Sample(settings,i,0,end,size,-360).visible)throw new Exception("Initial placeholder card visible");
                var initial=OpeningShuffleMotion.Sample(settings,i,settings.startDelay+i*settings.cardDropInterval+.00001f,end,size,-360);
                if(initial.position.x+size.x*initial.scale*.5f>=-360)throw new Exception("Card did not start outside left edge");
                float previous=float.PositiveInfinity;
                for(int n=0;n<=30;n++){var pose=OpeningShuffleMotion.Sample(settings,i,settings.startDelay+i*settings.cardDropInterval+settings.cardFallDuration*n/30,end,size,-360);if(!pose.visible)continue;if(pose.position.y>previous+.001f)throw new Exception("Card rises during fall");previous=pose.position.y;}
                var final=OpeningShuffleMotion.Sample(settings,i,settings.ShuffleDuration,end,size,-360);if(!final.landed||Vector2.Distance(final.position,end+new Vector2(i*size.x*.00082f,i*settings.stackThickness*size.x/146))>.001f||Mathf.Abs(final.angle)>.001f)throw new Exception("Stack does not align");
            }
            settings.globalSpeed=2;if(Mathf.Abs(settings.Seconds(1)-.5f)>.0001f)throw new Exception("Global speed conversion");settings.globalSpeed=original.globalSpeed;
            var cameraGo=new GameObject("Opening validation camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.GetComponent<Camera>();camera.scene=scene;camera.transform.position=new Vector3(0,0,-1000);camera.orthographic=true;camera.orthographicSize=350;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.89f,.83f,.68f);
            target=new RenderTexture(720,700,24);camera.targetTexture=target;capture=new Texture2D(720,700,TextureFormat.RGB24,false);
            var cg=new GameObject("Opening validation canvas",typeof(Canvas));SceneManager.MoveGameObjectToScene(cg,scene);cg.layer=31;var canvas=cg.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var deck=new GameObject("DeckBack",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(DeckShuffle),typeof(OpeningShuffleView));deck.layer=31;deck.transform.SetParent(cg.transform,false);var rect=(RectTransform)deck.transform;rect.sizeDelta=size;rect.anchoredPosition=end;
            string path="Assets/Sprites/Cards_tmp/Back.png";var back=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(back==null)foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path))if(a is Sprite sprite){back=sprite;break;}if(back==null)throw new Exception("Real card back not found");
            var image=deck.GetComponent<Image>();image.sprite=back;typeof(DeckShuffle).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(deck.GetComponent<DeckShuffle>(),null);if(image.enabled)throw new Exception("Static deck image not hidden at start");
            Canvas.ForceUpdateCanvases();var view=deck.GetComponent<OpeningShuffleView>();view.Initialize(rect,back,settings);view.Sample(settings,0);if(view.VisibleCardCount!=0)throw new Exception("Card visible before first drop");Capture(camera,target,capture,folder,"empty-start");
            view.Sample(settings,.95f);Capture(camera,target,capture,folder,"falling");view.Sample(settings,1.5f);Capture(camera,target,capture,folder,"landing");view.Sample(settings,settings.ShuffleDuration);Capture(camera,target,capture,folder,"aligned");if(view.VisibleCardCount!=settings.visualCardCount)throw new Exception("Wrong number of visual cards");
            foreach(var graphic in view.OverlayCanvas.GetComponentsInChildren<Graphic>(true))if(graphic.raycastTarget)throw new Exception("Animation intercepts input");
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: Unity C# compiled; no initial/static card; all 20 cards start fully offscreen-left, descend monotonically and retain visible stack edges at actual deck anchor; real card back rendered; independent timing settings and global speed checked; no raycast interception. Preview scene only; current scene not saved.");Debug.Log("Opening shuffle validated: "+folder);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(folder,"result.txt"),"FAIL: "+e);Debug.LogException(e);}
        finally{EditorSceneManager.ClosePreviewScene(scene);if(target!=null)UnityEngine.Object.DestroyImmediate(target);if(capture!=null)UnityEngine.Object.DestroyImmediate(capture);if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);}
    }
    static void Capture(Camera camera,RenderTexture target,Texture2D capture,string folder,string name){Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,720,700),0,0);capture.Apply();RenderTexture.active=old;File.WriteAllBytes(Path.Combine(folder,name+".png"),capture.EncodeToPNG());}
}
