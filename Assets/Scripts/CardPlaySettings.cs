using UnityEngine;
[CreateAssetMenu(menuName="T-card/Card Play Settings")]
public sealed class CardPlaySettings : ScriptableObject
{
    [Header("カードプレイ演出")]
    public bool enabledEffect = true;
    [Min(.05f), Tooltip("全パートの再生速度。1 = 承認プレビューと同じ")]
    public float speed = 1f;
    [Header("各パートの秒数（速度1のとき）")]
    [Min(.01f)] public float rotateZoomDuration = .5f;
    [Min(0)] public float revealHoldDuration = .16f;
    [Min(.01f)] public float fieldTravelDuration = .32f;
    [Header("中央のカード・サイズ")]
    [Tooltip("720×760を基準に、画面に合わせて拡縮")]
    public Vector2 revealSize = new Vector2(230,345);
    public Vector2 centerOffset = Vector2.zero;
    [Tooltip("敵の演出だけ上へ移動。正数=上。720×760基準の値")]
    public float enemyCenterYOffset = 70f;
    [Range(.1f,1f)] public float initialSizeRatio = .58f;
    [Range(0,.2f)] public float zoomOvershoot = .035f;
    [Range(-90,90)] public float initialTilt = -18f;
    [Header("味方の演出色 / 背景")]
    public Color backdropColor = new Color(.078f,.059f,.047f,1);
    [Range(0,1)] public float backdropOpacity = .43f;
    [Header("味方のカード表面と輪郭の光")]
    public Color highlightColor = new Color(1,.97f,.84f,1);
    [Range(0,2)] public float highlightStrength = .85f;
    [Range(0,2)] public float outerGlowStrength = 1.15f;
    [Min(0)] public float glowSpread = 42f;
    [Header("味方の柔らかい光・粒子")]
    public Color auraColor = new Color(1,.87f,.59f,1);
    [Range(0,2)] public float auraStrength = .55f;
    [Range(0,2)] public float particleStrength = .55f;
    [Range(0,60)] public int particleCount = 30;
    [Header("味方の円形波紋")]
    public Color rippleColor = new Color(1,.96f,.80f,1);
    [Range(0,1.5f)] public float rippleStrength = .4f;
    [Min(.01f)] public float rippleDuration = .38f;
    [Min(0)] public float rippleExpansion = 47f;
    [Min(1)] public float rippleSoftness = 15f;
    [Header("味方の影")]
    public Color shadowColor = new Color(.176f,.114f,.067f,1);
    [Range(0,1)] public float shadowStrength = .24f;
    [Min(1)] public float shadowBlur = 22;
    public Vector2 shadowOffset = new Vector2(18,-34);
    [Header("敵の演出色")]
    public Color enemyHighlightColor = new Color(.78f,.88f,1f,1);
    public Color enemyAuraColor = new Color(.53f,.67f,1f,1);
    public Color enemyRippleColor = new Color(.8f,.88f,1f,1);
    public Color enemyBackdropColor = new Color(.047f,.059f,.09f,1);
    public Color enemyShadowColor = new Color(.09f,.11f,.176f,1);
    public Color Highlight(bool enemy)=>enemy?enemyHighlightColor:highlightColor;
    public Color Aura(bool enemy)=>enemy?enemyAuraColor:auraColor;
    public Color Ripple(bool enemy)=>enemy?enemyRippleColor:rippleColor;
    public Color Backdrop(bool enemy)=>enemy?enemyBackdropColor:backdropColor;
    public Color Shadow(bool enemy)=>enemy?enemyShadowColor:shadowColor;
    [Header("描画")]
    [Min(1)] public int sortingOffset = 20;
    public float RotateTime => Mathf.Max(.01f,rotateZoomDuration);
    public float HoldTime => Mathf.Max(0,revealHoldDuration);
    public float TravelTime => Mathf.Max(.01f,fieldTravelDuration);
    public float TotalTime => RotateTime+HoldTime+TravelTime;
    static CardPlaySettings fallback;
    public static CardPlaySettings Load()
    {
        var asset=Resources.Load<CardPlaySettings>("TCardPlay/CardPlaySettings");
        if(asset!=null)return asset;
        if(fallback==null){fallback=CreateInstance<CardPlaySettings>();fallback.hideFlags=HideFlags.HideAndDontSave;}
        return fallback;
    }
}
