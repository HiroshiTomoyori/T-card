using UnityEngine;
[CreateAssetMenu(menuName="T-card/Field Highlight Settings")]
public sealed class FieldHighlightSettings : ScriptableObject
{
    [Header("カードサイズ")]
    [Tooltip("自分・敵とも1〜3枚の幅を、そのフィールドの4枚時の幅に揃えます。既存の間隔と5枚以上の設定は維持")]
    public bool useFourCardSizeFromStart = true;
    [Header("フィールドの判定と表示位置 / 720px幅基準")]
    public bool highlightEnabled = true;
    [Min(0)] public float screenSideMargin = 20;
    [Tooltip("相手フィールド下端から下へ空ける距離")]
    public float enemyFieldGap = 12;
    [Tooltip("自分のウォール上端から上へ空ける距離")]
    public float wallClearance = 18;
    [Tooltip("Xは右、Yは上。判定とハイライトの両方を移動")]
    public Vector2 positionOffset = Vector2.zero;
    [Tooltip("判定とハイライトの幅・高さを追加調整")]
    public Vector2 sizeAdjustment = Vector2.zero;
    [Header("縁の色・明るさ")]
    public Color highlightColor = new Color(32f/255,214f/255,196f/255,1);
    [Range(0,3)] public float brightness = 1.15f;
    [Min(.1f)] public float lineWidth = 3.5f;
    [Min(1)] public float blur = 16;
    [Min(0)] public float cornerRadius = 20;
    [Header("内側の発光と外へ広がる光")]
    [Range(0,2)] public float interiorGlow = .45f;
    [Range(0,2)] public float outerGlow = 1;
    [Min(.2f)] public float driftPeriod = 3.6f;
    [Min(0)] public float outwardSpread = 34;
    [Header("呼吸する明暗 / ドロップ判定内")]
    [Min(.2f)] public float pulsePeriod = 2.8f;
    [Range(0,2)] public float minimumPulseBrightness = .3f;
    [Range(0,3)] public float maximumPulseBrightness = 1.4f;
    [Header("フェード / 秒")]
    [Min(0)] public float fadeIn = .14f;
    [Min(0)] public float fadeOut = .18f;
    static FieldHighlightSettings cached;
    public static FieldHighlightSettings Load()
    {
        if(cached!=null)return cached;
        cached=Resources.Load<FieldHighlightSettings>("TCardField/FieldHighlightSettings");
        if(cached==null){cached=CreateInstance<FieldHighlightSettings>();cached.hideFlags=HideFlags.HideAndDontSave;}
        return cached;
    }
}
