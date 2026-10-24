using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CoinTossDiagnostics
{
    [InitializeOnLoadMethod]
    private static void QueueValidation()
    {
        if (SessionState.GetBool("TCardCoinV4Validated", false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { QueueValidation(); return; }
            SessionState.SetBool("TCardCoinV4Validated", true);
            Validate();
        };
    }

    [MenuItem("Tools/T-card/Validate 2D Coin Toss")]
    public static void Validate()
    {
        string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp", "TCardCoinValidation");
        Directory.CreateDirectory(dir);
        try
        {
            var s = Resources.Load<CoinTossSettings>("TCardCoin/CoinTossSettings");
            if (s == null || s.butterflyTexture == null || s.moonTexture == null || s.coinShader == null)
                throw new Exception("Coin asset references are missing.");
            if (ShaderUtil.ShaderHasError(s.coinShader)) throw new Exception("Coin shader has compiler errors.");
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/CoinTossManager.cs");
            if (script == null || script.GetClass() != typeof(CoinTossManager)) throw new Exception("Manager MonoScript is not bound.");
            foreach (bool front in new[] { true, false })
            {
                var p = CoinTossMotion.Evaluate(s, 1f, front);
                if ((p.cosine > 0) != front || Mathf.Abs(p.sine) > 0.0001f) throw new Exception("Result face mismatch.");
            }
            RenderSamples(s, dir);
            File.WriteAllText(Path.Combine(dir, "result.txt"), "PASS: C# compiled; shader compiled; textures/settings referenced; both result faces correct; Unity camera samples rendered.\nUnity " + Application.unityVersion);
            Debug.Log("T-card 2D Coin Toss validated. " + dir);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(dir, "result.txt"), "FAIL: " + ex);
            Debug.LogException(ex);
        }
    }

    private static void RenderSamples(CoinTossSettings s, string dir)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        RenderTexture rt = null;
        Texture2D image = null;
        try
        {
            var cameraObject = new GameObject("Coin validation camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.transform.position = new Vector3(0, 0, -10000);
            camera.orthographic = true; camera.orthographicSize = 462;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 31; camera.nearClipPlane = 0.1f; camera.farClipPlane = 10;
            rt = new RenderTexture(432, 924, 24); camera.targetTexture = rt;
            var canvasObject = new GameObject("Coin validation canvas", typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasObject, preview); canvasObject.layer = 31;
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            var board = new GameObject("Original board", typeof(RectTransform), typeof(RawImage));
            board.layer = 31; board.transform.SetParent(canvasObject.transform, false);
            var br = board.GetComponent<RectTransform>(); br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            board.GetComponent<RawImage>().texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Image/gameScreen.png");
            var go = new GameObject("Coin validation graphic", typeof(RectTransform), typeof(CoinTossGraphic));
            go.layer = 31; go.transform.SetParent(canvasObject.transform, false);
            var gr = go.GetComponent<RectTransform>(); gr.anchorMin = Vector2.zero; gr.anchorMax = Vector2.one; gr.offsetMin = gr.offsetMax = Vector2.zero;
            var graphic = go.GetComponent<CoinTossGraphic>();
            image = new Texture2D(432, 924, TextureFormat.RGB24, false);
            Canvas.ForceUpdateCanvases();
            foreach (float t in new[] { 0f, 0.4f, 0.64f, 0.9f, 1.64f, 2f })
            {
                graphic.ShowFrame(s, CoinTossMotion.Evaluate(s, t / 2f, false));
                Canvas.ForceUpdateCanvases(); camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 432, 924), 0, 0); image.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(dir, "unity-" + t.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ".png"), image.EncodeToPNG());
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
