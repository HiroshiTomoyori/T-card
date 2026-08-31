using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

public class CardBuilderEditor : EditorWindow
{
    private const string ShadowTexturePath = "Assets/CardShadow_Generated.png";
    private const int ShadowSpread = 32; // 影の広がり幅
    private const int CardWidth = 225;
    private const int CardHeight = 350;

    [MenuItem("GameObject/UI/Bridge Size Card (Auto Setup)", false, 10)]
    public static void CreateBridgeCard()
    {
        // 1. 影用テクスチャの生成と取得
        Sprite shadowSprite = GetOrCreateShadowSprite();

        // 2. キャンバスの取得（なければ作成）
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 3. カードのルートオブジェクト作成
        GameObject cardRoot = new GameObject("Card_BridgeSize", typeof(RectTransform));
        cardRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = cardRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(CardWidth, CardHeight);

        // 4. 背景画像（ベースカラー）の作成
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(cardRoot.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = new Color(0.9f, 0.9f, 0.9f, 1f); // 少しオフホワイト

        // 5. アート用プレースホルダー作成
        GameObject artObj = new GameObject("ArtArea", typeof(RectTransform), typeof(Image));
        artObj.transform.SetParent(cardRoot.transform, false);
        RectTransform artRect = artObj.GetComponent<RectTransform>();
        artRect.anchorMin = Vector2.zero;
        artRect.anchorMax = Vector2.one;
        artRect.sizeDelta = new Vector2(-20, -20); // 枠を少し残す
        Image artImage = artObj.GetComponent<Image>();
        artImage.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        // 6. 影と境界線（ShadowFrame）の作成
        GameObject shadowObj = new GameObject("ShadowFrame", typeof(RectTransform), typeof(Image));
        shadowObj.transform.SetParent(cardRoot.transform, false);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.anchorMin = Vector2.zero;
        shadowRect.anchorMax = Vector2.one;
        // 影の広がり（ShadowSpread）分だけ、カード本体より大きく設定する
        shadowRect.sizeDelta = new Vector2(ShadowSpread * 2, ShadowSpread * 2);

        Image shadowImage = shadowObj.GetComponent<Image>();
        shadowImage.sprite = shadowSprite;
        shadowImage.type = Image.Type.Sliced; // 9パッチを適用
        shadowImage.raycastTarget = false;    // 影はクリック判定を無視させる

        // 作成したオブジェクトを選択状態にする
        Selection.activeGameObject = cardRoot;
        Debug.Log("ブリッジサイズのカードプレハブを構築しました！");
    }

    /// <summary>
    /// 影と内側境界線を持つ9-Slice用のテクスチャをプログラムで生成する
    /// </summary>
    private static Sprite GetOrCreateShadowSprite()
    {
        if (File.Exists(ShadowTexturePath))
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ShadowTexturePath);
        }

        int texSize = 128;
        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[texSize * texSize];

        int innerBoxMin = ShadowSpread;
        int innerBoxMax = texSize - ShadowSpread;
        int borderThickness = 2; // 内側の境界線の太さ（ピクセル）

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                int index = y * texSize + x;

                // ボックスからの距離を計算（角を丸くするための距離関数）
                float distX = Mathf.Max(0, Mathf.Max(innerBoxMin - x, x - innerBoxMax));
                float distY = Mathf.Max(0, Mathf.Max(innerBoxMin - y, y - innerBoxMax));
                float dist = Mathf.Sqrt(distX * distX + distY * distY);

                if (dist > 0)
                {
                    // 枠外：ドロップシャドウ領域（外に行くほど透明に）
                    float alpha = Mathf.Clamp01(1f - (dist / ShadowSpread)) * 0.4f; // 最大不透明度0.4
                    pixels[index] = new Color(0, 0, 0, alpha);
                }
                else
                {
                    // 枠内：内側の境界線 or 透明な中心
                    float innerDistX = Mathf.Min(x - innerBoxMin, innerBoxMax - x);
                    float innerDistY = Mathf.Min(y - innerBoxMin, innerBoxMax - y);
                    float innerDist = Mathf.Min(innerDistX, innerDistY);

                    if (innerDist < borderThickness)
                    {
                        // 境界線（Inner Border）: 少し濃い影でエッジを引き締める
                        pixels[index] = new Color(0, 0, 0, 0.7f);
                    }
                    else
                    {
                        // カードの表示領域：完全に透明
                        pixels[index] = new Color(0, 0, 0, 0f);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // PNGとして保存
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(ShadowTexturePath, bytes);
        AssetDatabase.ImportAsset(ShadowTexturePath, ImportAssetOptions.ForceUpdate);

        // テクスチャのインポート設定をSprite(9-Slice)に変更
        TextureImporter importer = AssetImporter.GetAtPath(ShadowTexturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            // Border設定 (Left, Bottom, Right, Top) - シャドウの広がり幅に合わせて設定
            importer.spriteBorder = new Vector4(ShadowSpread, ShadowSpread, ShadowSpread, ShadowSpread);
            importer.mipmapEnabled = false;
            AssetDatabase.ImportAsset(ShadowTexturePath, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(ShadowTexturePath);
    }
}