using UnityEngine;

[CreateAssetMenu(menuName = "T-card/Turn Signal Settings")]
public sealed class TurnSignalSettings : ScriptableObject
{
    [Header("Turn signal / ターン切り替え")]
    [Tooltip("オフにするとターン切り替えの映像を表示しません（効果音は維持）")]
    public bool enabledEffect = true;
    [Min(.05f), Tooltip("1 = 承認したAEプレビューと同じ速さ")]
    public float speed = 1f;
    [Header("Layout / 大きさ・位置")]
    [Range(.1f, 1.5f), Tooltip("画面横幅に対する倍率。1で画面幅いっぱい")]
    public float widthRatio = 1f;
    [Min(.05f), Tooltip("縦方向の倍率。1でAE素材の比率を維持")]
    public float heightScale = 1f;
    [Tooltip("画面中央からの位置。CanvasのUI座標、正のYで上")]
    public Vector2 centerOffset = Vector2.zero;
    [Header("Appearance / 色・透明度")]
    public Color playerTint = Color.white;
    public Color opponentTint = Color.white;
    [Range(0, 1)] public float opacity = 1f;
    [Header("Background Lowlight / 背景の薄暗さ")]
    public Color lowlightColor = Color.black;
    [Range(0,1)] public float lowlightOpacity = .18f;
    [Min(0)] public float lowlightFadeIn = .08f;
    [Min(0)] public float lowlightFadeOut = .16f;
    [Header("Layer / 表示順")]
    [Min(1), Tooltip("既存UIの最前面から追加する描画順")]
    public int sortingOffset = 100;

    // Native AE frames, with a shared crop. Hidden so normal tuning cannot alter timing metadata.
    [HideInInspector] public float frameRate = 60f;
    [HideInInspector] public Vector2 frameSize = new Vector2(1800, 400);
    [HideInInspector] public Texture2D[] playerFrames;
    [HideInInspector] public Texture2D[] opponentFrames;
    static TurnSignalSettings cached;
    public static TurnSignalSettings Load()
    {
        if (cached == null) cached = Resources.Load<TurnSignalSettings>("TCardTurnSignals/TurnSignalSettings");
        return cached;
    }
    public Texture2D[] Frames(bool opponent) => opponent ? opponentFrames : playerFrames;
    public bool HasFrames(bool opponent)
    {
        var frames = Frames(opponent);
        if (frames == null || frames.Length == 0 || frameRate <= 0f) return false;
        foreach (var frame in frames) if (frame != null) return true;
        return false;
    }
}
